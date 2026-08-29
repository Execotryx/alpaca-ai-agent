using AlpacaAgent.Application.Ports;
using AlpacaAgent.Application.Workflow;
using AlpacaAgent.Domain.Workflow;

namespace AlpacaAgent.UnitTests.Workflow;

public sealed class DurableWorkflowRunnerTests
{
    [Fact]
    public async Task RunOne_HandlerExecutesAfterClaimAndCompletionGoesThroughGuardedFinalization()
    {
        var kernel=new RecordingKernel(Claim());var handler=new RecordingHandler(new("COMPLETED","OK"),kernel.Events);
        var result=await new DurableWorkflowRunner(kernel,new Clock(DateTimeOffset.UnixEpoch)).RunOneAsync("worker",TimeSpan.FromMinutes(1),handler,CancellationToken.None);
        Assert.True(result.WorkFound);Assert.Equal(new[]{"claim","handler","finalize"},kernel.Events);
        Assert.Equal("COMPLETED",kernel.Completion!.Outcome);
    }

    [Fact]
    public async Task RunOne_DeadlineCancellationReachesHandlerAndIsClassifiedSeparately()
    {
        var claim=Claim() with { CycleDeadline=DateTimeOffset.UnixEpoch };var kernel=new RecordingKernel(claim);var handler=new CancellationHandler();
        await new DurableWorkflowRunner(kernel,new Clock(DateTimeOffset.UnixEpoch)).RunOneAsync("worker",TimeSpan.FromMinutes(1),handler,CancellationToken.None);
        Assert.True(handler.ObservedCancellation);Assert.Equal("CANCELLED",kernel.Completion!.Classification);
    }

    private static DurableStepClaim Claim()=>new(Guid.NewGuid(),Guid.NewGuid(),WorkflowNode.ReconcileBrokerState,1,"worker",1,DateTimeOffset.UnixEpoch.AddMinutes(1),DateTimeOffset.UnixEpoch.AddMinutes(5));
    private sealed record Clock(DateTimeOffset UtcNow):IClock;
    private sealed class RecordingKernel(DurableStepClaim claim):IDurableWorkflowKernel
    {
        public List<string> Events{get;}=[];public DurableStepCompletion? Completion{get;private set;}
        public Task<DurableStepClaim?> ClaimDueStepAsync(string owner,TimeSpan leaseDuration,CancellationToken cancellationToken){Events.Add("claim");return Task.FromResult<DurableStepClaim?>(claim);}
        public Task<DurableFinalizationResult> FinalizeAsync(DurableStepClaim value,DurableStepCompletion completion,CancellationToken cancellationToken){Completion=completion;Events.Add("finalize");return Task.FromResult(new DurableFinalizationResult(true,true,"OK"));}
        public Task<DurableScheduleResult> ScheduleAsync(DurableScheduleRequest request,CancellationToken cancellationToken)=>throw new NotSupportedException();
        public Task<int> RecoverExpiredLeasesAsync(int maximumAttempts,CancellationToken cancellationToken)=>throw new NotSupportedException();
        public Task SetControlAsync(ControlChange change,CancellationToken cancellationToken)=>throw new NotSupportedException();
        public Task<WorkflowHealth> ObserveHealthAsync(CancellationToken cancellationToken)=>throw new NotSupportedException();
    }
    private sealed class RecordingHandler(DurableStepCompletion completion,List<string> events):IDurableWorkflowNodeHandler
    {
        public ValueTask<DurableStepCompletion> ExecuteAsync(DurableStepClaim claim,CancellationToken cancellationToken){events.Add("handler");return ValueTask.FromResult(completion);}
    }
    private sealed class CancellationHandler:IDurableWorkflowNodeHandler
    {
        public bool ObservedCancellation{get;private set;}
        public ValueTask<DurableStepCompletion> ExecuteAsync(DurableStepClaim claim,CancellationToken cancellationToken){ObservedCancellation=cancellationToken.IsCancellationRequested;throw new OperationCanceledException(cancellationToken);}
    }
}
