using AlpacaAgent.Application.Acceptance;
using AlpacaAgent.Domain.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public abstract class AcceptanceTestBase
{
    private readonly IAcceptanceContract contract = new SpecificationAcceptanceSkeleton();

    protected void AssertImplemented(string stableId, string expectedPublicOutcome)
    {
        var observed = contract.Observe(stableId, expectedPublicOutcome, expectedPublicOutcome);

        Assert.Equal(stableId, observed.StableId);
        Assert.Equal(expectedPublicOutcome, observed.PublicOutcome);
        Assert.Equal(expectedPublicOutcome, observed.SafetyInvariant);
        Assert.True(
            observed.Kind is AcceptanceOutcomeKind.Observed,
            $"{stableId} reached its public contract boundary but production behavior returned NOT_IMPLEMENTED. Expected: {expectedPublicOutcome}");
    }
}
