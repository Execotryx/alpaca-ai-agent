# Alpaca Agent

This repository contains the .NET 10 modular-monolith skeleton, the Phase 2A unit-acceptance catalogue, and the first implemented behavioral slice. UT-040 through UT-055 cover the deterministic workflow kernel; remaining placeholder IDs stay red until implemented in dependency order.

## Validate the baseline

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --list-tests
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~AcceptanceCatalogIntegrityTests"
```

The catalogue integrity test and all IDs marked `IMPLEMENTED` in the manifest must pass. IDs marked `NOT_IMPLEMENTED` intentionally fail at the public acceptance boundary until their owning behavioral phases are implemented.

Regenerate catalogue source and its manifest after a reviewed specification update:

```powershell
./tools/Generate-AcceptanceCatalog.ps1
```
