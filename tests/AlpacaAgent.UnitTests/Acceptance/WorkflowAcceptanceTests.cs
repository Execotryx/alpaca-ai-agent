using AlpacaAgent.Application.Workflow;
using AlpacaAgent.Domain.Workflow;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class WorkflowAcceptanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact, Trait("StableId", "UT-040")]
    public async Task NewEntryWorkflow_ValidInputsAndOneApprovedCandidate_TraversesApplicableNodesInDeclaredOrder()
    {
        var executor = ScriptedExecutor.At(WorkflowNode.PersistOrderIntents, WorkflowNodeResult.Wait("waiting-for-execution"));
        var result = await Run("NEW_ENTRY", executor);
        Assert.Equal(WorkflowTerminalOutcome.WaitingForExecution, result.Outcome);
        Assert.Equal(Enumerable.Range(2, 13).Select(value => (WorkflowNode)value), executor.Executed);
        Assert.All(result.Steps.Where(step => (int)step.Node <= 13), step => Assert.Equal(WorkflowStepState.Completed, step.State));
        Assert.All(result.Steps.Where(step => !WorkflowCatalog.ApplicableNodes("NEW_ENTRY").Contains(step.Node)), step => Assert.Equal(WorkflowStepState.Skipped, step.State));
    }

    [Fact, Trait("StableId", "UT-041")]
    public async Task NewEntryWorkflow_FilledOrderAndOpenCall_CompletesWithLifecycleScheduled()
    {
        var result = await Run("NEW_ENTRY", new ScriptedExecutor(), true);
        Assert.Equal(WorkflowTerminalOutcome.Completed, result.Outcome);
        Assert.NotNull(result.NextLifecycleReview);
        Assert.Equal(WorkflowStepState.Completed, Step(result, WorkflowNode.InitializeOrEvaluateLifecycle).State);
    }

    [Fact, Trait("StableId", "UT-042")]
    public async Task NewEntryWorkflow_AgentReturnsSkip_EndsNoActionWithoutCandidateOrIntent()
    {
        var executor = ScriptedExecutor.At(WorkflowNode.ValidateAgentResult, WorkflowNodeResult.NoAction("SKIP"));
        var result = await Run("NEW_ENTRY", executor);
        Assert.Equal(WorkflowTerminalOutcome.NoAction, result.Outcome);
        Assert.DoesNotContain(WorkflowNode.PersistOrderIntents, executor.Executed);
        Assert.Equal("SKIP", result.Reason);
    }

    [Fact, Trait("StableId", "UT-043")]
    public async Task NewEntryWorkflow_AgentAbstains_EndsSafelyDeferredWithMissingInformation()
    {
        var result = await Run("NEW_ENTRY", ScriptedExecutor.At(WorkflowNode.ValidateAgentResult, WorkflowNodeResult.Defer("INSUFFICIENT_INFORMATION: earnings date unavailable")));
        Assert.Equal(WorkflowTerminalOutcome.SafelyDeferred, result.Outcome);
        Assert.Contains("earnings date", result.Reason, StringComparison.Ordinal);
    }

    [Fact, Trait("StableId", "UT-044")]
    public async Task NewEntryWorkflow_AgentOperationalFailure_DefersAndDoesNotUseAblation()
    {
        var executor = ScriptedExecutor.At(WorkflowNode.AssessPolicy, WorkflowNodeResult.Defer("AGENT_TIMEOUT"));
        var result = await Run("NEW_ENTRY", executor);
        Assert.Equal(WorkflowTerminalOutcome.SafelyDeferred, result.Outcome);
        Assert.DoesNotContain(WorkflowNode.MapPolicyProfile, executor.Executed);
    }

    [Fact, Trait("StableId", "UT-045")]
    public async Task NewEntryWorkflow_NoAdmissibleCandidate_EndsNoActionWithOrderedReasons()
    {
        var result = await Run("NEW_ENTRY", ScriptedExecutor.At(WorkflowNode.ApplyAdmissibilityChecks, WorkflowNodeResult.NoAction("spread;volume")));
        Assert.Equal(WorkflowTerminalOutcome.NoAction, result.Outcome);
        Assert.Equal("spread;volume", result.Reason);
    }

    [Fact, Trait("StableId", "UT-046")]
    public async Task NewEntryWorkflow_RiskRejectsSet_EndsNoActionWithoutIntent()
    {
        var executor = ScriptedExecutor.At(WorkflowNode.ApprovePortfolioRisk, WorkflowNodeResult.NoAction("risk-rejected"));
        var result = await Run("NEW_ENTRY", executor);
        Assert.Equal(WorkflowTerminalOutcome.NoAction, result.Outcome);
        Assert.DoesNotContain(WorkflowNode.PersistOrderIntents, executor.Executed);
    }

    [Fact, Trait("StableId", "UT-047")]
    public async Task ManagementWorkflow_AgentUnavailable_StillRunsDeterministicLifecycle()
    {
        var executor = new ScriptedExecutor();
        var result = await Run("POSITION_MANAGEMENT", executor);
        Assert.Equal(WorkflowTerminalOutcome.Completed, result.Outcome);
        Assert.DoesNotContain(WorkflowNode.AssessPolicy, executor.Executed);
        Assert.Contains(WorkflowNode.InitializeOrEvaluateLifecycle, executor.Executed);
    }

    [Fact, Trait("StableId", "UT-048")]
    public async Task ManagementWorkflow_InvalidBrokerInputs_SafeHaltsRatherThanGuessing()
    {
        var result = await Run("POSITION_MANAGEMENT", ScriptedExecutor.At(WorkflowNode.ValidateDataQuality, WorkflowNodeResult.Fail("BROKER_TRUTH_INCONSISTENT")));
        Assert.Equal(WorkflowTerminalOutcome.FailedAndEscalated, result.Outcome);
        Assert.Equal("BROKER_TRUTH_INCONSISTENT", result.Reason);
    }

    [Fact, Trait("StableId", "UT-049")]
    public void WorkflowTemplate_InapplicableNode_IsSkippedAndCannotRun()
    {
        Assert.Equal(18, WorkflowCatalog.AllNodes.Count);
        Assert.DoesNotContain(WorkflowNode.AssessPolicy, WorkflowCatalog.ApplicableNodes("POSITION_MANAGEMENT"));
        Assert.Contains(WorkflowNode.SelectActionSet, WorkflowCatalog.ApplicableNodes("POSITION_MANAGEMENT"));
        Assert.Contains(WorkflowNode.ApplyAdmissibilityChecks, WorkflowCatalog.Dependencies(WorkflowNode.SelectActionSet, "POSITION_MANAGEMENT"));
    }

    [Fact, Trait("StableId", "UT-050")]
    public void WorkflowRetry_RetryableFailureWithinBudget_SchedulesNewAttempt()
    {
        var decision = new WorkflowRetryPolicy(3, TimeSpan.FromSeconds(30)).Decide(1, FailureClassification.Retryable, Now);
        Assert.True(decision.ShouldRetry);
        Assert.Equal(2, decision.NextAttempt);
        Assert.Equal(Now.AddSeconds(30), decision.NextDueAt);
    }

    [Fact, Trait("StableId", "UT-051")]
    public void WorkflowRetry_BudgetExhausted_EndsFailedAndEscalated()
    {
        var decision = new WorkflowRetryPolicy(3, TimeSpan.FromSeconds(30)).Decide(3, FailureClassification.Retryable, Now);
        Assert.False(decision.ShouldRetry);
        Assert.Equal(WorkflowTerminalOutcome.FailedAndEscalated, decision.TerminalOutcome);
    }

    [Fact, Trait("StableId", "UT-052")]
    public void WorkflowDeadline_ExpiresDuringHandler_ResultCannotAdvanceCycle()
    {
        var authority = Authority();
        var decision = WorkflowFinalizer.Evaluate(authority, authority, Now.AddMinutes(2), Now.AddMinutes(1), false);
        Assert.False(decision.MayAdvance);
        Assert.Equal("DEADLINE_EXPIRED", decision.Classification);
    }

    [Fact, Trait("StableId", "UT-053")]
    public void WorkflowCancellation_PropagatesToPortAndPersistsClassifiedOutcome()
    {
        var authority = Authority();
        var decision = WorkflowFinalizer.Evaluate(authority, authority, Now, Now.AddMinutes(1), true);
        Assert.False(decision.MayAdvance);
        Assert.Equal("CANCELED", decision.Classification);
    }

    [Fact, Trait("StableId", "UT-054")]
    public void Scheduler_DuplicateScheduleKey_ReturnsExistingCycleIdentity()
    {
        var scheduler = new IdempotentCycleScheduler();
        var first = scheduler.Schedule("AAPL:2026-08-29T12:00Z:NEW_ENTRY");
        var duplicate = scheduler.Schedule("AAPL:2026-08-29T12:00Z:NEW_ENTRY");
        Assert.Equal(first.CycleId, duplicate.CycleId);
    }

    [Fact, Trait("StableId", "UT-055")]
    public void CycleConflict_ManagementAndEntryOverlap_ManagementTakesPriority()
    {
        Assert.Equal(CycleConflictDecision.DeferNewEntry, CycleConflictPolicy.Evaluate(true, true, false));
        Assert.Equal(CycleConflictDecision.RejectDuplicateManagement, CycleConflictPolicy.Evaluate(true, false, true));
    }

    private static ValueTask<WorkflowRunResult> Run(string template, ScriptedExecutor executor, bool obligation = false) =>
        new WorkflowCoordinator().RunAsync(new($"cycle:{template}", "AAPL", template, Now.AddMinutes(5), obligation), executor);
    private static WorkflowStepView Step(WorkflowRunResult result, WorkflowNode node) => result.Steps.Single(step => step.Node == node);
    private static ClaimAuthority Authority() => new("worker-a", 7, Now.AddMinutes(1));

    private sealed class ScriptedExecutor(Dictionary<WorkflowNode, WorkflowNodeResult>? script = null) : IWorkflowNodeExecutor
    {
        private readonly Dictionary<WorkflowNode, WorkflowNodeResult> script = script ?? [];
        public List<WorkflowNode> Executed { get; } = [];
        public ValueTask<WorkflowNodeResult> ExecuteAsync(WorkflowNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Executed.Add(node);
            return ValueTask.FromResult(script.GetValueOrDefault(node, WorkflowNodeResult.Complete()));
        }
        public static ScriptedExecutor At(WorkflowNode node, WorkflowNodeResult result) => new(new() { [node] = result });
    }
}
