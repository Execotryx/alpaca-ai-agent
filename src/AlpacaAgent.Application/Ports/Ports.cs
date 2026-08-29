using AlpacaAgent.Domain.Contracts;

namespace AlpacaAgent.Application.Ports;

public interface IClock { DateTimeOffset UtcNow { get; } }
public interface IPolicyAssessor { Task<PolicyAssessmentV1> AssessAsync(string inputReference, CancellationToken cancellationToken); }
public interface IBrokerReadPort { Task<BrokerObservation> ObserveAsync(string requestReference, CancellationToken cancellationToken); }
public interface IBrokerWritePort { Task<BrokerObservation> SubmitAsync(OrderIntent intent, CancellationToken cancellationToken); }
public interface IEvidencePort { Task<EvidenceBundle> RetrieveAsync(string inputReference, CancellationToken cancellationToken); }
public interface IWorkflowRepository { Task<WorkflowCycle?> FindAsync(CycleId cycleId, CancellationToken cancellationToken); }
