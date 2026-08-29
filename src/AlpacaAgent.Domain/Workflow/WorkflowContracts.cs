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
