# Implementation plan version 6

Updated `Alpaca_AI_Agent_Framework_Neutral_Implementation_Plan.md` in ChatGPT Library from version 5 to version 6.

## Changes made

- Documented the normal new-entry, no-action, safe-deferral, open-order, and existing-position lifecycle flows.
- Added an edge-case model covering scheduling, time/calendar behavior, market-data quality and pagination, account permissions, fractional/encumbered coverage, corporate actions, concurrency, order submission ambiguity, status/event ordering, cancel/replace races, partial fills, early assignment, expiration confirmation, paper-trading limitations, evidence safety, AI failures, persistence failures, replay determinism, and control races.
- Added Phase 2A, which requires the complete behavioral unit-test suite to be written and reviewed before behavioral production implementation begins.
- Added 180 stable unit-test IDs with exact test names and required public assertions across contracts, state machines, workflow routing, reconciliation, features, evidence, AI validation, candidate selection, portfolio risk, execution, lifecycle, replay, reporting, and cross-cutting invariants.
- Required test discovery checks, immutable fixtures, an expected red baseline, phase-filtered green gates, property-test seed retention, and safety-critical mutation testing.
- Recast the later test phase as non-unit verification for PostgreSQL, SDK/REST contracts, failure injection, AI evaluation, replay, shadow mode, and paper trading.
- Expanded the scenario catalogue from 48 to 70 scenarios and added source links for the investigated Alpaca and options behavior.
- Updated dependency order and MVP completion criteria so coding cannot begin before the unit-test baseline exists and completion requires every stable unit test to pass.

## Result

The revised plan makes tests the executable acceptance contract for coding agents. Production work must prove compliance by turning the prewritten red tests green without deleting, skipping, or weakening them.
