# Phases 1–3 baseline inventory

Date: 29 August 2026

## Frozen baseline

- Starting commit: `1607fd5053961f0413a7305aa958f3e8add0f127`.
- Governing plan: `Alpaca_AI_Agent_Framework_Neutral_Implementation_Plan.md`, version 7.
- Remediation plan: `Phases_1_3_Compliance_Remediation_Implementation_Plan.md`, version 1.
- .NET SDK: `10.0.400`.
- Package lock: no `packages.lock.json` existed at the frozen baseline.
- PostgreSQL test image selected for remediation: `postgres:18.6-alpine3.23`.
- Stable acceptance catalogue: 180 unique IDs.

The machine-readable per-ID inventory is `tests/AlpacaAgent.UnitTests/acceptance-remediation-ledger.json`; its schema is stored beside it. The ledger records the exact method, Section 10 assertion, production owner, fixture IDs, initial implementation status, initial evidence quality, and current remediation status for every stable ID.

## Existing behavior at the baseline

| Component | Observed behavior | Baseline evidence quality |
| --- | --- | --- |
| `WorkflowCoordinator` | Executes applicable nodes sequentially in memory and routes non-completed dispositions to a terminal result. | Partial; no durable repository boundary. |
| `WorkflowCatalog` | Declares the seven templates except replay instantiation and returns simple predecessor dependencies. | Partial. |
| `WorkflowRetryPolicy` | Classifies retry, cancellation, and exhausted attempts using application time. | Partial. |
| `WorkflowFinalizer` | Compares owner, fence, lease, deadline, and cancellation in memory. | Partial. |
| `IdempotentCycleScheduler` | Deduplicates schedule keys in a process-local dictionary. | Prototype only. |
| `CycleConflictPolicy` | Gives management work priority using Boolean inputs. | Prototype only. |

`UT-040` through `UT-055` are therefore recorded as `PARTIAL_REVIEW_REQUIRED`, not accepted as complete baseline evidence. The remaining 164 tests initially used the generic `SpecificationAcceptanceSkeleton` expected-red boundary.

## Safety and hygiene inventory

- No broker SDK package or network-capable broker-write adapter was present.
- No broker credentials or committed secret values were found in the tracked application sources.
- API and Worker were unmodified .NET templates and were scheduled for removal during contract-boundary cleanup.
- Persistence and Infrastructure had no implementation files at the baseline.

## Reproduction note

The baseline was clean before remediation. Machine-specific command transcripts are intentionally not committed; validation summaries belong in dated changelogs and CI artifacts.
