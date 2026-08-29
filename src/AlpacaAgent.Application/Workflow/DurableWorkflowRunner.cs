using AlpacaAgent.Application.Ports;
using AlpacaAgent.Domain.Workflow;

namespace AlpacaAgent.Application.Workflow;

public interface IDurableWorkflowNodeHandler
{
    ValueTask<DurableStepCompletion> ExecuteAsync(DurableStepClaim claim, CancellationToken cancellationToken);
}

public sealed record DurableRunOneResult(bool WorkFound, DurableStepClaim? Claim, DurableFinalizationResult? Finalization);

public sealed class DurableWorkflowRunner(IDurableWorkflowKernel kernel, IClock clock)
{
    public async Task<DurableRunOneResult> RunOneAsync(
        string owner,
        TimeSpan leaseDuration,
        IDurableWorkflowNodeHandler handler,
        CancellationToken cancellationToken)
    {
        var claim = await kernel.ClaimDueStepAsync(owner, leaseDuration, cancellationToken);
        if (claim is null) return new(false, null, null);

        using var authorityCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var remaining = claim.CycleDeadline - clock.UtcNow;
        if (remaining <= TimeSpan.Zero) authorityCancellation.Cancel();
        else authorityCancellation.CancelAfter(remaining);

        DurableStepCompletion completion;
        try
        {
            completion = await handler.ExecuteAsync(claim, authorityCancellation.Token);
        }
        catch (OperationCanceledException) when (authorityCancellation.IsCancellationRequested)
        {
            completion = new("CANCELLED", "CANCELLED", DiagnosticReference: "handler-cancelled");
        }
        catch (Exception exception)
        {
            completion = new("FAILED", "UNHANDLED_HANDLER_FAILURE", DiagnosticReference: exception.GetType().Name);
        }

        var finalization = await kernel.FinalizeAsync(claim, completion, CancellationToken.None);
        return new(true, claim, finalization);
    }
}
