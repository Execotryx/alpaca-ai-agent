using System.Text.Json.Serialization;

namespace AlpacaAgent.Domain.Contracts;

public readonly record struct CycleId(Guid Value);
public readonly record struct StepId(int Value);
public readonly record struct AttemptId(Guid Value);
public readonly record struct SnapshotId(string Value);
public readonly record struct EvidenceId(string Value);
public readonly record struct IntentId(Guid Value);

public enum CycleType { NewEntry, OpenOrderMonitor, PositionManagement, PreMarket, EndOfDay, ReplaySimulation, PerformanceReport }
public enum StepStatus { Pending, Running, Completed, Skipped, SafelyDeferred, Failed }
public enum PortfolioEligibilityState { Eligible, PendingReconciliation, Ineligible, SafeHalt }
public enum OrderState { IntentCreated, Ready, SubmissionPending, SubmittedUnknown, Accepted, PartiallyFilled, Filled, CancelPending, Canceled, Expired, Rejected, ReconciliationRequired }
public enum OptionLifecycleState { Open, ClosePending, AssignmentPending, ExpirationPending, Closed, Assigned, Expired, SafeHalt }

public sealed record WorkflowBudget(TimeSpan MaximumDuration, int RetryLimit, int AgentAttemptLimit, int ExternalCallLimit, int ActionLimit, int ModelToolLimit = 0);
public sealed record ContractVersions(string Policy, string Schema, string Workflow, string Prompt, string Components);
public sealed record WorkflowCycle(CycleId Id, CycleType Type, DateTimeOffset ScheduledAt, DateTimeOffset StartedAt, StepId CurrentStep, StepStatus Status, WorkflowBudget Budget, ContractVersions Versions, string? CheckpointReference, string? TerminalOutcome, string? TerminalReason);
public sealed record MarketSnapshot(SnapshotId Id, DateTimeOffset SourceTime, DateTimeOffset RetrievalTime, string SessionState, string FreshnessStatus, string ValidationStatus);
public sealed record AccountSnapshot(SnapshotId Id, DateTimeOffset ObservedAt, decimal Cash, decimal BuyingPower, decimal Equity, string AccountStatus, bool TradingBlocked, int OptionsLevel);
public sealed record ReconciledPortfolio(SnapshotId Id, decimal VerifiedShares, decimal FreeShares, decimal ReservedShares, bool SafeHalt, IReadOnlyList<string> Inconsistencies);
public sealed record EvidenceRecord(EvidenceId Id, string Source, DateTimeOffset PublishedAt, string ContentReference, bool Allowlisted, bool Fresh, bool Available);
public sealed record EvidenceBundle(string Id, string ContentHash, string Symbol, DateTimeOffset DecisionTime, IReadOnlyList<EvidenceRecord> Evidence, string ValidationStatus);
public sealed record PolicyAssessmentV1(string InputContentHash, IReadOnlyList<string> SupportingFactors, IReadOnlyList<string> OpposingFactors, IReadOnlyList<string> Conflicts, IReadOnlyList<string> MissingInformation, string Policy, decimal DeclaredConfidence, string Uncertainty, IReadOnlyDictionary<string, IReadOnlyList<EvidenceId>> ClaimEvidence);
public sealed record AgentAttempt(AttemptId Id, string InputHash, string Provider, string Model, DateTimeOffset TimeoutAt, int ToolCount, bool HasSession, ContractVersions Versions);
public sealed record AgentAttemptResult(AttemptId AttemptId, DateTimeOffset ObservedAt, string FinishStatus, string ValidationResult, string? FailureClassification);
public sealed record Candidate(string Id, string Symbol, string Contract, decimal RequiredShares, string CoverageReference, string PolicyProfile, IReadOnlyList<string> Warnings);
public sealed record RankedCandidate(string CandidateId, decimal Score, string ScoringPolicyVersion, IReadOnlyList<string> TieBreakValues);
public sealed record ProposedActionSet(string Id, IReadOnlyList<string> CandidateIds, string ContentHash);
public sealed record PortfolioRiskDecision(string Id, bool Approved, string ActionSetHash, DateTimeOffset ExpiresAt, IReadOnlyList<string> RuleResults);
public sealed record OrderIntent(IntentId Id, string IntentKey, string ClientOrderId, string Contract, int Quantity, string Side, decimal LimitPrice, string TimeInForce, string ApprovalId);
public sealed record DecisionRecord(string Id, CycleId CycleId, string Outcome, string Reason, ContractVersions Versions, IReadOnlyList<string> InputReferences);
public sealed record WorkflowStepAttempt(AttemptId Id, CycleId CycleId, StepId StepId, int AttemptNumber, string Owner, long FencingToken, DateTimeOffset ClaimedAt);
public sealed record WorkflowStepAttemptResult(AttemptId AttemptId, DateTimeOffset ObservedAt, string Outcome, string Classification);
public sealed record OutboxMessage(string Id, string DeduplicationKey, string Kind, DateTimeOffset DueAt, string PayloadReference);
public sealed record BrokerRequestAttempt(string Id, IntentId IntentId, string Operation, DateTimeOffset StartedAt);
public sealed record BrokerObservation(string Id, IntentId IntentId, DateTimeOffset ObservedAt, string Kind, string PayloadReference);
public sealed record BrokerReconciliationResult(IntentId IntentId, OrderState State, string Reason, DateTimeOffset ObservedAt);

[JsonSerializable(typeof(WorkflowCycle))]
[JsonSerializable(typeof(MarketSnapshot))]
[JsonSerializable(typeof(AccountSnapshot))]
[JsonSerializable(typeof(ReconciledPortfolio))]
[JsonSerializable(typeof(EvidenceBundle))]
[JsonSerializable(typeof(PolicyAssessmentV1))]
[JsonSerializable(typeof(AgentAttempt))]
[JsonSerializable(typeof(AgentAttemptResult))]
[JsonSerializable(typeof(Candidate))]
[JsonSerializable(typeof(RankedCandidate))]
[JsonSerializable(typeof(ProposedActionSet))]
[JsonSerializable(typeof(PortfolioRiskDecision))]
[JsonSerializable(typeof(OrderIntent))]
[JsonSerializable(typeof(DecisionRecord))]
[JsonSerializable(typeof(WorkflowStepAttempt))]
[JsonSerializable(typeof(WorkflowStepAttemptResult))]
[JsonSerializable(typeof(OutboxMessage))]
[JsonSerializable(typeof(BrokerRequestAttempt))]
[JsonSerializable(typeof(BrokerObservation))]
[JsonSerializable(typeof(BrokerReconciliationResult))]
public partial class DomainJsonContext : JsonSerializerContext;
