using AlpacaAgent.Domain.Policies;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class PolicyProfileAcceptanceTests
{
    [Fact, Trait("StableId", "UT-130"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void PolicyProfile_EachInnerField_DoesNotExceedOuterLimit()
    {
        var outer = Outer();
        var profiles = new[]
        {
            new CoveredCallProfile("CONSERVATIVE_CALL", 14, 30, .15m, .25m, .08m, 1, .10m, 1),
            new CoveredCallProfile("BALANCED_CALL", 10, 35, .20m, .30m, .10m, 1, .15m, 2),
            new CoveredCallProfile("INCOME_CALL", 7, 45, .25m, .35m, .12m, 1, .20m, 3)
        };
        Assert.All(profiles, profile => Assert.True(CoveredCallProfileValidator.Validate(outer, profile).IsValid));
    }

    [Fact, Trait("StableId", "UT-131"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void PolicyProfile_AnyInnerLimitExceedsOuterLimit_IsRejected()
    {
        var outer = Outer();
        var valid = new CoveredCallProfile("BOUNDARY", 7, 45, .10m, .40m, .15m, 1, .25m, 3);
        var mutations = new[]
        {
            valid with { MinimumDte = 6 }, valid with { MaximumDte = 46 }, valid with { MinimumDelta = .09m },
            valid with { MaximumDelta = .41m }, valid with { MaximumSpreadRatio = .16m },
            valid with { MaximumContractsPerShareBlock = 2 }, valid with { MaximumPortfolioExposure = .26m }, valid with { RetryLimit = 4 }
        };
        Assert.All(mutations, profile => Assert.False(CoveredCallProfileValidator.Validate(outer, profile).IsValid));
    }

    private static CoveredCallLimits Outer() => new(7, 45, .10m, .40m, .15m, 1, .25m, 3);
}
