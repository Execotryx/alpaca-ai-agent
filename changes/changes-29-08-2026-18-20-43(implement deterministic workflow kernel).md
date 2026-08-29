# Implement deterministic workflow kernel

Created: 29-08-2026-18-20-43

## Scope

Advanced the implementation from the Phase 2A placeholder catalogue into the first production behavior slice by implementing Section 10 tests UT-040 through UT-055 and their public Domain/Application workflow behavior. PostgreSQL durability and the remaining Phase 3 infrastructure are explicitly deferred to the next slice.

## Key decisions

- Kept node 1 scheduler-owned and represented all 18 nodes in every workflow result.
- Encoded each Section 5.2 template as an explicit applicable-node set; inapplicable nodes receive a stable `SKIPPED` reason.
- Used typed node dispositions for completed, no-action, safe-deferral, failure, and external-wait outcomes instead of interpreting strings.
- Made terminal routing skip all remaining work, while an external wait retains pending successors for restart.
- Enforced bounded retries, exact deadline behavior, cancellation classification, lease/fencing authority checks, idempotent schedule keys, and management priority.
- Changed catalogue generation so implemented tests remain hand-written and cannot be overwritten by placeholder regeneration.

## Files changed

- Added `src/AlpacaAgent.Domain/Workflow/WorkflowContracts.cs`.
- Added `src/AlpacaAgent.Application/Workflow/WorkflowServices.cs`.
- Replaced generated UT-040 through UT-055 placeholders with behavioral tests and a controlled node-executor fake.
- Renamed remaining generated acceptance classes to clearly distinguish placeholders.
- Updated manifest implementation status, generator behavior, repository guidance, and README state.

## Validation

- Captured a focused red baseline: all 16 workflow cases failed against explicit stubs after correcting one initially vacuous scheduler stub.
- `dotnet build --configuration Release`: passed with zero warnings and zero errors.
- Focused workflow suite: 16 passed, zero failed, zero skipped.
- Catalogue integrity plus workflow suite: 17 passed.
- Discovery: 181 total tests, including all 180 stable IDs.
- Full solution test: expected non-zero while later placeholders remain; unit result was 164 intentional red placeholders and 17 passing tests, while all other scaffold test projects passed.
