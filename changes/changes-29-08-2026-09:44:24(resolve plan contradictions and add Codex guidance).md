# Resolve plan contradictions and add Codex guidance

**Date:** 29 August 2026  
**Plan version:** 7

## Scope

Reviewed the implementation plan repeatedly for contradictions and duplicated normative requirements, corrected every finding, added repository-wide Codex guidance, and prepared both documents for publication.

## Key decisions

- Added explicit workflow templates: all 18 node identities are persisted, applicable nodes execute, and inapplicable nodes are terminal `SKIPPED` records.
- Made entry cycles register lifecycle obligations and schedule separate management cycles instead of remaining open for the life of an option position.
- Split workflow, agent, and broker request-start records from immutable result or observation records.
- Added physical share-reservation storage and missing account, contract, corporate-action, and dividend-calendar fields.
- Excluded native replacement and rolling from the MVP. Order changes now require cancel, broker confirmation, fresh exact-set approval, and a new intent.
- Kept risk approval exact-set only; any smaller-set choice belongs to deterministic selection before approval.
- Removed deterministic semantic evidence labels and separated model semantic output from system-owned operational evidence.
- Reordered solution creation, the complete test-first baseline, runtime spikes, crash-window gates, shadow mode, and paper trading to remove circular phase dependencies.
- Kept all 180 stable unit-test IDs mandatory by rewriting deferred-feature tests as MVP-disabled behavior checks.
- Consolidated repeated platform and implementation rules into authoritative sections with traceability references.

## Files changed

- `Alpaca_AI_Agent_Framework_Neutral_Implementation_Plan.md`
- `AGENTS.md`
- `changes/changes-29-08-2026-09:44:24(resolve plan contradictions and add Codex guidance).md`

## Validation

- Completed correction and review cycles until the final pass found no unresolved contradiction or duplicated normative requirement.
- Confirmed 180 unique stable unit-test IDs remain in the plan.
- Confirmed the required scenario catalogue contains 70 uniquely numbered scenarios.
- Checked for stale contradictory phrases and exact repeated normative prose.
- Confirmed Markdown code fences are balanced.
- No application build or runtime tests were run because this commit contains planning and agent-instruction documents only; the solution does not yet exist in the repository.
