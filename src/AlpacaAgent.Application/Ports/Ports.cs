using AlpacaAgent.Domain.Contracts;
using AlpacaAgent.Domain.Workflow;

namespace AlpacaAgent.Application.Ports;

public interface IClock { DateTimeOffset UtcNow { get; } }
public interface IPolicyAssessor { Task<PolicyAssessmentV1> AssessAsync(string inputReference, CancellationToken cancellationToken); }
public interface IBrokerReadPort { Task<BrokerObservation> ObserveAsync(string requestReference, CancellationToken cancellationToken); }
public interface IBrokerWritePort { Task<BrokerObservation> SubmitAsync(OrderIntent intent, CancellationToken cancellationToken); }
public interface IEvidencePort { Task<EvidenceBundle> RetrieveAsync(string inputReference, CancellationToken cancellationToken); }
public interface IWorkflowRepository { Task<WorkflowCycle?> FindAsync(CycleId cycleId, CancellationToken cancellationToken); }
public interface IDurableWorkflowKernel
{
    Task<DurableScheduleResult> ScheduleAsync(DurableScheduleRequest request, CancellationToken cancellationToken);
    Task<DurableStepClaim?> ClaimDueStepAsync(string owner, TimeSpan leaseDuration, CancellationToken cancellationToken);
    Task<DurableFinalizationResult> FinalizeAsync(DurableStepClaim claim, DurableStepCompletion completion, CancellationToken cancellationToken);
    Task<int> RecoverExpiredLeasesAsync(int maximumAttempts, CancellationToken cancellationToken);
    Task SetControlAsync(ControlChange change, CancellationToken cancellationToken);
    Task<WorkflowHealth> ObserveHealthAsync(CancellationToken cancellationToken);
}
public interface IPhase3MessageSink
{
    Task DeliverAsync(string deduplicationKey, string messageType, string messageVersion, string payloadJson, CancellationToken cancellationToken);
}
public interface IDurableOutboxDispatcher
{
    Task<DurableOutboxClaim?> ClaimAsync(string owner, TimeSpan leaseDuration, CancellationToken cancellationToken);
    Task<OutboxDispatchResult> DispatchAsync(DurableOutboxClaim claim, IPhase3MessageSink sink, CancellationToken cancellationToken);
    Task<int> RecoverExpiredAsync(CancellationToken cancellationToken);
}
