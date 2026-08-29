using AlpacaAgent.Domain.Acceptance;

namespace AlpacaAgent.Application.Acceptance;

public interface IAcceptanceContract
{
    AcceptanceOutcome Observe(string stableId, string expectedPublicOutcome, string safetyInvariant);
}

public sealed class SpecificationAcceptanceSkeleton : IAcceptanceContract
{
    public AcceptanceOutcome Observe(string stableId, string expectedPublicOutcome, string safetyInvariant) =>
        AcceptanceOutcome.NotImplemented(stableId, expectedPublicOutcome, safetyInvariant);
}
