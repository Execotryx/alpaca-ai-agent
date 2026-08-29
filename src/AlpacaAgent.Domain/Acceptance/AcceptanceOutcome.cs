namespace AlpacaAgent.Domain.Acceptance;

public enum AcceptanceOutcomeKind
{
    NotImplemented,
    Observed
}

public sealed record AcceptanceOutcome(
    string StableId,
    AcceptanceOutcomeKind Kind,
    string PublicOutcome,
    string SafetyInvariant)
{
    public static AcceptanceOutcome NotImplemented(
        string stableId,
        string expectedPublicOutcome,
        string safetyInvariant) =>
        new(stableId, AcceptanceOutcomeKind.NotImplemented, expectedPublicOutcome, safetyInvariant);
}
