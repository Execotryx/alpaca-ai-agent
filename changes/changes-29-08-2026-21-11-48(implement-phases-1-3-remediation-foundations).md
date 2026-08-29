# Implement Phases 1–3 remediation foundations

Date: 29 August 2026

## Scope

Implemented the first compliance-remediation slices: real Phase 1/2 acceptance behavior, repaired acceptance metadata, the Phase 3 database schema and durable kernel boundary, safe composition roots, and baseline traceability artifacts.

## Key decisions

- Replaced generic acceptance routing for the active contract and state-machine IDs with direct public Domain behavior.
- Removed the universal future-ID acceptance router; each expected-red test now calls an explicitly named component-owned boundary.
- Kept future-phase IDs expected-red and preserved all 180 stable IDs.
- Used PostgreSQL database time, short explicit transactions, row locks, leases, and monotonically increasing fencing tokens for authority.
- Limited the Phase 3 outbox to alerts, recorder messages, and tests; broker writes remain unreachable.
- Kept the Worker disabled until persistence and concrete node handlers are configured.
- Selected `postgres:18.6-alpine3.23` for container tests and pinned all newly introduced packages.

## Files changed

- Domain contract values, policy profiles, portfolio/order/lifecycle state machines, and durable workflow records.
- Complete immutable Section 7 v1 record surface and source-generated serialization context.
- Ten strict versioned contract schemas, 17 hashed scenario fixtures, and exhaustive boundary matrices.
- Application durable workflow port.
- Persistence EF Core context, initial migration, PostgreSQL workflow kernel, fenced outbox dispatcher, exact-scope controls, health observations, and service registration.
- API/Worker composition roots.
- Acceptance tests, typed manifest, schema validation, deterministic generators, remediation ledger, and project dependencies.
- Baseline inventory, persistence mapping specification, verification-evidence matrix, `README.md`, and `AGENTS.md`.

## Validation performed

- `dotnet build --configuration Release --no-restore` — passed with zero warnings and errors after package alignment.
- Focused active acceptance suite — 43 passed, 0 failed, 0 skipped; catalogue integrity passed separately.
- Full acceptance audit — 43 active passes and 137 approved expected-red failures, with zero skips, unexpected greens, or unexpected failures.
- `dotnet tool run dotnet-ef migrations list --project src/AlpacaAgent.Persistence --configuration Release --no-build --no-connect` — discovered `20260829180000_InitialPhase3`.
- Acceptance catalogue regeneration repeated with byte-identical output.
- Remediation ledger generated with 180 unique entries and no null fixture arrays.
- `git diff --check` — passed after generator newline normalization.
- Contract suite — 8 tests passed for schema round trips, negative validation, fixture hashes, and architecture boundaries.
- PostgreSQL Testcontainers suite — two final consecutive full runs each passed 26 tests with 0 failures and 0 skips, including concurrent claims, restart coverage, and explicit pre-claim, post-finalization, and post-delivery crash injection.
- Live PostgreSQL validation exposed and verified fixes for the EF Core model snapshot and Npgsql 10 UTC timestamp reads.

## Not run and remaining risk

- Real broker-submission process-kill windows remain later Phase 10 scope; Phase 3 keeps broker writes unreachable.
