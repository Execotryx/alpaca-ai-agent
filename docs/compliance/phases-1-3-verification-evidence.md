# Phases 1-3 verification evidence

Date: 30 August 2026

## Acceptance baseline

- Catalogue: 180 unique stable IDs.
- Active through Phase 3: 43 passing direct public-behavior tests.
- Approved future expected-red: 137 failures through explicitly named component boundaries.
- Skipped, unexpected-green, and unexpected-failure counts: zero.
- Universal stable-ID acceptance router: removed and prohibited by the integrity test.

## Contract and architecture evidence

- Ten strict draft 2020-12 contract schemas are generated under `schemas/contracts/v1/`.
- Contract tests cover source-generated round trips, unknown and missing properties, enum and numeric rejection, versions, fixture hashes, and dependency boundaries.
- Latest local result: 8 passed, 0 failed, 0 skipped.

## PostgreSQL evidence prepared

The compiled Testcontainers suite exposes `P3-DB-001` through `P3-DB-016`, `P3-OUT-001` through `P3-OUT-003`, `P3-CTRL-001` through `P3-CTRL-003`, `P3-HEALTH-001`, and explicit `P3-FI-001`, `P3-FI-006`, and `P3-FI-008` crash-boundary cases. It covers unique scheduling, exact node persistence, immutable evidence, concurrent claims, reclaim fencing, stale finalization, atomic finalization and rollback, retries, deadlines, cancellation, terminal immutability, restart, every template, frozen replay, checkpoints, domain events, outbox recovery/dead-letter behavior, all control scopes, and health safety signals.

The suite was executed through the Docker Engine installed in the Ubuntu WSL distribution. Two consecutive full runs passed:

- Final run 1: 26 passed, 0 failed, 0 skipped in 1 minute 23 seconds.
- Final run 2: 26 passed, 0 failed, 0 skipped in 1 minute 27 seconds.

The first live database run identified and prompted correction of an incomplete EF Core model snapshot and an Npgsql 10 UTC timestamp mapping mismatch. Both corrections were included before the two green full runs.

## Failure-injection traceability

| Required crash boundary | Current executable evidence |
|---|---|
| Before claim commit | `P3-FI-001` proves cancellation leaves no attempt and work remains pending. |
| After claim commit, before handler | `P3-DB-013` abandons a committed claim and restarts the kernel. |
| During handler | Lease-expiry recovery and preserved-attempt evidence in `P3-DB-005`, `P3-DB-013`, and runner cancellation tests. |
| After handler, before finalization | Expired claim recovery plus stale-finalization rejection in `P3-DB-006`. |
| During finalization, before commit | Forced invalid outbox payload proves state/result/outbox rollback in `P3-DB-008`. |
| After finalization commit, before acknowledgment | `P3-FI-006` replays finalization and proves one authoritative result and one successor activation. |
| After outbox claim, before dispatch | Atomic expired-dispatch recovery in `P3-OUT-003`. |
| After dispatch, before guarded completion | `P3-FI-008` injects failure after the logical effect and proves redelivery remains effectively once. |
| After lease expiry and second-worker takeover | Fencing increase and stale-worker rejection in `P3-DB-005` and `P3-DB-006`. |

## Commands last run

```powershell
dotnet build --configuration Release
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --filter "GateStatus=ActiveThroughPhase3"
dotnet test tests/AlpacaAgent.ContractTests/AlpacaAgent.ContractTests.csproj --configuration Release --no-build
dotnet test tests/AlpacaAgent.PersistenceTests/AlpacaAgent.PersistenceTests.csproj --configuration Release --no-build # run twice
git diff --check
```

The full PostgreSQL suite has passed twice, including concurrent and simulated crash/restart cases. The remediation plan permits simulated crash injection at these boundaries; later real broker-submission process-kill windows remain Phase 10 scope.
