# Add Phases 1–3 compliance remediation plan

Created: 29-08-2026-15-35-24

## Scope

Added a separate, detailed implementation plan for correcting the repository deficiencies found against Phases 1, 2, 2A, and 3 of implementation plan version 7.

## Key decisions

- Treat the current in-memory workflow implementation as an unverified prototype until strengthened acceptance tests prove its complete public behavior.
- Repair the acceptance manifest, fixture catalogue, discovery validation, and all generic placeholder tests before adding further production behavior.
- Preserve all 180 Section 10 stable IDs and enforce real fixture/public-contract traceability.
- Separate the active Phase 1–3 passing gate from the unfiltered 180-ID expected-red audit without skipping or hiding future tests.
- Complete Phase 1 state machines and Phase 2 contracts before replacing the in-memory workflow authority with the Phase 3 PostgreSQL implementation.
- Require real PostgreSQL concurrency, restart, append-only, lease, fencing, atomic finalization, outbox, control, and failure-injection evidence.
- Keep every broker-write path disabled throughout the remediation.

## Files changed

- Added `Phases_1_3_Compliance_Remediation_Implementation_Plan.md`.
- Added this changelog.

## Validation

- Reviewed the remediation stages against Sections 4 through 10 and the dependency/completion rules of the governing plan.
- Checked that the plan addresses every finding from the Phase 1–3 repository compliance review.
- Checked Markdown structure and repository filename rules.
- No source code, tests, schemas, runtime configuration, or deployment behavior changed.
