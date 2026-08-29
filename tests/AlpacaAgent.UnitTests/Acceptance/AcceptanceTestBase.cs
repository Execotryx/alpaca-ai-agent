using AlpacaAgent.Domain.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public abstract class AcceptanceTestBase
{
    protected static void AssertExpectedRed(
        string stableId,
        string expectedPublicOutcome,
        AcceptanceOutcome observed)
    {
        Assert.Equal(stableId, observed.StableId);
        Assert.Equal(expectedPublicOutcome, observed.PublicOutcome);
        Assert.Equal(expectedPublicOutcome, observed.SafetyInvariant);
        Assert.True(
            observed.Kind is AcceptanceOutcomeKind.Observed,
            $"{stableId} reached its public contract boundary but production behavior returned NOT_IMPLEMENTED. Expected: {expectedPublicOutcome}");
    }
}
