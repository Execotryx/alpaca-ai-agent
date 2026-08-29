# Alpaca Agent

This repository currently contains the .NET 10 modular-monolith skeleton and the Phase 2A red unit-acceptance baseline defined by Section 10 of `Alpaca_AI_Agent_Framework_Neutral_Implementation_Plan.md`.

## Validate the baseline

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --list-tests
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~AcceptanceCatalogIntegrityTests"
```

The catalogue integrity test must pass. The 180 `StableId` tests intentionally fail with a precise `NOT_IMPLEMENTED` message until their owning behavioral phases are implemented. This is the expected Phase 2A red baseline, not a completed production suite.

Regenerate catalogue source and its manifest after a reviewed specification update:

```powershell
./tools/Generate-AcceptanceCatalog.ps1
```
