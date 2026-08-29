using System.Text.Json;
using AlpacaAgent.Application.Workflow;
using AlpacaAgent.Domain.Contracts;
using AlpacaAgent.Domain.StateMachines;
using AlpacaAgent.Domain.Workflow;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class BoundaryMatrixIntegrityTests
{
    [Fact]
    public void BoundaryMatrices_AllRowsMatchPublicBehavior()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "boundary-matrices.json")));
        var root = document.RootElement;
        Assert.Equal(7, root.GetProperty("portfolioEligibility").GetArrayLength());
        foreach (var row in root.GetProperty("portfolioEligibility").EnumerateArray())
        {
            var actual = PortfolioEligibilityStateMachine.Derive(new(row.GetProperty("approved").GetBoolean(), row.GetProperty("shares").GetDecimal(), row.GetProperty("reserved").GetDecimal(), row.GetProperty("reconciled").GetBoolean(), row.GetProperty("paused").GetBoolean()));
            Assert.Equal(Enum.Parse<PortfolioEligibilityState>(row.GetProperty("state").GetString()!), actual);
        }
        Assert.Equal(18, root.GetProperty("orderNormalization").GetArrayLength());
        foreach (var row in root.GetProperty("orderNormalization").EnumerateArray())
            Assert.Equal(Enum.Parse<OrderState>(row.GetProperty("state").GetString()!), OrderStateMachine.Normalize(row.GetProperty("raw").GetString()!).State);
        Assert.Equal(7, root.GetProperty("workflowTemplates").GetArrayLength());
        foreach (var row in root.GetProperty("workflowTemplates").EnumerateArray())
        {
            var template = row.GetProperty("template").GetString()!;
            var source = row.GetProperty("sourceTemplate").ValueKind == JsonValueKind.Null ? template : row.GetProperty("sourceTemplate").GetString()!;
            var actual = WorkflowCatalog.ApplicableNodes(source).OrderBy(node => (int)node).Select(node => (int)node);
            Assert.Equal(row.GetProperty("nodes").EnumerateArray().Select(value => value.GetInt32()), actual);
        }
        Assert.Equal(4, root.GetProperty("retryClassification").GetArrayLength());
        foreach (var row in root.GetProperty("retryClassification").EnumerateArray())
        {
            var policy = new WorkflowRetryPolicy(row.GetProperty("maximumAttempts").GetInt32(), TimeSpan.FromSeconds(1));
            var result = policy.Decide(row.GetProperty("completedAttempts").GetInt32(), Enum.Parse<FailureClassification>(row.GetProperty("classification").GetString()!), DateTimeOffset.UnixEpoch);
            Assert.Equal(row.GetProperty("retry").GetBoolean(), result.ShouldRetry);
            Assert.Equal(Enum.Parse<WorkflowTerminalOutcome>(row.GetProperty("terminal").GetString()!), result.TerminalOutcome);
        }
        Assert.Equal(8, root.GetProperty("numericThresholds").GetArrayLength());
        Assert.Equal(10, root.GetProperty("lifecycle").GetArrayLength());
        foreach (var row in root.GetProperty("lifecycle").EnumerateArray())
        {
            var source = Enum.Parse<OptionLifecycleState>(row.GetProperty("source").GetString()!);
            var @event = Enum.Parse<OptionLifecycleEvent>(row.GetProperty("event").GetString()!);
            var result = OptionLifecycleStateMachine.Apply(new(source, source == OptionLifecycleState.NoPosition ? 0 : 1, new HashSet<string>()), Guid.NewGuid().ToString("N"), @event, 1);
            Assert.True(result.Accepted);
            Assert.Equal(Enum.Parse<OptionLifecycleState>(row.GetProperty("target").GetString()!), result.State.State);
        }
        Assert.Equal(4, root.GetProperty("controlBoundaries").GetArrayLength());
    }
}
