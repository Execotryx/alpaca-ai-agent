# Phase 3 persistence mapping specification

Date: 29 August 2026

PostgreSQL owns workflow authority. Domain and Application contracts contain no EF Core or Npgsql types. Projection rows may change only through guarded kernel operations; attempt, result, checkpoint, snapshot, event, broker observation, and control-audit rows are insert-only.

| Record | Key and ownership | Payload/version | Required constraints and indexes | Mutability |
| --- | --- | --- | --- | --- |
| Workflow cycle | `cycle_id`; Application workflow | budgets and independent versions as JSONB | unique schedule key; terminal guard | Guarded projection |
| Workflow step | `(cycle_id, step_id)` | input/output references as JSONB | exactly one of each node 1–18; due partial index; lease consistency | Guarded projection |
| Step attempt/result | independent UUID; workflow step FK | immutable references and component versions | unique attempt number; one authoritative result | Append-only trigger |
| Checkpoint | UUID; workflow step | contract version and content hash | unique cycle/step/hash/version | Append-only trigger |
| Outbox | UUID; causing cycle/intent | immutable versioned JSONB payload | unique deduplication key; due partial index; lease/fence columns | Status projection; payload immutable by repository contract |
| Order intent | UUID; approved cycle | immutable executable JSONB plus hash | unique intent key and `client_order_id` | Executable fields immutable after readiness |
| Share reservation | UUID; intent and holding block | quantity and lifecycle state | unique active share block; positive quantity | Released only from broker truth |
| Agent attempt/result | independent UUID | immutable configuration/result JSONB | one authoritative result per attempt | Append-only trigger |
| Broker operation/observation | independent UUID; optional intent/outbox | safely stored request/observation JSONB | foreign-key correlation | Append-only trigger |
| Domain event | UUID and correlation ID | versioned JSONB | event/correlation lookup | Append-only trigger |
| Snapshot/decision | UUID; cycle | versioned JSONB and content hash | unique type/hash/version | Append-only trigger |
| Lifecycle obligation/review | UUID | normalized lifecycle projection | durable due schedule key | Guarded projection |
| Control state/audit | scope key / event UUID | explicit prior/new value and actor/source/reason | unique scope; audit FK | State guarded; audit append-only |

The initial migration owns table and trigger creation. Claiming and finalization use explicit Npgsql transactions because locking, fencing, conditional authority, and atomic outbox creation are correctness boundaries. No network call occurs in these transactions.
