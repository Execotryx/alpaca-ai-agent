using AlpacaAgent.Domain.Workflow;

namespace AlpacaAgent.Application.Workflow;

public interface IWorkflowNodeExecutor { ValueTask<WorkflowNodeResult> ExecuteAsync(WorkflowNode node, CancellationToken cancellationToken); }
public sealed class WorkflowCoordinator
{
    public async ValueTask<WorkflowRunResult> RunAsync(WorkflowRunRequest request, IWorkflowNodeExecutor executor, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ScheduleKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Symbol);
        var applicable = WorkflowCatalog.ApplicableNodes(request.Template, request.FillCreatesObligation);
        var steps = WorkflowCatalog.AllNodes.ToDictionary(
            node => node,
            node => new WorkflowStepView(node, applicable.Contains(node) ? WorkflowStepState.Pending : WorkflowStepState.Skipped, applicable.Contains(node) ? "awaiting-dependency" : $"not-applicable:{request.Template}"));
        steps[WorkflowNode.StartCycle] = new(WorkflowNode.StartCycle, WorkflowStepState.Completed, "scheduler-created", 1);

        foreach (var node in WorkflowCatalog.AllNodes.Where(node => node != WorkflowNode.StartCycle && applicable.Contains(node)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await executor.ExecuteAsync(node, cancellationToken);
            var state = result.Disposition switch
            {
                NodeDisposition.Completed or NodeDisposition.NoAction or NodeDisposition.Waiting => WorkflowStepState.Completed,
                NodeDisposition.SafelyDeferred => WorkflowStepState.SafelyDeferred,
                NodeDisposition.Failed => WorkflowStepState.Failed,
                _ => throw new ArgumentOutOfRangeException()
            };
            steps[node] = new(node, state, result.Reason, 1);
            if (result.Disposition != NodeDisposition.Completed)
            {
                var outcome = result.Disposition switch
                {
                    NodeDisposition.NoAction => WorkflowTerminalOutcome.NoAction,
                    NodeDisposition.SafelyDeferred => WorkflowTerminalOutcome.SafelyDeferred,
                    NodeDisposition.Failed => WorkflowTerminalOutcome.FailedAndEscalated,
                    NodeDisposition.Waiting => WorkflowTerminalOutcome.WaitingForExecution,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (outcome != WorkflowTerminalOutcome.WaitingForExecution)
                {
                    foreach (var remaining in steps.Where(pair => pair.Value.State == WorkflowStepState.Pending).Select(pair => pair.Key).ToArray())
                        steps[remaining] = new(remaining, WorkflowStepState.Skipped, $"terminal-route:{result.Reason}");
                }
                return Result(request, outcome, result.Reason, steps);
            }
        }
        return Result(request, WorkflowTerminalOutcome.Completed, "completed", steps);
    }

    private static WorkflowRunResult Result(WorkflowRunRequest request, WorkflowTerminalOutcome outcome, string reason, Dictionary<WorkflowNode, WorkflowStepView> steps) =>
        new(request.ScheduleKey, outcome, reason, WorkflowCatalog.AllNodes.Select(node => steps[node]).ToArray(), request.FillCreatesObligation && outcome == WorkflowTerminalOutcome.Completed ? request.Deadline.AddHours(1) : null);
}
public static class WorkflowCatalog
{
    private static readonly WorkflowNode[] Nodes = Enum.GetValues<WorkflowNode>();
    private static readonly IReadOnlyDictionary<string, int[]> Templates = new Dictionary<string, int[]>(StringComparer.Ordinal)
    {
        ["NEW_ENTRY"] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 18],
        ["OPEN_ORDER_MONITOR"] = [1, 2, 3, 15, 16, 17, 18],
        ["POSITION_MANAGEMENT"] = [1, 2, 3, 9, 10, 12, 13, 14, 15, 16, 17, 18],
        ["PRE_MARKET"] = [1, 2, 3, 4, 5, 17, 18],
        ["END_OF_DAY"] = [1, 2, 3, 15, 16, 17, 18],
        ["PERFORMANCE_REPORT"] = [1, 17, 18]
    };
    public static IReadOnlyList<WorkflowNode> AllNodes => Nodes;
    public static IReadOnlySet<WorkflowNode> ApplicableNodes(string template, bool fillCreatesObligation = false)
    {
        if (template == "REPLAY_SIMULATION") throw new ArgumentException("Replay must supply its recorded source template.", nameof(template));
        if (!Templates.TryGetValue(template, out var numbers)) throw new ArgumentException($"Unknown workflow template '{template}'.", nameof(template));
        var nodes = numbers.Select(number => (WorkflowNode)number).ToHashSet();
        if (template == "NEW_ENTRY" && fillCreatesObligation) nodes.Add(WorkflowNode.InitializeOrEvaluateLifecycle);
        return nodes;
    }
    public static IReadOnlyList<WorkflowNode> Dependencies(WorkflowNode node, string template)
    {
        var applicable = ApplicableNodes(template).OrderBy(value => (int)value).ToArray();
        var index = Array.IndexOf(applicable, node);
        return index <= 0 ? [] : [applicable[index - 1]];
    }
}
public sealed class WorkflowRetryPolicy(int maximumAttempts, TimeSpan retryDelay)
{
    public RetryDecision Decide(int completedAttempts, FailureClassification classification, DateTimeOffset now)
    {
        if (completedAttempts < 0) throw new ArgumentOutOfRangeException(nameof(completedAttempts));
        if (classification == FailureClassification.Retryable && completedAttempts < maximumAttempts)
            return new(true, completedAttempts + 1, now.Add(retryDelay), WorkflowTerminalOutcome.None, "retry-scheduled");
        var outcome = classification == FailureClassification.Cancellation ? WorkflowTerminalOutcome.SafelyDeferred : WorkflowTerminalOutcome.FailedAndEscalated;
        return new(false, completedAttempts, null, outcome, classification == FailureClassification.Retryable ? "retry-budget-exhausted" : classification.ToString());
    }
}
public static class WorkflowFinalizer
{
    public static FinalizationDecision Evaluate(ClaimAuthority claimed, ClaimAuthority current, DateTimeOffset completedAt, DateTimeOffset deadline, bool cancellationRequested)
    {
        if (cancellationRequested) return new(false, "CANCELED");
        if (completedAt >= deadline) return new(false, "DEADLINE_EXPIRED");
        if (claimed.Owner != current.Owner || claimed.FencingToken != current.FencingToken) return new(false, "LOST_AUTHORITY");
        if (completedAt >= current.LeaseExpiresAt) return new(false, "LEASE_EXPIRED");
        return new(true, "AUTHORIZED");
    }
}
public sealed class IdempotentCycleScheduler
{
    private readonly Dictionary<string, ScheduledCycle> cycles = new(StringComparer.Ordinal);
    public ScheduledCycle Schedule(string scheduleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleKey);
        if (cycles.TryGetValue(scheduleKey, out var existing)) return existing;
        var created = new ScheduledCycle(Guid.NewGuid().ToString("N"), scheduleKey);
        cycles.Add(scheduleKey, created);
        return created;
    }
}
public static class CycleConflictPolicy
{
    public static CycleConflictDecision Evaluate(bool managementActive, bool newEntryRequested, bool duplicateManagementRequested)
    {
        if (managementActive && duplicateManagementRequested) return CycleConflictDecision.RejectDuplicateManagement;
        if (managementActive && newEntryRequested) return CycleConflictDecision.DeferNewEntry;
        return CycleConflictDecision.Allow;
    }
}
