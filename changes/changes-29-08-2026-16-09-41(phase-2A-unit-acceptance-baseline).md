# Phase 2A unit-acceptance baseline

Created: 29-08-2026-16-09-41

## Scope

Created the .NET 10 modular-monolith project skeleton required to compile and discover the complete Section 10 unit-test catalogue. No trading, workflow, broker, persistence, or AI behavior was implemented.

## Key decisions

- Preserved the plan's Phase 2A red-baseline boundary: every stable acceptance test reaches a public Application contract and reports `NOT_IMPLEMENTED` at the intended assertion.
- Parsed the specification as the catalogue source and required exactly 180 unique stable IDs, preserving its reserved gaps.
- Added a discovery/manifest integrity test so missing, duplicated, renamed, or untagged acceptance cases fail visibly.
- Kept provider, broker, persistence, and Agent Framework types out of Domain and Application.

## Files changed

- Added the solution and all twelve Section 5.6 source/test projects.
- Added immutable core contract records, state identifiers, source-generated JSON context declarations, and Application-owned ports.
- Added eleven generated Section 10 acceptance classes, the shared explicit red assertion boundary, the 180-entry JSON manifest, and its JSON Schema.
- Added the repeatable catalogue generator, repository README, SDK pin, ignore rules, and Phase 2A guidance in `AGENTS.md`.

## Validation

- `dotnet restore`: passed; all projects were up to date.
- `dotnet build --configuration Release`: passed with zero warnings and zero errors.
- Unit-test discovery: found 181 tests: 180 stable-ID acceptance cases and one catalogue-integrity gate.
- `AcceptanceCatalog_SpecificationManifestAndDiscovery_AreComplete`: passed.
- Focused `UT-001`: failed at the intended assertion with `NOT_IMPLEMENTED`; no setup, compilation, serialization, or discovery failure occurred.
- Full stable-ID red baseline: exactly 180 failed, zero passed, zero skipped, as required before production behavior.
- `dotnet test --configuration Release --no-build`: expected non-zero exit; the five non-unit placeholder suites passed and the unit project reported 180 intentional red cases plus the passing integrity gate.
