namespace AlpacaAgent.Domain.Workflow;

public enum WorkflowNode { StartCycle = 1, ReconcileBrokerState, ValidateDataQuality, CalculateFeatures, RetrieveBoundedContext, AssessPolicy, ValidateAgentResult, MapPolicyProfile, GenerateCandidates, ApplyAdmissibilityChecks, ScoreAndRank, SelectActionSet, ApprovePortfolioRisk, PersistOrderIntents, ExecuteAndReconcile, InitializeOrEvaluateLifecycle, ExplainOutcome, FinalizeAudit }
public enum WorkflowStepState { Pending, Running, Completed, Skipped, SafelyDeferred, Failed }
public enum WorkflowTerminalOutcome { None, Completed, NoAction, SafelyDeferred, FailedAndEscalated, WaitingForExecution }
public enum NodeDisposition { Completed, NoAction, SafelyDeferred, Failed, Waiting }
public sealed record WorkflowNodeResult(NodeDisposition Disposition, string Reason, string? OutputReference = null)
{
    public static WorkflowNodeResult Complete(string output = "completed") => new(NodeDisposition.Completed, "completed", output);
    public static WorkflowNodeResult NoAction(string reason) => new(NodeDisposition.NoAction, reason);
    public static WorkflowNodeResult Defer(string reason) => new(NodeDisposition.SafelyDeferred, reason);
    public static WorkflowNodeResult Fail(string reason) => new(NodeDisposition.Failed, reason);
    public static WorkflowNodeResult Wait(string reason) => new(NodeDisposition.Waiting, reason);
}
public sealed record WorkflowStepView(WorkflowNode Node, WorkflowStepState State, string Reason, int AttemptNumber = 0, DateTimeOffset? NextDueAt = null);
public sealed record WorkflowRunRequest(string ScheduleKey, string Symbol, string Template, DateTimeOffset Deadline, bool FillCreatesObligation = false);
public sealed record WorkflowRunResult(string CycleId, WorkflowTerminalOutcome Outcome, string Reason, IReadOnlyList<WorkflowStepView> Steps, DateTimeOffset? NextLifecycleReview = null)
{
    public static WorkflowRunResult NotImplemented(WorkflowRunRequest request) => new(request.ScheduleKey, WorkflowTerminalOutcome.None, "NOT_IMPLEMENTED", []);
}
public enum FailureClassification { Retryable, Terminal, Cancellation }
public sealed record RetryDecision(bool ShouldRetry, int NextAttempt, DateTimeOffset? NextDueAt, WorkflowTerminalOutcome TerminalOutcome, string Reason);
public sealed record ClaimAuthority(string Owner, long FencingToken, DateTimeOffset LeaseExpiresAt);
public sealed record FinalizationDecision(bool MayAdvance, string Classification);
public sealed record ScheduledCycle(string CycleId, string ScheduleKey);
public enum CycleConflictDecision { Allow, DeferNewEntry, RejectDuplicateManagement }
public static class WorkflowCycleStateMachine
{
    public static bool MayAdvance(WorkflowTerminalOutcome outcome) => outcome is WorkflowTerminalOutcome.None or WorkflowTerminalOutcome.WaitingForExecution;
}

public sealed record DurableScheduleRequest(string ScheduleKey, string Symbol, string Template, DateTimeOffset Deadline, string? ReplaySourceTemplate = null, string? Strategy = null);
public sealed record DurableScheduleResult(Guid CycleId, bool Created, string Classification);
public sealed record DurableStepClaim(Guid AttemptId, Guid CycleId, WorkflowNode Node, int AttemptNumber, string Owner, long FencingToken, DateTimeOffset LeaseExpiresAt, DateTimeOffset CycleDeadline);
public sealed record CausedOutboxMessage(Guid Id, string DeduplicationKey, string MessageType, string MessageVersion, string PayloadJson, DateTimeOffset DueAt, int MaximumAttempts);
public sealed record CausedCheckpoint(Guid Id, string ContentHash, string ContractVersion, string PayloadReference);
public sealed record CausedDomainEvent(Guid Id, string CorrelationId, string EventType, string ContractVersion, string PayloadJson, DateTimeOffset OccurredAt);
public sealed record DurableStepCompletion(string Outcome, string Classification, string OutputReferencesJson = "[]", string? DiagnosticReference = null, DateTimeOffset? NextDueAt = null, IReadOnlyList<CausedOutboxMessage>? Outbox = null, IReadOnlyList<CausedCheckpoint>? Checkpoints = null, IReadOnlyList<CausedDomainEvent>? DomainEvents = null);
public sealed record DurableFinalizationResult(bool Authoritative, bool Advanced, string Classification);
public sealed record ControlChange(string ScopeType, string ScopeKey, bool Active, string Actor, string Source, string Reason);
public sealed record WorkflowHealth(bool DatabaseAvailable, long? ClaimLoopHeartbeatAgeSeconds, long OldestDueStepSeconds, int ExpiredLeaseCount, int OutboxDepth, long OldestDueOutboxSeconds, int RepeatedRecoveryCount, int DeadLetterCount, int StaleFenceRejectionCount, bool KillSwitchActive);
public sealed record DurableOutboxClaim(Guid AttemptId, Guid OutboxId, string DeduplicationKey, string MessageType, string MessageVersion, string PayloadJson, string Owner, long FencingToken, DateTimeOffset LeaseExpiresAt, int AttemptNumber, int MaximumAttempts);
public sealed record OutboxDispatchResult(bool Completed, bool Retryable, string Classification);
