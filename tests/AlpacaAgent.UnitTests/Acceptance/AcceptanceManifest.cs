namespace AlpacaAgent.UnitTests.Acceptance;

public sealed record AcceptanceManifestEntry(
    string StableId,
    string Method,
    string OwningPhase,
    string ProductionComponent,
    string[] FixtureIds,
    string Classification,
    string ExpectedPublicOutcome,
    string SafetyInvariant,
    string ImplementationStatus,
    string? LastPassingCommit);
