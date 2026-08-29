# Alpaca Agent

This repository contains the .NET 10 modular monolith and the in-progress Phases 1–3 compliance remediation. Direct public-behavior tests currently cover `UT-001`–`UT-012`, `UT-020`–`UT-032`, `UT-040`–`UT-055`, and `UT-130`–`UT-131`. Future-phase stable IDs remain expected-red until their owning behavior is implemented.

Phase 3 now has an EF Core migration and a PostgreSQL-backed workflow kernel for unique scheduling, all-node persistence, database-time claiming, leases and fencing, guarded finalization, atomic checkpoints/events/recorder outbox creation, global/symbol/strategy controls, recovery, and health observations. The Worker registers no scheduler or dispatcher by default, and Phase 3 rejects broker-write outbox message types.

## Validate the baseline

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --list-tests
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~AcceptanceCatalogIntegrityTests"
dotnet test tests/AlpacaAgent.ContractTests/AlpacaAgent.ContractTests.csproj --configuration Release --no-build
```

The catalogue integrity test and all IDs marked `IMPLEMENTED` in the manifest must pass. IDs marked `NOT_IMPLEMENTED` intentionally fail through explicitly named component boundaries until their owning behavioral phases are implemented; there is no universal stable-ID router.

Regenerate catalogue source and its manifest after a reviewed specification update:

```powershell
./tools/Generate-AcceptanceCatalog.ps1
./tools/Generate-RemediationLedger.ps1
```

PostgreSQL integration tests use the pinned `postgres:18.6-alpine3.23` image and support a Docker-compatible runtime, including Docker Engine hosted in WSL through an accessible Docker endpoint. No paper-broker write path is enabled.
