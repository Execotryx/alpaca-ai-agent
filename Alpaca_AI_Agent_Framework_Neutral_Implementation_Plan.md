# Alpaca AI Agent — .NET 10 and PostgreSQL Durable Workflow Implementation Plan

**Document version:** 7  
**Updated:** 29 August 2026  
**Version 7 change:** resolves workflow-template, lifecycle, persistence, replacement, scope, phase-order, and test-governance contradictions; removes duplicated normative requirements by assigning each rule one authoritative section and using traceability references elsewhere.

## 1. Purpose

This document defines the platform-specific implementation plan for an autonomous Alpaca paper-trading agent entered in **Income & Portfolio Overlay Agents (Track 4)**.

The MVP is a conservative covered-call overlay on approved long-equity or ETF holdings. An ETF may be treated as an individual approved holding; systematic ETF allocation or rotation across an ETF universe is deferred. Cash-secured puts, complete wheel transitions, multi-leg spreads, live-capital trading, model training, and unconstrained option structures are outside the MVP.

The system is implemented as a **durable workflow**. The workflow as a whole is the autonomous trading agent. AI agents are used only for bounded semantic work that materially benefits from reasoning over conflicting structured signals and unstructured evidence. Broker truth, data validation, calculations, candidate construction, ranking, portfolio selection, risk approval, position sizing, execution, reconciliation, and lifecycle safety remain deterministic.

The design must:

- operate autonomously within bounded paper-trading cycles;
- use Alpaca-supported market-data and trading interfaces;
- use AI materially in every eligible new-entry evaluation that reaches the policy-assessment stage;
- prevent AI from constructing or submitting executable orders;
- prefer no trade or safe deferral when evidence, state, or controls are insufficient;
- record every workflow input, agent assessment, validation result, action, outcome, and error;
- support exact deterministic replay and separate AI stability evaluation;
- preserve domain-level portability behind application-owned interfaces while using the selected .NET and PostgreSQL infrastructure explicitly.

### 1.1 Selected implementation platform

The MVP shall use:

- **C# on .NET 10 LTS** for the deterministic domain, workflow kernel, adapters, worker, control API, replay, and tests;
- **PostgreSQL** as the authoritative workflow, audit, decision, intent, outbox, reconciliation, and lifecycle store;
- an **ASP.NET Core Worker Service** using `BackgroundService` for due-step claiming, workflow execution, outbox dispatch, reconciliation, and scheduled cycle creation;
- a thin **ASP.NET Core control API** for health, audit queries, workflow status, pause controls, and the global kill switch;
- Alpaca's stable official **`Alpaca.Markets`** SDK behind an application-owned adapter, with narrowly scoped typed `HttpClient` fallbacks for required endpoints missing from the pinned stable SDK;
- **Npgsql** as the PostgreSQL driver, EF Core migrations for schema evolution, and explicit Npgsql transactions or targeted SQL for lease, outbox, append-only, and uniqueness-critical operations;
- `System.Text.Json` source-generated serializers plus versioned JSON Schema artifacts for external and persisted contracts;
- an application-owned `IPolicyAssessor` port, with a thin Microsoft Agent Framework `ChatClientAgent` in Infrastructure as the preferred candidate and a direct-SDK candidate evaluated before one production path is frozen;
- an initial OpenAI provider path through the stable Agent Framework connector or the official OpenAI .NET `ResponsesClient`, with strict Structured Outputs and a direct-`ResponsesClient` fallback if the integration spike finds that the agent wrapper obscures request control, failure mapping, or statelessness;
- pinned agent-runtime, connector, provider-SDK, prompt, schema, and model versions, with requested and resolved provider/model identities recorded for every attempt;
- no Python or separate AI service for the MVP;
- OpenTelemetry and structured .NET logging for operational telemetry, with the append-only PostgreSQL audit record remaining authoritative;
- xUnit, property-based invariant tests, Testcontainers for PostgreSQL, and recorded or stubbed Alpaca/model-provider fixtures.

The MVP shall **not** depend on Temporal, Durable Task, Dapr Workflow, Step Functions, a message broker, Kubernetes, or a general-purpose workflow engine. It shall also not use Agent Framework Workflows or hosting, an OpenAI Agents SDK Python/TypeScript sidecar, agent sessions or memory, handoffs, MCP, hosted tools, computer use, code execution, A2A, or managed persistent agents. The thin agent runtime is only an invocation and structured-response boundary for node 6; it is not the workflow engine. The application owns a small PostgreSQL-backed workflow kernel implementing only the 18 nodes in Section 5.1 and the cycle types in Section 5.2.

Use ordinary framework-dependent .NET deployment for the MVP. Native AOT is deferred because the worker is long-running and AOT adds compatibility risk around SDKs, serialization, and reflection without addressing a measured bottleneck.

## 2. Formal Problem Statement

Given:

- a paper-trading account;
- authoritative account positions, cash, buying power, permissions, open orders, and activities;
- an approved universe of optionable long holdings;
- timestamped underlying and option-market data;
- bounded news, filing, earnings, and macro-event evidence;
- immutable outer safety limits;
- versioned policy-specific inner selection profiles;

the `NEW_ENTRY` workflow template shall periodically:

1. start a bounded evaluation or management cycle;
2. reconstruct and persist the authoritative broker state;
3. validate data completeness, consistency, provenance, and freshness;
4. compute deterministic market, volatility, liquidity, event, and portfolio features;
5. retrieve a bounded textual context set from approved sources;
6. obtain a schema-valid, evidence-linked AI overlay-policy assessment;
7. distinguish valid AI abstention from AI or system failure;
8. map an accepted call policy to an immutable, versioned deterministic selection profile;
9. generate structurally covered candidates inside the approved strategy template;
10. apply deterministic candidate admissibility checks;
11. score and rank admissible candidates reproducibly;
12. construct a proposed portfolio action set;
13. validate the complete action set through a final deterministic portfolio risk gate;
14. persist approved order intents before any external write;
15. submit, monitor, cancel, and reconcile paper orders idempotently;
16. initialize or evaluate durable management obligations; subsequent position-management cycles continue until closed, expired, assigned, or otherwise reconciled;
17. produce an evidence-based explanation and evaluation record.

New entries shall safely defer whenever account state, market data, AI output, evidence grounding, or deterministic approval is missing, stale, contradictory, invalid, or insufficient. Deterministic management of existing obligations may continue during AI failure only when its own required account and market inputs remain valid.

## 3. Scope

### 3.1 MVP

- Paper-trading covered calls on approved equities or individually approved ETFs.
- At most one short-call contract per complete block of 100 verified, unencumbered shares.
- Share coverage reduced by existing short calls, pending option orders, pending share-sale orders, and other reservations.
- Explicit entry, hold, profit-taking, close, expiration, assignment-risk, and failure behavior.
- One required combined **Market Context and Overlay Policy Agent** in the initial implementation.
- Deterministic, fact-linked explanation templates for human-readable reports.
- Interfaces that allow the combined agent to be split later into Evidence Extraction and Overlay Policy agents without redesigning deterministic workflow stages.
- Replay and simulation of expiration and assignment.
- Live paper demonstration of entry plus at least one close or expiration path.

### 3.2 Deferred extensions

- cash-secured puts;
- covered-call and cash-secured-put wheel transitions;
- systematic ETF portfolio allocation or rotation;
- multi-leg option structures;
- portfolio hedging strategies outside the covered-call overlay;
- live-capital trading;
- model training or fine-tuning;
- native order replacement, rolling, an AI Explanation Agent, and a Grounding Critic; these are post-MVP extensions and do not gate MVP completion.

### 3.3 Non-goals

- predicting every market movement;
- maximizing premium without accounting for assignment, capped upside, and downside exposure;
- replacing arithmetic, state reconciliation, or safety checks with natural-language reasoning;
- treating paper fills or short evaluation periods as proof of durable profitability;
- allowing any workflow step or agent to modify immutable outer safety limits at runtime;
- allowing an agent to invent executable strategies or risk limits;
- giving any agent broker write access.

## 4. Authority Model and Invariants

The system must preserve this authority order:

1. broker/account truth;
2. immutable safety and risk invariants;
3. versioned strategy policy and outer limits;
4. validated inputs and deterministic calculations;
5. bounded AI evidence and overlay-policy assessment;
6. deterministic policy mapping, candidate generation, ranking, and portfolio selection;
7. deterministic final portfolio risk approval;
8. persisted, approved order intent;
9. constrained execution and broker reconciliation;
10. reporting and narrative.

No lower layer may override a higher layer.

Required invariants:

1. Every short call is covered by broker-verified, unencumbered shares.
2. No new order is submitted using stale account, quote, option-chain, or session state.
3. AI cannot select a symbol outside the evaluated input, invent a structure, change a limit, size a position, construct an order, or access broker write tools.
4. Every field in an AI-selected profile must be inside or equal to its immutable outer strategy and safety limit; at least one field may be tighter, but mathematical strict-subset status is not required.
5. A valid no-trade result is preferable to manufactured activity.
6. Re-running a cycle cannot create duplicate intents, orders, or lifecycle actions.
7. Submission success is not treated as a fill; broker-reported state is reconciled.
8. Every broker-visible or other non-idempotent external side effect is preceded by a durable checkpoint and stable idempotency key. Reads, model calls, evidence retrieval, telemetry, and alerts use bounded attempts and correlation records but do not use the trading outbox unless they are themselves non-idempotent.
9. Every agent call has bounded context, tools, duration, retries, and output size.
10. Agent outputs are untrusted until deterministic schema and policy validation succeeds.
11. Agent attempts are append-only records; retries do not overwrite prior attempts.
12. AI failure blocks normal new entries but cannot prevent safe deterministic management of existing obligations.
13. The workflow terminates every cycle as completed, no action, safely deferred, or failed and escalated; every applicable step is terminal and every inapplicable step is explicitly `SKIPPED`.
14. PostgreSQL is the source of truth for workflow progress; in-memory task state is disposable and never sufficient for recovery.
15. A workflow step may be processed more than once after failure, but database effects and broker-visible actions must be effectively once through unique constraints, stable idempotency keys, and reconciliation.
16. No external network call occurs while a PostgreSQL transaction is held open.
17. A state change and every outbox message caused by that change are committed in one PostgreSQL transaction.
18. A worker may finalize a claimed step only while holding its current lease owner and fencing token.

Sections 4 through 8 are the normative architecture. Later phases, tests, scenarios, and completion criteria trace to these rules and must not redefine them. If later wording conflicts with Sections 4 through 8, the conflict is a plan defect that must be corrected before implementation.

## 5. Workflow Architecture

### 5.1 End-to-end workflow

| Step | Workflow node | Implementation | Output |
| ---: | --- | --- | --- |
| 1 | Start cycle | Deterministic | Cycle identity, type, deadline, budgets |
| 2 | Reconcile broker state | Deterministic | Authoritative account and obligation snapshot |
| 3 | Validate data quality | Deterministic | Valid snapshot or safe deferral |
| 4 | Calculate features | Deterministic | Versioned market, portfolio, liquidity, volatility, and event features |
| 5 | Retrieve bounded context | Deterministic retrieval | Approved evidence set with provenance and timestamps |
| 6 | Assess context and select policy | Required stateless narrow AI agent behind `IPolicyAssessor`; `ChatClientAgent` or direct-SDK fallback | Evidence-linked allowlisted policy assessment |
| 7 | Validate agent result | Deterministic | Accepted assessment, valid abstention, or explicit agent failure |
| 8 | Map policy profile | Deterministic | Versioned inner parameter bands and ranking preferences |
| 9 | Generate candidates | Deterministic | Structurally covered covered-call candidates |
| 10 | Apply admissibility checks | Deterministic | Admissible candidates and ordered rejection reasons |
| 11 | Score and rank | Deterministic | Ranked candidates |
| 12 | Select proposed action set | Deterministic | Zero or more proposed portfolio actions |
| 13 | Approve portfolio risk | Deterministic | Short-lived approval for the exact action set or rejection |
| 14 | Persist order intents | Deterministic | Durable, idempotent intents |
| 15 | Execute and reconcile | Deterministic | Broker-confirmed order and fill state |
| 16 | Initialize or evaluate lifecycle | Deterministic state machines | Durable management obligation or current-cycle lifecycle action; entry cycles never wait for a weeks-long position lifecycle |
| 17 | Explain outcome | Deterministic fact-linked templates | Human-readable explanation |
| 18 | Finalize audit record and metrics | Deterministic | Replayable decision and evaluation records; checkpoints were recorded at the side-effect boundaries that required them |

### 5.2 Cycle types

- pre-market preparation;
- normal new-entry evaluation;
- open-order monitoring;
- position management;
- end-of-day reconciliation;
- replay or simulation;
- periodic performance reporting.

Each cycle declares its preconditions, maximum duration, agent-attempt budget, external-call budget, maximum actions, checkpoint rules, terminal outcomes, and one workflow template. Model tools remain fixed at zero.

| Cycle template | Applicable nodes | Routing rule |
| --- | --- | --- |
| `NEW_ENTRY` | 1-15, 17-18; node 16 only when a fill creates or changes an obligation | Nodes 4-15 may become `SKIPPED` after validation, abstention, rejection, or no-action. A fill registers a separate durable management schedule; it does not keep this cycle open. |
| `OPEN_ORDER_MONITOR` | 1-3, 15-18 | Nodes 4-14 are `SKIPPED`; reconciliation and any lifecycle consequence are deterministic. |
| `POSITION_MANAGEMENT` | 1-3, 9-10, 12-18 | Nodes 4-8 and 11 are `SKIPPED`. A deterministic lifecycle proposal is generated at node 9, checked at node 10, then goes through selection, exact-set risk approval, intent, execution, and reconciliation. |
| `PRE_MARKET` | 1-5, 17-18 | Nodes 6-16 are `SKIPPED`; this template prepares data/context and creates no exposure. |
| `END_OF_DAY` | 1-3, 15-18 | Nodes 4-14 are `SKIPPED`; this template reconciles broker truth and creates no exposure. |
| `REPLAY_SIMULATION` | The recorded source template's applicable nodes | Live broker, evidence, and model ports are forbidden in frozen replay. |
| `PERFORMANCE_REPORT` | 1, 17-18 | All trading nodes are `SKIPPED`. |

Node 1 is scheduler-owned: the cycle-creation transaction records node 1's immutable start attempt/result and activates the first applicable successor. A worker therefore normally first claims node 2. A step is `PENDING`, `RUNNING`, `COMPLETED`, `SKIPPED`, `SAFELY_DEFERRED`, or `FAILED`; `COMPLETED`, `SKIPPED`, `SAFELY_DEFERRED`, and `FAILED` are terminal for that step.

### 5.3 Deterministic control flow

The workflow controller owns all durable state. Agents receive immutable snapshots and return typed results. Agents do not mutate workflow state, retain cross-cycle conversational memory, or communicate directly with one another.

The controller must implement:

- stable step identifiers;
- persisted step inputs and outputs;
- timeout and cancellation handling;
- retry classification;
- idempotency for every externally visible action;
- restart from the last durable checkpoint;
- mutual exclusion or lease semantics for overlapping cycles;
- manual pause, symbol pause, strategy pause, and global kill switch.

### 5.4 Persisted workflow kernel

The workflow controller is a small C# state-transition kernel backed by PostgreSQL. It is not a generic workflow product. The 18 workflow nodes are stable identifiers defined in code. Each cycle persists all 18 identifiers so audit shape is stable, but executes only the nodes applicable to its template; the others are persisted as `SKIPPED` with a routing reason.

The minimum physical records are:

| Record/table | Purpose | Required constraints |
| --- | --- | --- |
| `workflow_cycles` | Cycle identity, type, status, budgets, versions, deadline, and terminal outcome | Primary key on `cycle_id`; unique scheduled-cycle key; terminal states immutable |
| `workflow_steps` | One persisted instance of each required node for a cycle, including status, due time, input/output references, and dependency state | Unique `(cycle_id, step_id)`; stable status transition checks |
| `workflow_step_attempts` | Immutable claim/start record: owner, fencing token, database claim time, input references, and component versions | Unique `(cycle_id, step_id, attempt_number)`; insert only |
| `workflow_step_attempt_results` | Immutable completion, retry, deferral, cancellation, lost-lease, or failure observation for one attempt | Unique result identity; partial unique index for one authoritative final result per attempt; diagnostic late-result events remain insert-only |
| `workflow_checkpoints` | Immutable pre-side-effect and phase checkpoints | Unique checkpoint identity and content/version hash |
| lease columns on `workflow_steps` and `outbox_messages` | Time-bounded ownership of due work without a separate coordination service | Monotonic fencing token; database-time expiration; one active owner |
| `outbox_messages` | Durable requests for broker writes, reconciliation, alerts, and other post-commit work | Unique deduplication key; due-time and status indexes; append-only payload |
| `order_intents` | Approved executable intent created before broker access | Unique stable `intent_key`; unique Alpaca `client_order_id`; immutable executable fields after readiness |
| `broker_operations` | Immutable broker request-attempt records for submit, query, cancel, reconcile, or activity poll | Insert only; linked to intent and outbox message |
| `broker_observations` | Immutable response, timeout, stream, REST, activity, and recovery observations | Insert only; normalized projection is derived from ordered observations |
| `share_reservations` | Physical coverage claims bound to a cycle, intent, holding, and share block | Unique active share-block identity; quantity and lifecycle state checks; created atomically with intent/outbox and released only from broker truth |
| `agent_attempts` | Immutable model-call start/configuration record inserted before the provider call | Unique attempt identity; insert only |
| `agent_attempt_results` | Immutable provider outcome, usage, validation, timeout, cancellation, or lost-response observation | Insert only; partial unique index for one authoritative terminal result per attempt; diagnostic late observations remain insert-only |
| `domain_events` | Append-only audit timeline and projection source | Unique event ID and correlation ID; immutable payload and component versions |
| snapshot and decision tables | Raw/normalized account, market, evidence, AI, candidate, risk, and decision records | Immutable versioned rows referenced by identifiers, not overwritten blobs |

The worker claims due steps in a short transaction using PostgreSQL row locking such as `FOR UPDATE SKIP LOCKED`, sets a bounded lease, increments the fencing token, appends the immutable attempt-start row, and commits. Work then runs outside the transaction. Finalization uses a new short transaction that verifies the current owner and fencing token, inserts the authoritative attempt-result row, stores immutable output, advances dependent steps, writes domain events, and inserts any outbox messages atomically.

Expired leases make work eligible for another worker. A stale worker whose fencing token no longer matches may store diagnostic evidence but cannot advance the workflow or emit a new side effect.

### 5.5 Transactional outbox and broker-write protocol

The transactional outbox separates committed workflow decisions from network side effects:

1. The final risk gate produces an approval for an exact action set and exact snapshot versions.
2. In one PostgreSQL transaction, the application creates immutable `order_intents`, assigns stable intent keys and Alpaca `client_order_id` values, records the pre-write checkpoint, inserts `SUBMIT_ORDER` outbox messages, and advances the workflow to waiting-for-execution.
3. The dispatcher claims a due outbox message with a lease and fencing token.
4. In a short transaction, it inserts an immutable broker request-attempt record and changes the intent from `READY` to `SUBMISSION_PENDING`; it then commits.
5. It calls Alpaca outside the transaction using the stored `client_order_id`.
6. A received response is inserted as an immutable broker observation in a new transaction and the normalized projection becomes accepted, rejected, or reconciliation-required; the outbox item is finalized only in the same transaction.
7. If the process dies, times out, loses the response, or loses its lease after step 4, recovery treats the stale `SUBMISSION_PENDING` intent as `SUBMITTED_UNKNOWN`.
8. `SUBMITTED_UNKNOWN` must query Alpaca by `client_order_id` and inspect relevant open/closed orders and account activities before any resubmission. It may become accepted, rejected, filled, canceled, still unknown, or safely halted.
9. A retry is allowed only after the reconciliation policy proves that no broker order exists, the approval and market state are still valid, and the same stable `client_order_id`/intent identity remains safe to use. Blind resubmission is forbidden.

The same outbox pattern applies to cancellation. The MVP does not issue Alpaca replace requests. A user or lifecycle request to change an order becomes: cancel request, broker-confirmed cancellation or other terminal reconciliation, a newly evaluated and approved action, and a new intent/client order identity. Externally observed `pending_replace` or `replaced` statuses are still normalized safely because another actor or broker process may produce them.

### 5.6 Runtime topology and solution boundaries

Begin as a modular monolith with these .NET projects:

```text
src/
  AlpacaAgent.Domain/          # contracts, state machines, features, ranking, risk
  AlpacaAgent.Application/     # ports, workflow-node handlers, use cases
  AlpacaAgent.Persistence/     # PostgreSQL schema, repositories, leases, outbox
  AlpacaAgent.Infrastructure/  # Alpaca, evidence, Agent Framework/OpenAI, clock adapters
  AlpacaAgent.Worker/          # schedulers, claim loops, dispatchers, reconciliation
  AlpacaAgent.Api/             # health, audit, control and demonstration endpoints
tests/
  AlpacaAgent.UnitTests/
  AlpacaAgent.ContractTests/
  AlpacaAgent.PersistenceTests/
  AlpacaAgent.FailureInjectionTests/
  AlpacaAgent.ReplayTests/
  AlpacaAgent.PaperTests/
schemas/
fixtures/
deploy/
```

The Worker and API may share one process for the first demonstration, but they remain separate modules. PostgreSQL is the only mandatory infrastructure service. Multiple worker instances are not required for the MVP, but lease and fencing semantics must remain correct so restart recovery does not depend on single-process memory.

`AIAgent`, `ChatClientAgent`, `IChatClient`, connector types, and provider-SDK types remain inside Infrastructure. Domain and Application see only immutable application-owned contracts and `IPolicyAssessor`. The agent runtime holds no authoritative workflow state, conversation history, thread, session, or previous-response identifier; PostgreSQL remains the sole durable authority.

### 5.7 Normal operational flows

The following flows are the reference paths against which unit tests, replay fixtures, and later integration tests are written. They are not merely demonstration scripts; each branch has a declared terminal outcome and persisted evidence.

#### 5.7.1 Normal new-entry flow

1. The scheduler creates one due `NORMAL_NEW_ENTRY` cycle from its unique schedule key.
2. A worker claims node 2 with a lease and fencing token, then reconstructs account, positions, open and relevant closed orders, activities, option permissions, market clock/calendar, underlying data, and the complete required option-chain pages.
3. Validation accepts only a complete, internally consistent, fresh snapshot. Reconciliation computes verified free whole-share coverage after existing short calls, pending option orders, pending share sales, and other reservations.
4. Pure calculators produce versioned features. Deterministic retrieval produces a bounded, allowlisted, deduplicated evidence bundle whose publication/event times do not exceed the decision-time availability boundary.
5. The stateless policy agent receives the immutable input package and returns one strict `PolicyAssessmentV1`. Deterministic validation accepts the schema, supplied evidence identifiers, symbol/input scope, frozen runtime/model versions, confidence rules, and exactly one allowlisted policy.
6. A call policy maps to one immutable inner profile. Deterministic code generates structurally covered candidates, rejects inadmissible contracts, ranks the survivors with stable tie-breaking, and selects a non-overlapping action set.
7. The portfolio gate evaluates the complete set against the exact reconciled account and market versions. An approval is short-lived and bound to the action-set hash and every relevant snapshot version.
8. Immediately before submission, the workflow rechecks quote freshness, coverage, permissions, reservations, pauses, kill switch, and approval validity.
9. In one transaction it persists the exact decision, checkpoint, immutable intent, share reservation, domain event, and `SUBMIT_ORDER` outbox item. Only then may a dispatcher call Alpaca with the stored stable `client_order_id`.
10. Submission response, streaming order events, REST order lookup, positions, and activities are normalized into append-only broker observations. Acceptance is not a fill. Partial and terminal states remain under reconciliation.
11. When a short call position is broker-confirmed, the entry cycle atomically registers the lifecycle obligation and schedules a separate `POSITION_MANAGEMENT` cycle. The entry cycle then ends with a persisted terminal result and metrics; later cycles own the obligation until close, expiration, or assignment is confirmed.

#### 5.7.2 Normal no-action and safe-deferral flows

- `SKIP` is a valid AI-completed no-trade outcome. It produces no candidates or order intent and ends as `NO_ACTION`.
- `INSUFFICIENT_INFORMATION` is a valid grounded AI abstention. It produces no candidates or order intent and ends as `SAFELY_DEFERRED` with the missing-information evidence.
- Agent timeout, refusal, rate limit, truncation, invalid output, or configuration drift is an operational failure, not abstention. It blocks new entries and ends as `SAFELY_DEFERRED` or `FAILED_AND_ESCALATED` according to retry budget.
- Deterministic rejection after a valid call policy is a normal result. The exact rule failures are persisted and no intent is created.
- A management cycle for an existing obligation does not wait for the policy agent. It proceeds only with valid broker/account/market inputs and the deterministic lifecycle rules.

#### 5.7.3 Normal open-order and lifecycle flow

1. Reconcile the order by broker order ID and `client_order_id`, then merge streaming events with REST order and activity truth.
2. Normalize raw broker statuses through Section 8.2, including the externally observed replace-status rule in Section 5.5.
3. Recompute reservations from cumulative filled quantity and remaining broker-visible obligation; duplicated or out-of-order observations cannot reduce safety.
4. A cancel acknowledgment is not finality. Reconcile the race in which the order fills before cancellation completes. If an externally initiated replacement is observed, reconcile both identities without creating or assuming a successor.
5. For a filled short call, retain coverage until broker activities confirm close, expiry, or assignment. Poll REST activities for assignment and expiration because option non-trade activities are not guaranteed through the order WebSocket path.
6. Schedule the next lifecycle review durably; never depend on an in-memory timer.

### 5.8 Investigated edge-case model

The implementation must use safe defaults for the following edge classes. Edge handling is deterministic unless the row explicitly concerns evaluation of the narrow AI output.

| Area | Edge case | Required behavior |
| --- | --- | --- |
| Scheduling | Duplicate tick, delayed tick, overlapping new-entry and management cycles | Unique schedule key; serialize conflicting portfolio writes; management has priority over new exposure. |
| Time | Holiday, early close, daylight-saving transition, local clock skew | Use Alpaca clock/calendar plus UTC instants and database time; never infer the session from machine-local time. |
| Market data | Missing page token, partial option-chain pagination, feed downgrade, stale source time with fresh retrieval time | Mark the snapshot incomplete or policy-ineligible; retrieval time never refreshes stale market truth. |
| Market data | Zero/negative bid or ask, crossed market, locked market, absent quote, out-of-order observation, unknown numeric text | Reject or defer under the declared quote-quality policy; never coerce unknown to zero. |
| Market state | Halt, volatility pause, auction-only state, or options market closed while equities are open | Block new option orders and persist the exact market-state reason. |
| Coverage | 99.999 fractional shares, non-standard option multiplier/deliverable, adjusted contract, pending stock sale, existing call, or pending sell-to-open | Count only verified unencumbered whole shares and standard MVP contracts; adjusted/non-standard contracts force management-only review. |
| Account | Options level below 1, trading blocked, insufficient options buying power, account restricted/closed, configuration changed mid-cycle | Reject before intent; invalidate any prior approval. |
| Concurrency | Two approvals reserve the same 100-share block | Only one reservation commits; the loser re-reconciles and receives no intent. |
| Corporate actions | Split, reverse split, merger, spinoff, symbol/CUSIP change, special dividend, broker-canceled GTC order | Pause affected new entries, ingest equity and option corporate-action activity, invalidate stale contracts/reservations, and reconcile successor symbols and deliverables. |
| Candidate maths | Boundary DTE, strike, spread, delta, volume, open interest, premium, or rounding value | Use documented inclusive/exclusive boundaries and invariant-culture decimal conversion; a value exactly on a threshold has one tested outcome. |
| Portfolio | Individually valid trades collectively exceed coverage, concentration, expiration, action-count, or daily limits | Node 12 may deterministically choose a smaller set before approval. Node 13 approves or rejects the exact submitted set and never mutates it. A rejected set can cause a new deterministic selection pass or later cycle, never a patched approval. |
| Approval | Quote, position, permission, policy version, kill switch, or reservation changes after approval | Invalidate approval and re-run the affected deterministic stages; never patch an old approval. |
| Submission | HTTP timeout, disconnect, 429, 5xx, malformed body, response lost after broker acceptance | Enter `SUBMITTED_UNKNOWN` when outcome may be ambiguous; reconcile before any retry. Honor bounded retry timing only for proven read-safe operations. |
| Submission | Duplicate `client_order_id`, request replay, process crash at any pre/post-call boundary | Reuse the stable identity, query by client ID, and produce at most one broker-visible order or a safe halt. |
| Order events | Duplicate, out-of-order, missing, corrected, or busted fills; unknown future broker status | Preserve append-only observations, reconcile cumulative quantities against REST/activities, and route unknown status to reconciliation rather than assuming terminality. |
| Cancel or externally observed replace | Fill wins race with cancel; external actor initiates replacement; cancel rejected | Apply Section 5.5: reconcile all observed identities and retain coverage until broker truth permits a separately approved new intent. |
| Partial fill | Some contracts fill and the rest remain open, expire, or are canceled | Reserve coverage for filled obligations plus working remainder; never resubmit the full original quantity. |
| Assignment | Early assignment on any eligible day, especially around ex-dividend; assignment during a trading halt | Treat assignment as possible throughout the short call's life; poll activities and reconcile shares before any new coverage calculation. |
| Expiration | Exactly $0.01 ITM, price oscillation near strike, expiration processing delay, weekend/holiday delay | Do not predict final disposition from the last quote; hold the obligation in pending confirmation until broker activities and positions agree. |
| Paper environment | Unrealistic fills, absent market impact/queue position/dividends, random partial fills, next-day paper NTA visibility | Label results as simulation; do not infer live fill quality or immediate activity availability from paper behavior. |
| Evidence | Duplicate syndication, stale article, future publication, timezone ambiguity, unavailable/paywalled body, oversized or invalid encoding | Deduplicate deterministically, enforce decision-time availability, preserve missingness, and cap content without inventing text. |
| Evidence | Prompt injection, quoted instructions, false ticker mention, unsupported claim, source conflict | Treat all retrieved text as data; validate symbol and evidence scope; require conflicts/opposing evidence to remain visible. |
| Agent | Malformed JSON, extra field, unknown policy/evidence ID/symbol, executable field, contradictory confidence, missing material citation | Reject deterministically and record the specific failure; no repair agent or silent coercion. |
| Agent | Refusal, truncation, timeout, cancellation, rate limit, provider/model substitution, tool request, session identifier | Classify operational failure, prohibit silent fallback, and defer new entries. |
| Agent stability | Semantically identical evidence reordered or duplicated changes policy materially | Record stability failure; configuration cannot be promoted until the frozen threshold passes. |
| Persistence | Deadlock, serialization failure, lost lease, expired fencing token, transaction rollback after handler success | Retry only the safe database unit; stale workers cannot finalize or emit outbox work. |
| Schema/replay | Old JSON version, unknown enum, unordered database result, culture/timezone change, hash mismatch | Deserialize or explicitly migrate; canonicalize ordering/serialization; otherwise fail replay visibly. |
| Controls | Pause or kill switch activates between selection and external write | Final pre-write check wins; create no broker call and retain an auditable canceled/safe-halt outcome. |

Research basis for these flows and edges:

- Alpaca order lifecycle and uncommon statuses: <https://docs.alpaca.markets/docs/orders-at-alpaca>
- order lookup by `client_order_id`: <https://docs.alpaca.markets/reference/getorderbyclientorderid>
- externally observed replace-status semantics: <https://docs.alpaca.markets/reference/patchorderbyorderid-1>
- option trading, assignment/expiry activities, and lack of WebSocket assignment events: <https://docs.alpaca.markets/docs/options-trading>
- option level, coverage, buying-power, and regular-hours constraints: <https://docs.alpaca.markets/docs/options-trading-overview>
- option-chain feed and pagination behavior: <https://docs.alpaca.markets/reference/optionchain>
- paper fill assumptions and limitations: <https://docs.alpaca.markets/docs/paper-trading>
- calendar early closures: <https://docs.alpaca.markets/reference/legacycalendar>
- corporate-action effects and option activity types: <https://docs.alpaca.markets/docs/activities>
- assignment may occur before expiration and dividend risk is higher before the ex-dividend date: <https://www.finra.org/investors/insights/trading-options-understanding-assignment> and <https://www.finra.org/investors/insights/options-z-basics-greeks>

## 6. Narrow-Scoped Agents

### 6.1 MVP agent: Market Context and Overlay Policy Agent

The initial implementation uses one combined required agent to minimize contracts, latency, failure modes, and evaluation work. Its input and output contracts are `PolicyInputPackageV1` and `PolicyAssessmentV1`. Each attempt is one stateless request with zero tools, no retained conversation, and developer instructions kept separate from evidence, which is always treated as untrusted data.

Allowed inputs:

- validated structured features;
- bounded textual evidence from approved sources;
- a limited, read-only portfolio summary;
- definitions of the allowlisted policies;
- explicit evidence, recency, and uncertainty requirements.

Required output:

- supporting factors;
- opposing factors;
- unresolved conflicts;
- relevant events and materiality;
- missing or unreliable information;
- source references for material claims;
- confidence declaration and uncertainty category;
- exactly one allowlisted policy recommendation.

Allowed policies:

| Policy | Workflow interpretation |
| --- | --- |
| `SKIP` | Valid no-trade decision for the current context |
| `CONSERVATIVE_CALL` | Use the conservative inner selection profile |
| `BALANCED_CALL` | Use the balanced inner selection profile |
| `INCOME_CALL` | Use the income-oriented inner profile inside unchanged outer limits |
| `INSUFFICIENT_INFORMATION` | Valid, grounded AI abstention and safe deferral |

`MANAGE_EXISTING` is not an AI policy. Existing positions are managed by deterministic lifecycle state machines.

The agent is prohibited from returning executable order fields, including contract identifiers, strikes, expirations, quantities, sides, order types, prices, time-in-force values, or broker operations.

### 6.2 Runtime candidates and freeze decision

The preferred MVP candidate is a thin Microsoft Agent Framework `ChatClientAgent` backed by `Microsoft.Extensions.AI.IChatClient`. Install only the minimal stable packages required for that path. The framework is used for model invocation and typed response handling, not orchestration, hosting, memory, tools, handoffs, or durable state. The production path is not selected until the Phase 6 spike below passes.

Before the production adapter is frozen, run a narrow integration spike that sends the same `PolicyInputPackageV1` and expects the same strict `PolicyAssessmentV1` schema through:

1. `ChatClientAgent` with the stable OpenAI connector; and
2. the official OpenAI .NET `ResponsesClient` directly.

Choose the direct `ResponsesClient` adapter if the wrapper prevents exact request inspection, reliable timeout/cancellation, complete usage and response metadata capture, deterministic failure classification, strict statelessness, or delivery within the Phase 6 timebox. Both paths implement the same `IPolicyAssessor` contract, stay in Infrastructure, and must pass the same contract and evaluation suite.

### 6.3 Provider and model policy

OpenAI is the initial provider path. Evaluate GPT-5.6 Terra as the preferred combined-agent candidate and GPT-5.6 Luna as its lower-latency combined-agent challenger on the frozen labelled set; freeze the winning model and reasoning setting before shadow mode. GPT-5.6 Sol is reserved for offline critic or judge experiments and must never silently enter the production decision path. Extraction/explanation-specific model evaluation belongs to post-MVP extensions.

Azure/Foundry, Anthropic, Gemini, and local Ollama-compatible implementations remain challenger adapters behind `IPolicyAssessor`. Local models on current hardware may be evaluated for extraction, explanation, offline replay, or degraded offline analysis, but cannot enter the live decision path until they pass the identical schema, grounding, abstention, stability, latency, and safety gates. Provider or model changes are explicit, versioned policy changes; silent fallback is forbidden.

### 6.4 Invocation and validation rules

- Require strict JSON Schema with all fields required, `additionalProperties: false`, bounded strings and arrays, allowlisted enums, and evidence identifiers rather than copied source text.
- Give the production agent zero tools. It receives no web, file, MCP, code-execution, database, or broker capability.
- Put trusted developer instructions and untrusted evidence in separate message/content boundaries. Retrieved text can supply evidence, never instructions.
- Validate deserialized output again in deterministic application code for schema, policy allowlists, symbol scope, forbidden executable fields, evidence-reference integrity, confidence rules, and input-version consistency.
- Persist every attempt append-only. A retry is a new attempt; do not invoke a free-form repair agent after invalid output.
- Record input-package hash, requested and resolved provider/model, reasoning setting, prompt/schema/runtime/connector versions, finish status, provider request identifier, token usage, timing, and deterministic validation result.

### 6.5 Optional later split

The combined contract may later be split into two narrow agents:

1. **Evidence Extraction Agent** — converts bounded sources into a structured evidence bundle.
2. **Overlay Policy Agent** — consumes validated features plus the evidence bundle and selects one allowlisted policy.

The split is permitted only when both agents remain stateless across cycles, use versioned schemas, and cannot bypass deterministic validation. Promote it only if the frozen evaluation set shows a measurable improvement in grounding or policy accuracy that justifies added latency, cost, and failure surface. The MVP shall not require the split.

### 6.6 Post-MVP Explanation Agent

Use deterministic explanation templates first. An optional Explanation Agent receives only a closed, completed, read-only fact table. It may produce pre-trade, rejection, no-trade, and post-trade narratives. Every stated fact must map to that table; discard the narrative on validation failure. It has no trading authority, cannot change stored facts, and cannot influence risk or execution.

### 6.7 Post-MVP Grounding Critic

A separate AI critic may first run offline against frozen assessments. Promote it only if labelled evaluation shows meaningful incremental detection beyond deterministic validators. In production it may only reject or defer an assessment. It cannot approve a trade and cannot replace deterministic validation of schemas, source allowlists, timestamps, symbols, or evidence references.

### 6.8 AI result versus AI failure

A valid AI result and an operational failure are different data types.

Valid assessment statuses:

- `COMPLETED` with a call policy;
- `COMPLETED` with `SKIP`;
- `COMPLETED` with `INSUFFICIENT_INFORMATION`.

Agent attempt failures include:

- `TIMEOUT`;
- `SERVICE_UNAVAILABLE`;
- `RATE_LIMITED`;
- `REFUSED`;
- `INCOMPLETE_OR_TRUNCATED`;
- `OUTPUT_SCHEMA_INVALID`;
- `POLICY_VALUE_INVALID`;
- `EXECUTABLE_FIELDS_PRESENT`;
- `GROUNDING_REJECTED`;
- `SOURCE_SCOPE_VIOLATION`;
- `LOW_CONFIDENCE_REJECTED`;
- `CANCELLED`.

Failures produce an operational safe deferral for new entries. They must not be rewritten as a schema-valid `INSUFFICIENT_INFORMATION` recommendation.

## 7. Core Data Contracts

All contracts must be represented as immutable C# records, machine-validatable at boundaries, serialized through source-generated `System.Text.Json` contexts, and versioned independently of their PostgreSQL representation.

### 7.1 Workflow cycle

- cycle identifier and type;
- scheduled and actual start time;
- current step and status;
- maximum duration, retry, agent-attempt, external-call, and action budgets; model tool budget fixed at zero;
- parent cycle or replay reference;
- policy, schema, workflow, prompt, and component versions;
- durable checkpoint reference;
- terminal outcome and reason.

### 7.2 Market snapshot

- observation identifier, source time, and retrieval time;
- market-session state;
- underlying trade and quote data;
- recent-history references;
- option-chain expirations, strikes, contract identifiers, quotes, volume, open interest, and available risk measures;
- contract multiplier, deliverable, adjusted-contract flag, active/tradable state, underlying identifier, and successor-contract references;
- authoritative corporate-action and dividend-calendar fields, including ex-dividend date, source time, retrieval time, and provenance;
- provenance and freshness status for each field group;
- explicit unknown and validation-failure states.

### 7.3 Account snapshot

- account identifier and timestamp;
- cash, buying power, options buying power, equity, account status, trading-blocked state, option permissions, and configuration version;
- positions and quantities;
- open orders and reserved quantities;
- existing option obligations;
- recent fills, cancellations, expirations, exercises, and assignments.

### 7.4 Reconciled portfolio

- holdings and verified quantities;
- free and reserved share quantities;
- pending order effects;
- open option obligations;
- reconciliation evidence;
- inconsistencies and safe-halt status.

### 7.5 Evidence bundle

- evaluated symbol and decision timestamp;
- deterministic evidence identifier and bundle content hash;
- source identity and allowlist status;
- source trust label and integrity-validation status;
- publication time, event time, and retrieval time;
- bounded excerpt or content reference;
- deterministic lexical symbol/time-scope relevance, duplicate, freshness, and availability flags;
- neutral source claims or excerpts without investment-direction labels;
- deterministic bundle-validation status.

The deterministic evidence builder owns provenance, integrity, time scope, lexical symbol scope, freshness, availability, deduplication, and bounded content. It does not classify evidence as supporting, opposing, or conflicting in investment meaning. Those semantic classifications belong only to the AI assessment and must reference supplied evidence identifiers.

### 7.6 AI policy assessment

The model-produced `PolicyAssessmentV1` contains only:

- the `PolicyInputPackageV1` content hash;
- supporting and opposing factors;
- explicit conflicts and unusual-event materiality;
- missing or unreliable information;
- exactly one allowed policy;
- declared confidence and uncertainty category;
- material claim-to-evidence references;
- no executable order fields.

The system-owned accepted-assessment envelope adds the assessment identifier, source attempt identifier, input snapshot/evidence references, and deterministic acceptance decision. Provider/runtime facts belong only to Section 7.7. The model cannot supply or self-attest operational evidence.

Do not call confidence calibrated unless a labelled calibration set, method, and evaluation result are defined. Otherwise use declared confidence.

### 7.7 Agent attempt and result records

- The immutable attempt-start record contains attempt identity, input references/versions/hash, requested provider/model and reasoning setting, prompt/schema/runtime/connector/provider-SDK versions, timeout boundary, and system-owned capability configuration proving zero tools and no session/thread/memory/handoff/previous-response chaining.
- The immutable attempt-result record contains its attempt reference, observation time, resolved provider/model, provider request identifier, finish status, token usage, timing, optional accepted-assessment reference, raw-output reference where safe, deterministic validation result, and any failure/deferral/retry classification.
- A retry inserts a new start record and later its result; neither record is updated. A late provider result may be diagnostic but cannot replace the authoritative workflow outcome after lease or deadline loss.

### 7.8 Strategy policy and policy profile

The strategy policy defines immutable outer limits. Each AI-selectable profile defines inner bands and ranking preferences.

- allowed structures and symbols;
- outer expiration, strike, quantity, liquidity, event, and exposure limits;
- inner profile parameter bands;
- ranking weights or ordered comparisons;
- entry, exit, and expiration rules;
- order pricing and retry rules;
- cycle, daily, and global stop limits;
- field-by-field proof that no inner profile value exceeds its corresponding outer limit.

### 7.9 Candidate

- candidate identifier;
- underlying and option contract;
- strategy type;
- required share coverage and allocation reference;
- premium, spread, break-even, capped-upside, assignment, and downside context;
- liquidity and event assessments;
- deterministic feature values;
- policy profile and evidence references;
- candidate-level warnings;
- candidate rationale and invalidation conditions.

The base Candidate does not contain a ranking score.

### 7.10 Ranked candidate

- candidate reference;
- score or ordered-comparison result;
- scoring-policy version;
- feature contributions;
- deterministic tie-break evidence;
- rank.

### 7.11 Proposed portfolio action set

- selected ranked candidates;
- combined quantity and share reservations;
- projected per-underlying, expiration, and portfolio exposure;
- rejected alternatives;
- no-trade or unable-to-decide result when applicable.

### 7.12 Portfolio risk decision

- exact proposed action-set reference;
- account, market, policy, and reconciliation references;
- ordered rule results with evidence and thresholds;
- projected state before and after all actions;
- approval or rejection;
- short-lived approval token and expiration;
- invalidation conditions.

### 7.13 Order intent

- stable idempotency key;
- approved action-set and risk-decision references;
- contract, side, quantity, order type, limit price, and time in force;
- submission deadline and cancellation rules;
- expected position effect;
- durable pre-write checkpoint.

### 7.14 Decision record

- workflow cycle and correlation identifiers;
- complete input references and versions;
- every agent attempt and accepted assessment;
- candidates considered and rejected;
- ranking and proposed action set;
- portfolio risk decision;
- persisted intent and broker request/response identifiers;
- order, fill, position, expiration, and assignment events;
- human intervention;
- final realized or unresolved outcome.

### 7.15 Workflow step attempt and result

- The immutable attempt-start record contains cycle/step identifiers, monotonically increasing attempt number, worker/lease/fencing data, scheduled/claimed/started times, database claim time, input references, and component versions.
- The immutable attempt-result record contains its attempt reference, observation time, authoritative-or-diagnostic classification, success/retry/failure/deferral/cancellation/lost-lease result, error/diagnostic reference, next due time, and output/checkpoint references.
- A stale worker may append a diagnostic late-result event, but only the current lease/fencing owner may insert the authoritative result that advances the step.

### 7.16 Outbox message

- outbox identifier and unique deduplication key;
- message type and version;
- aggregate/cycle/intent correlation identifiers;
- immutable payload or payload reference;
- status, due time, bounded attempt count, and terminal disposition;
- lease owner, fencing token, and lease expiry;
- created, claimed, dispatched, reconciled, and completed timestamps;
- related broker-operation and domain-event references.

### 7.17 Broker request attempt, observation, and reconciliation result

- operation and attempt identifiers;
- exact intent, `client_order_id`, order class, and expected position effect;
- operation type: submit, query, cancel, reconcile, or activity poll;
- immutable request-attempt data: request hash, safely stored request reference, start time, timeout boundary, and outbox/fencing identity;
- immutable observation data: safely stored response/event reference, response or observation time, broker identifiers, and source channel;
- normalized reconciliation result: accepted, rejected, filled, partial, canceled, absent, `SUBMITTED_UNKNOWN`, or unreconciled;
- broker order and activity identifiers when observed;
- reconciliation evidence and next permitted action;
- proof that no blind retry occurred.

## 8. State Machines

Do not combine unrelated state dimensions into one flat state machine.

### 8.1 Portfolio eligibility state

- no approved holding;
- approved but insufficient shares;
- eligible shares available;
- shares partially reserved;
- no free coverage;
- inconsistent or unreconciled;
- paused.

### 8.2 Order state

The domain state is independent of raw broker wording:

- `INTENT_PREPARED`;
- `SUBMISSION_PENDING`;
- `SUBMITTED_UNKNOWN`;
- `WORKING`;
- `PARTIALLY_FILLED`;
- `CANCEL_PENDING`;
- `FILLED`;
- `CANCELED`;
- `REJECTED`;
- `EXPIRED`;
- `REPLACED_EXTERNAL`;
- `RECONCILIATION_REQUIRED`.

| Raw broker status | Normalized domain state |
| --- | --- |
| `accepted`, `pending_new`, `new`, `held`, `accepted_for_bidding` | `WORKING` |
| `partially_filled` | `PARTIALLY_FILLED` |
| `pending_cancel` | `CANCEL_PENDING` |
| `filled` | `FILLED` |
| `canceled` | `CANCELED` |
| `rejected` | `REJECTED` |
| `expired` | `EXPIRED` |
| `pending_replace` | `RECONCILIATION_REQUIRED` with externally-initiated-replace reason |
| `replaced` | `REPLACED_EXTERNAL`; reconcile predecessor/successor identities |
| `done_for_day`, `stopped`, `suspended`, `calculated` | `RECONCILIATION_REQUIRED` with a due poll or escalation |
| Unknown future value | `RECONCILIATION_REQUIRED` plus alert; never infer success or terminality |

### 8.3 Option-position lifecycle state

- no position;
- entry order open;
- short call open;
- close review;
- close order open;
- near expiration;
- assignment possible;
- expired pending confirmation;
- assigned pending reconciliation;
- closed;
- exception or safe halt.

Transitions must be driven by persisted events and broker reconciliation. Rolling is excluded from the MVP. A roll request is rejected as out of scope; a post-MVP design may close the old obligation and then independently validate a new entry, never as one atomic transition.

## 9. Step-by-Step Implementation Plan

### Test-first execution rule for every phase

Behavioral production code must not be the first executable artifact for any slice. Use this order:

1. select the applicable test IDs from Section 10 and copy their IDs into the phase work item;
2. add or review the fixture and the unit test with the exact observable assertion;
3. run the focused test and record that it fails for the missing behavior, not because of a broken fixture;
4. implement the smallest production behavior that makes the focused test pass;
5. run the whole unit suite and the phase's later contract/persistence/failure-injection suites;
6. do not delete, skip, loosen, or rewrite an acceptance test merely to match an implementation; any test-contract change requires a reviewed plan/version change and rationale.

Tests assert public Domain/Application behavior and safety outcomes. They must not lock in private method structure, framework internals, SQL text, HTTP-client implementation details, or model prose. A test may use an in-memory fake port, fake clock, deterministic ID source, or recorded immutable fixture. PostgreSQL constraints, SDK/REST compatibility, and real broker behavior remain contract/integration tests and must not be falsely represented by mocks.

Use `xUnit` theories for boundary matrices and a property-based library for invariants. Test names use `UnitUnderTest_Condition_ExpectedOutcome`; every test carries a stable Section 10 ID through a trait or display-name prefix. Randomized tests must print and persist the seed on failure.

### Phase 0 — Freeze the experiment contract

1. Confirm Track 4 and the covered-call MVP.
2. Define the approved demonstration holdings and evaluation window.
3. Define the minimum number of completed decision cycles.
4. Define buy-and-hold, fixed-schedule covered-call, and deterministic-ablation baselines.
5. Use identical starting holdings, timestamps, fees, slippage, fill assumptions, and assignment treatment for every comparison.
6. Define success, failure, and safety thresholds before collecting results.
7. State the AI hypothesis: bounded synthesis of conflicting structured and textual evidence improves policy selection over a fixed deterministic overlay.
8. Define labelled scenario sets for conflict recognition, abstention, no-trade correctness, and grounding.
9. Record paper-environment and short-window limitations.

**Exit criterion:** the team can state what is traded, what AI changes, what remains deterministic, which baselines apply, and what counts as failure.

### Phase 1 — Specify separate state machines and policies

1. Formalize the three state machines in Section 8.
2. Define all allowed transitions, triggering events, invalid-transition handling, and safe stops.
3. Define covered-call eligibility, entry, hold, close, expiration, and assignment rules.
4. Specify that MVP roll requests are rejected as out of scope; preserve close-then-revalidate only as a post-MVP design rule.
5. Define immutable outer limits and policy-specific inner profiles.
6. Prove that every inner profile is a subset of the outer limits.

**Exit criterion:** every portfolio, order, and option-position state has a bounded next action or safe stop.

### Phase 2 — Define contracts and component boundaries

1. Formalize Section 7 as machine-validatable schemas, including explicit `EvidenceBundleV1`, `PolicyInputPackageV1`, `PolicyAssessmentV1`, and system-owned attempt/result artifacts.
2. Assign ownership and provenance for every field.
3. Represent unknown, invalid, unavailable, and zero as distinct values.
4. Version workflow, policy, profile, schema, feature, prompt, model, and decision logic independently.
5. Create fixtures for success, no trade, valid abstention, agent failure, rejection, partial fill, restart, expiration, and assignment.
6. Define C# ports for broker reads, broker writes, evidence retrieval, policy assessment, clock, persistence, workflow-node execution, outbox dispatch, and alerts; `IPolicyAssessor` belongs to Application.
7. Create immutable C# records and versioned JSON Schema artifacts for all Section 7 contracts.
8. Define the .NET solution/project boundaries in Section 5.6 and prohibit Agent Framework, `IChatClient`, connector, provider-SDK, and other infrastructure models from crossing into Domain or Application contracts.
9. Define PostgreSQL persistence mappings separately from the domain records, including JSONB payload versions and relational indexes.
10. Define strict output schemas with all fields required, `additionalProperties: false`, bounded collections, allowlisted enums, forbidden executable fields, and evidence-reference rules.
11. Create the .NET 10 solution, all empty projects from Section 5.6, project references, test discovery, and compile-time contract skeleton needed by Phase 2A; do not implement behavior.
12. Define the production capability contract as zero tools, no sessions, no memory, no handoffs, no MCP, and no previous-response chaining.

**Exit criterion:** each workflow node can be replaced by a test double using only published contracts.

### Phase 2A — Write the behavioral acceptance suite before implementation

Phase 0 through Phase 2 produce specifications and compile-time contracts only. No behavioral production implementation begins until this gate passes.

1. Freeze the Section 10 test manifest, test IDs, fixture schemas, threshold-boundary tables, fake-clock rules, and canonical expected outputs.
2. Create `AlpacaAgent.UnitTests` organized by `Contracts`, `StateMachines`, `Workflow`, `Reconciliation`, `Features`, `Evidence`, `PolicyAssessment`, `Candidates`, `Selection`, `Risk`, `Execution`, `Lifecycle`, `Replay`, and `Reporting`.
3. Use the compile-time Domain/Application skeleton from Phase 2. Stubs return explicit `NOT_IMPLEMENTED` domain outcomes rather than throwing from shared setup, so each behavioral assertion reaches and fails at its intended observable boundary.
4. Implement the entire Section 10 unit catalogue against public contracts and controlled fakes. No test is skipped, conditionally disabled, or made vacuous.
5. Add test-discovery verification that checks the stable IDs and prevents a coding agent from silently omitting a required case.
6. Capture the expected red baseline: every failing test must reach its intended assertion and fail because the specified behavior is absent. Fixture, serialization, discovery, compilation, or setup failures are fixed before production work begins.
7. Lock golden fixtures and expected canonical hashes as review-owned artifacts. Production code cannot update them automatically.
8. Configure focused phase filters plus an all-unit-tests completion gate. A phase is complete only when its assigned tests and all previously green tests pass.

**Exit criterion:** the full acceptance manifest is discoverable, fixtures are valid and immutable, failures point to unimplemented behavior rather than test defects, and production behavior has not yet been implemented.

### Phase 3 — Build the durable workflow foundation

1. Fill the Phase 2 solution skeleton with the first production slice only after the complete Phase 2A red baseline is accepted.
2. Create PostgreSQL migrations for `workflow_cycles`, `workflow_steps` with lease columns, `workflow_step_attempts`, `workflow_step_attempt_results`, `workflow_checkpoints`, `outbox_messages` with lease columns, `order_intents`, `share_reservations`, `agent_attempts`, `agent_attempt_results`, `broker_operations`, `broker_observations`, `domain_events`, and immutable snapshot/decision tables.
3. Encode the 18 workflow nodes and Section 5.2 cycle templates as stable C# identifiers; persist inapplicable nodes as `SKIPPED` and register dependencies, input contract version, timeout, retry classification, and terminal outcomes in code.
4. Implement scheduled-cycle creation with a unique schedule key so repeated scheduler ticks cannot create duplicate cycles.
5. Implement due-step claiming in short Npgsql transactions using database locking, bounded leases, and monotonically increasing fencing tokens.
6. Append one attempt-start row on every claim and one authoritative attempt-result row on valid finalization; never overwrite either.
7. Run node handlers outside the claim transaction and finalize them only when lease owner and fencing token still match.
8. Atomically persist output references, domain events, dependent-step activation, and outbox messages during step finalization.
9. Implement immutable checkpoints before every broker-visible write.
10. Implement bounded retry, timeout, cancellation, next-due-time, dead-letter/safe-halt, and restart behavior.
11. Implement expired-lease recovery and prove that a stale worker cannot advance a step after another worker acquires a newer fencing token.
12. Implement PostgreSQL-backed manual pause, symbol pause, strategy pause, and global kill-switch controls checked before scheduling and before side effects.
13. Implement the transactional outbox dispatcher and reconciliation dispatcher without a separate message broker.
14. Add health checks for PostgreSQL connectivity, claim-loop progress, outbox backlog, oldest due work, and kill-switch state.

**Exit criterion:** every Section 5.2 template can stop and restart before, during, and after each applicable node; inapplicable nodes persist as `SKIPPED`; expired leases are recovered; stale fencing tokens are rejected; state and outbox writes remain atomic; and repeated scheduling or dispatch cannot create duplicate durable effects.

### Phase 4 — Acquire, normalize, and reconcile authoritative data

1. Pin a stable `Alpaca.Markets` package version and implement `AlpacaBrokerReadAdapter`. Define the separate write-port contract and recorded DTO fixtures, but defer its network-capable implementation to Phase 10.
2. Perform a non-writing endpoint/type-coverage spike for account, holdings, open and closed orders, activities, option permissions, clock, calendar, assets, option contracts, option-chain snapshots, corporate actions/dividend calendar, order lookup by `client_order_id`, submission, and cancellation. Verify read-only normalization of externally observed replacement statuses; do not send broker writes or implement a replace write.
3. Implement narrowly scoped typed `HttpClient` fallbacks only for required endpoints missing from the pinned SDK; record them explicitly and cover them with contract tests.
4. Retrieve account, holdings, open orders, closed orders needed for reconciliation, activities, and option permissions.
5. Retrieve required underlying and option-chain data, including multiplier, deliverable, adjusted/active/tradable state, underlying/successor identifiers, and authoritative corporate-action and ex-dividend inputs.
6. Normalize SDK and raw-REST responses into application-owned C# records for symbols, timestamps, quantities, prices, option identifiers, and session state.
7. Record source time separately from retrieval time and preserve safely stored raw response references for diagnosis.
8. Reject stale, incomplete, duplicated, crossed, or inconsistent observations.
9. Reconstruct free shares, reservations, existing calls, and pending obligations.
10. Stop new trading when broker state cannot be reconciled.
11. Persist immutable normalized snapshots and raw-response hashes for replay without allowing stale cache use in live decisions.

**Exit criterion:** invalid data cannot progress, and the system cannot expose more short calls than verified free-share coverage permits.

### Phase 5 — Calculate features and acquire bounded context

1. Define each feature's formula, lookback, inputs, valid range, and purpose.
2. Compute declared trend, realized-volatility, implied-volatility, liquidity, spread, event, and portfolio features.
3. Prevent look-ahead bias using decision-time availability.
4. Retrieve only bounded, approved news, filing, earnings, and macro-event text. Consume authoritative structured corporate-action and dividend-calendar inputs from Phase 4; do not reacquire or infer them semantically.
5. Preserve deterministic evidence identifiers, source identity, trust label, publication time, event time, retrieval time, integrity state, and bounded evidence references.
6. Exclude or explicitly mark stale, irrelevant, duplicated, or unsupported context.
7. Persist the complete `PolicyInputPackageV1`, its content hash, and the ordered evidence identifiers.
8. Implement feature calculators as pure C# functions; use `decimal` for persisted money, strikes, quantities, and ratios, and use `double` only for declared statistical calculations with explicit conversion and rounding boundaries.
9. Implement evidence adapters with `HttpClientFactory`, explicit allowlists, per-source timeouts, bounded response sizes, and deterministic deduplication.
10. Label retrieved content as untrusted data and keep it separate from developer instructions; source text never becomes an instruction.
11. Give the policy agent no retrieval capability. All evidence retrieval completes deterministically before invocation.

**Exit criterion:** the same stored observations produce the same deterministic features and evidence input set.

### Phase 6 — Implement the required narrow AI agent

1. Implement the combined `PolicyInputPackageV1` to `PolicyAssessmentV1` contract behind `IPolicyAssessor` in Application.
2. Spike the preferred thin Agent Framework `ChatClientAgent` adapter over `IChatClient` entirely in Infrastructure.
3. Spike the direct OpenAI `ResponsesClient` adapter with the same fixtures, contract tests, timeout/cancellation tests, and stored metadata.
4. Freeze exactly one production path using the Section 6.2 decision criteria and pin its stable package versions; retain the other as non-production research code or remove it.
5. Enforce one stateless request per attempt with zero tools, no session, no memory, no handoff, no MCP, no hosted capability, and no previous-response chaining.
6. Require strict Structured Outputs matching the versioned schema; all fields are required, additional properties are forbidden, and strings/arrays are bounded.
7. Require supporting evidence, opposing evidence, conflicts, missing information, uncertainty, and exactly one allowed policy using only supplied evidence identifiers.
8. Prohibit executable fields, unknown symbols, unknown evidence identifiers, invented strategies, and changes to limits.
9. Validate schema, allowlist, source scope, evidence references, confidence rules, input hash, and forbidden fields deterministically after deserialization.
10. Insert an immutable agent-attempt start before the provider call and an immutable result/observation afterward, using the fields and late-result rule in Section 7.7; retries receive new attempt identities.
11. Apply PostgreSQL lease, attempt, timeout, cancellation, and append-only failure rules; set the model-call timeout below the remaining step lease and retry budget.
12. Treat invalid output as failure and a new attempt; do not use a free-form repair agent.
13. Distinguish valid `INSUFFICIENT_INFORMATION` from timeout, refusal, rate limit, service failure, truncation, invalid schema, or policy rejection.
14. Forbid silent provider/model fallback. Any fallback requires an explicit versioned configuration and a new recorded attempt.
15. Evaluate GPT-5.6 Terra and GPT-5.6 Luna on the frozen labelled set, including reasoning-setting, latency, token, grounding, abstention, and stability measurements; freeze the winner before shadow mode. Keep GPT-5.6 Sol offline-only.
16. Evaluate any local/Ollama-compatible implementation only as a challenger for extraction, explanation, replay, or offline degradation until it passes the identical live-decision gates.
17. Implement deterministic ablation output separately; never use it silently as the normal hackathon entry path.
18. Do not implement the two-agent split, Grounding Critic, or Explanation Agent in the MVP; Section 14 governs any later promotion.

**Exit criterion:** every eligible new-entry cycle that reaches node 6 obtains an accepted, strictly validated assessment through the frozen stateless runtime/model configuration or safely defers with an explicit, correctly classified reason; deterministic deferrals before node 6 remain valid; no agent tool, session, silent fallback, or infrastructure type crosses the application boundary.

### Phase 7 — Map policy, generate candidates, and apply candidate checks

1. Map an accepted call policy to a versioned inner profile.
2. Verify the profile remains inside immutable outer limits.
3. Produce no entry candidates for `SKIP`, `INSUFFICIENT_INFORMATION`, or agent failure.
4. Enumerate eligible holdings and free 100-share blocks.
5. Filter contracts by policy profile, quote quality, liquidity, expiration, strike, and event constraints.
6. Calculate premium, yield estimate, distance to strike, break-even effect, capped upside, assignment context, and covered-call downside context.
7. Apply candidate-level structural and admissibility checks.
8. Produce an empty set with explicit reasons when no candidate qualifies.

**Exit criterion:** every candidate is structurally covered, policy-compatible, and independently auditable.

### Phase 8 — Rank candidates and select a proposed action set

1. Apply a declared deterministic scoring or ordered-comparison profile.
2. Use deterministic tie-breaking.
3. Penalize spread, illiquidity, assignment probability, uncertainty, concentration, and event risk.
4. Reward policy-aligned premium and portfolio efficiency without treating score as risk approval.
5. Select zero or more candidates while preventing overlapping share-block allocation inside the proposed set.
6. Return trade, no trade, or unable to decide safely.
7. Record the deterministic ablation result and whether AI materially changed the outcome.

**Exit criterion:** identical ranked-candidate inputs and versions produce the same proposed action set.

### Phase 9 — Run the final deterministic portfolio risk gate

Evaluate the complete proposed action set in a stable order:

1. input completeness and freshness;
2. account status and permissions;
3. combined verified share coverage;
4. duplicate and conflicting orders;
5. allowed symbols, contracts, expirations, and strategy;
6. quote and liquidity quality;
7. per-trade and per-cycle quantity;
8. per-underlying, expiration, and portfolio exposure;
9. event and blackout restrictions;
10. daily order, loss, error, and retry limits;
11. pause and kill-switch state.

Store evidence, threshold, result, and reason for every rule. Issue a short-lived approval only for the exact action set and exact snapshot versions. Revalidate relevant state immediately before submission.

**Exit criterion:** independently acceptable candidates cannot collectively overcommit shares or portfolio limits.

### Phase 10 — Build intents, execute, and reconcile orders

1. Translate only an approved action set into immutable `order_intents` using application-owned C# records.
2. Generate a stable intent key and deterministic Alpaca `client_order_id`; enforce unique constraints on both.
3. Validate contract, side, quantity, price, time in force, and expected position effect.
4. Recheck state, quote freshness, reservations, open orders, approval validity, pauses, and kill switches.
5. In one PostgreSQL transaction, persist the final pre-trade decision, checkpoint, order intent, share reservation, domain event, and `SUBMIT_ORDER` outbox message.
6. Claim the outbox message with a bounded lease and fencing token.
7. In a short transaction, append the immutable broker request-attempt record and move the intent to `SUBMISSION_PENDING`; commit before calling Alpaca.
8. Submit through `AlpacaBrokerWriteAdapter` outside every database transaction using the stored `client_order_id`.
9. Insert the immutable response/timeout observation, then update the normalized intent/outbox projection in the same new transaction; no request or observation row is updated.
10. If no definitive response is durably stored, transition the recovered intent to `SUBMITTED_UNKNOWN`; do not infer failure from a timeout or process crash.
11. Reconcile `SUBMITTED_UNKNOWN` by `client_order_id`, open/closed orders, positions, and relevant activities before any retry.
12. Allow a resubmission only after reconciliation proves absence, the approval is still valid, current broker/market state passes revalidation, and the same intent identity remains safe. Blind retry is prohibited.
13. Reconcile accepted, rejected, partial, filled, canceled, expired, and still-unknown states through append-only broker observations.
14. Implement cancellation and order-change handling exactly as specified once in Section 5.5.
15. Recover after restart from PostgreSQL intents, outbox state, and broker truth; never from in-memory tasks.
16. Before enabling any real paper-broker write, use Testcontainers plus a controlled HTTP broker stub to pass four crash windows: before `SUBMISSION_PENDING`, after it but before the HTTP call, after the HTTP call but before observation persistence, and after observation persistence but before outbox completion. Cover `SUBMITTED_UNKNOWN` lookup outcomes of accepted, filled, rejected, canceled, absent, and still unresolved. Until these pass, the broker-write adapter is forced to a recorder that cannot reach Alpaca.

**Exit criterion:** failure injection before submission, during the network call, after broker acceptance but before response persistence, and during result finalization produces either one reconciled broker order or a visible safe halt, never a duplicate order.

### Phase 11 — Manage open-position lifecycle

1. Reconcile every open position at the start of each management cycle.
2. Evaluate deterministic profit-taking, expiration, event, assignment, and underlying-change rules.
3. Generate close actions through deterministic lifecycle proposal, node-12 exact-set selection, node-13 risk approval, intent, and reconciliation controls. Reject roll requests as out of MVP scope.
4. Reserve shares until the obligation is definitively removed by broker confirmation.
5. Handle expiration and assignment explicitly.
6. Continue safe deterministic management during AI failure when required market and account data remain valid.
7. Record the reason, state, and next review time for every position.
8. Persist the next review as a due workflow step; do not rely on in-memory timers.
9. Poll Alpaca REST activities for assignment and expiration evidence because these events cannot be assumed to arrive through a WebSocket stream.

**Exit criterion:** every open position has a broker-reconciled state, management decision, and next review time.

### Phase 12 — Add auditability, observability, and explanations

1. Extend the minimal durable records into correlated audit events and timelines.
2. Track provenance and every component version without logging secrets.
3. Separate no trade, valid abstention, agent failure, deterministic rejection, and operational failure.
4. Track freshness, latency, retries, rejections, fill quality, reconciliation errors, and safety stops.
5. Add alerts for stale data, coverage violation, repeated order errors, unexplained state changes, and kill-switch activation.
6. Add deterministic explanation templates as the default reporting path.
7. Keep AI-generated explanations out of the MVP. Any post-MVP Explanation Agent follows Section 6.6 and cannot gate or alter trading.
8. Emit OpenTelemetry traces and metrics from the Worker, API, PostgreSQL operations, HTTP adapters, agent runtime, provider calls, and Alpaca calls with cycle/step/attempt correlation.
9. Use structured .NET logging for operational diagnosis, but treat PostgreSQL `domain_events`, attempts, checkpoints, and broker observations as the authoritative audit trail.
10. Add metrics for due-step age, claim rate, lease expiry, stale-fence rejection, outbox depth, `SUBMITTED_UNKNOWN` age, reconciliation duration, and terminal safe halts.

**Exit criterion:** an evaluator can reconstruct every decision and outcome without reading runtime internals.

### Phase 13 — Build replay and simulation

Implement two different replay modes:

1. **Frozen replay** reuses the stored accepted AI assessment and requires exact reproduction of all deterministic downstream results.
2. **AI reevaluation replay** calls an explicitly selected versioned runtime/provider/model/reasoning candidate against the stored input package and measures policy stability, evidence grounding, conflict recognition, abstention, latency, and tokens rather than exact textual equality.

Also:

- simulate fills, partial fills, cancellations, expiration, and assignment;
- model bid/ask effects, configurable slippage, and applicable fees;
- prevent future information from entering historical decisions;
- compare all policies under identical assumptions;
- compare the frozen production configuration with declared challenger configurations without silent substitution;
- generate decision-by-decision diffs across versions;
- load frozen inputs only from immutable PostgreSQL snapshot references and never call live adapters during frozen replay;
- run deterministic replay through the same Domain and Application handlers using in-memory test ports, not through the operational claim loop;
- verify that persisted JSON contract versions can still be deserialized or explicitly migrated.
- guarantee that replay agents receive only immutable stored inputs and never call live retrieval, broker, file, web, MCP, or other tools.

**Exit criterion:** deterministic changes are exactly regression-testable, while AI variation is separately and honestly measurable.

### Phase 14 — Complete non-unit verification layers
The unit suite in Section 10 was written before behavioral implementation and is already the mandatory gate. Phase 14 adds the boundaries that cannot be proved honestly by unit tests:

1. Run the complete unit suite and property-based invariant suite first.
2. Run JSON Schema, serialization compatibility, SDK/REST adapter, and recorded-response contract tests.
3. Run PostgreSQL workflow restart, checkpoint, transaction, lease, fencing-token, due-step claiming, constraint, and idempotency tests using Testcontainers.
4. Run failure injection for rate limits, stale data, malformed results, restarts, lease expiry, deadlocks, database disconnects, outbox redelivery, pagination loss, stream gaps, and partial fills.
5. Run frozen replay exactness and old-contract migration tests in a clean process under invariant culture and a non-default timezone.
6. Run AI runtime/evaluation tests for schemas, grounding, conflicts, prompt injection, abstention, refusals, truncation, configuration drift, stability, latency, tokens, and cost.
7. Consolidate the full deterministic and recorded-provider verification result. Phase 15 owns shadow operation and Phase 16 owns real paper-broker operation.
8. Re-run the broker-submission crash-window integration tests first introduced in Phase 10 at four boundaries: before marking `SUBMISSION_PENDING`, after marking it but before the HTTP call, after the HTTP call but before response persistence, and after response persistence but before outbox completion.
9. Test `SUBMITTED_UNKNOWN` reconciliation when lookup finds an accepted, filled, rejected, canceled, absent, or still-unresolved order.
10. Prove with real database constraints that duplicate schedule ticks, claims, intent keys, `client_order_id` values, reservations, and outbox deduplication keys cannot create duplicate durable effects.
11. Prove a stale worker cannot finalize a step or dispatch a broker side effect after lease loss.
12. Use deterministic assertions as the release gate. `Microsoft.Extensions.AI.Evaluation`, Agent Framework evaluation utilities, or promptfoo may run outside production for experiment management; LLM-as-judge scores are secondary diagnostics and never the sole promotion gate.

**Exit criterion:** every invariant and critical failure mode has an executable test and recorded result.

### Phase 15 — Run shadow mode

1. Run the intended schedules without submitting orders.
2. Persist hypothetical assessments, candidates, action sets, approvals, intents, and lifecycle actions.
3. Review false trades, missed trades, unstable policy selection, excessive deferral, and operational failures.
4. Compare the AI path with the deterministic ablation baseline.
5. Require repeated clean cycles containing at least one AI-driven policy change, one valid no-trade decision, one valid abstention, and one injected AI failure.
6. Calibrate parameters only through recorded, versioned changes.
7. Run the actual PostgreSQL scheduler, claim loop, leases, attempts, checkpoints, and outbox path; only the broker-write adapter is replaced with a shadow recorder.

**Exit criterion:** repeated autonomous cycles complete without unsafe intents, duplicate actions, or unreconciled state.

### Phase 16 — Run controlled paper trading

1. Enable the smallest quantity and narrowest approved universe.
2. Retain strict per-cycle and daily limits.
3. Monitor and immediately reconcile initial actions.
4. Exercise entry, cancellation, fill, and close or expiration in paper trading.
5. Exercise both expiration and assignment in simulation/replay even if assignment cannot complete during the live judging window.
6. Expand only after predefined stability gates pass.
7. Freeze the demonstration policy before the judging window.
8. Keep the required AI agent active for every eligible new-entry evaluation that reaches node 6; deterministic pre-node-6 deferrals remain valid.
9. Deploy the .NET Worker/API and PostgreSQL with pinned container and package versions; do not add Kubernetes or a message broker for the demonstration.
10. Back up the PostgreSQL schema and immutable decision records before the judging window and verify restart from a clean worker process.

**Exit criterion:** the AI-assisted workflow completes entry plus close or expiration in paper trading, with assignment and expiration paths verified in simulation/replay and no unresolved safety defect.

### Phase 17 — Evaluate performance and robustness

Report:

- trading outcome: realized/unrealized P&L, premium retained, comparison returns, assignment and expiration outcomes, sample size;
- risk: drawdown, concentration, downside capture, capped-upside opportunity cost, rejected candidates;
- execution: fill ratio, spread, slippage, latency, cancellations, externally observed replacements, cancel-and-new order changes, partial fills, and rejections;
- workflow quality: completed cycles, duplicate prevention, restart recovery, stale-data blocks, reconciliation failures;
- kernel quality: due-step age, claim contention, expired leases, stale-fence rejections, outbox redeliveries, `SUBMITTED_UNKNOWN` count and age, and crash-window recovery time;
- AI quality: valid assessment rate, failure categories, grounding, conflict recognition, labelled no-trade correctness, appropriate abstention, policy stability, unsafe-output rejection, and material decision changes, broken down by runtime/provider/model/reasoning version;
- AI operations: input/output tokens, estimated cost, latency percentiles, refusal/truncation/rate-limit incidence, resolved-versus-requested model identity, and configuration changes;
- benchmark comparison: buy-and-hold, fixed-schedule overlay, deterministic ablation, and AI-assisted workflow under identical assumptions.

Do not claim calibrated confidence without calibration evidence or durable profitability from a short favorable window.

**Exit criterion:** results are reproducible, bounded by sample size and simulation assumptions, and tied to the frozen experiment contract.

### Phase 18 — Build the submission package

1. Explain the portfolio problem and covered-call thesis.
2. Show the durable observe-assess-validate-select-approve-execute-reconcile-manage loop.
3. Demonstrate an accepted trade, deterministic rejection, valid `SKIP`, valid `INSUFFICIENT_INFORMATION`, and operational AI failure.
4. Show how the AI policy materially changes deterministic profile selection.
5. Show the final portfolio risk gate preventing combined over-allocation.
6. Show that agents cannot construct orders or call the broker.
7. Show a complete workflow timeline and restart-safe checkpoint.
8. Compare frozen replay with AI reevaluation replay.
9. Compare benchmarks and the deterministic ablation.
10. State limitations, paper assumptions, and deferred work.
11. Provide repeatable setup, configuration, run, replay, and test instructions.
12. Show the PostgreSQL workflow timeline: cycle, nodes, attempts, checkpoints, leases, fencing tokens, outbox messages, intents, and broker observations.
13. Demonstrate a forced crash after simulated broker acceptance but before response persistence, followed by `SUBMITTED_UNKNOWN` reconciliation without a duplicate order.
14. Explain why the deliberately small workflow kernel is sufficient for the 18 nodes and which conditions would trigger migration to an external workflow platform.
15. Show the concrete node-6 boundary: `IPolicyAssessor` in Application, the frozen thin `ChatClientAgent` or direct-SDK adapter in Infrastructure, strict `PolicyAssessmentV1`, zero tools, and no session or memory.
16. Document the direct `ResponsesClient` fallback decision and the frozen provider/model evaluation evidence.

**Exit criterion:** a judge can understand the AI contribution, verify deterministic safeguards, and reproduce the main result.

## 10. Test-First Unit-Test Baseline

This catalogue is the minimum behavioral contract. Every row is implemented in Phase 2A before the corresponding production behavior. A row with a case matrix is one xUnit theory whose individual cases appear in test discovery. `UnitUnderTest_Condition_ExpectedOutcome` is the required method-name pattern; the stable ID is also attached as a test trait.

### 10.1 Contracts, values, serialization, and canonical identity

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-001 | `Quantity_CreateFractionalOptionContracts_IsRejected` | Option quantities cannot be fractional. |
| UT-002 | `Coverage_FromFractionalShares_CountsOnlyCompleteHundreds` | `99.999` yields zero blocks; `100.999` yields one. |
| UT-003 | `OptionalValue_UnknownInvalidUnavailableAndZero_RemainDistinct` | Four states survive mapping and JSON round-trip without coercion. |
| UT-004 | `Money_RoundAtDeclaredBoundary_UsesConfiguredRuleOnce` | Money is rounded exactly once at the declared boundary. |
| UT-005 | `Ratio_DivideByZero_ReturnsInvalidNotInfinity` | Invalid arithmetic cannot become a rankable value. |
| UT-006 | `ContractSchema_UnknownProperty_IsRejected` | `additionalProperties: false` is enforced. |
| UT-007 | `ContractSchema_MissingRequiredProperty_IsRejected` | Missingness is not filled with a default. |
| UT-008 | `ContractSchema_UnknownEnumValue_IsRejectedOrMigratedExplicitly` | No permissive enum fallback enters Domain. |
| UT-009 | `CanonicalHasher_PropertyAndDictionaryOrderVaries_HashIsStable` | Semantically identical records produce the same hash. |
| UT-010 | `CanonicalHasher_MaterialValueChanges_HashChanges` | Any executable or approval-bound value changes identity. |
| UT-011 | `DecisionTime_ParseDifferentOffsets_RepresentsSameInstant` | Offset-equivalent timestamps normalize to one UTC instant. |
| UT-012 | `PersistedContract_OlderSupportedVersion_UsesExplicitMigrator` | Supported old data is migrated deliberately; unsupported versions fail visibly. |

### 10.2 State-machine unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-020 | `PortfolioEligibility_TransitionMatrix_AllDeclaredTransitionsMatchSpecification` | Theory covers every allowed source/event/target triple. |
| UT-021 | `PortfolioEligibility_InvalidTransition_EntersSafeHaltWithoutMutation` | Invalid transitions do not invent an eligible state. |
| UT-022 | `OrderState_TransitionMatrix_AllBrokerStatusesNormalizeDeterministically` | Covers `accepted`, `pending_new`, `new`, `held`, `accepted_for_bidding`, `partially_filled`, `done_for_day`, `pending_cancel`, `pending_replace`, `stopped`, `suspended`, `calculated`, `filled`, `canceled`, `expired`, `rejected`, and `replaced`. |
| UT-023 | `OrderState_UnknownBrokerStatus_RequiresReconciliation` | Future/unknown values fail closed rather than being treated as terminal. |
| UT-024 | `OrderState_TerminalObservationFollowedByOlderEvent_DoesNotRegress` | Out-of-order observations cannot reopen a terminal order. |
| UT-025 | `OrderState_TradeCorrectionOrBust_RecomputesCumulativeFill` | Corrected/busted activity changes derived fill state through a new observation. |
| UT-026 | `OptionLifecycle_TransitionMatrix_AllDeclaredTransitionsMatchSpecification` | Every declared lifecycle event has one next state and next action. |
| UT-027 | `OptionLifecycle_AssignmentObservedAtAnyAge_EntersAssignedPendingReconciliation` | Early assignment is accepted on any open-date state, not only expiry day. |
| UT-028 | `OptionLifecycle_ExpiryQuoteSeenWithoutBrokerConfirmation_RemainsPending` | A quote cannot release the obligation. |
| UT-029 | `OptionLifecycle_CloseFillConfirmed_ReleasesOnlyClosedQuantity` | Partial close retains the remaining obligation. |
| UT-030 | `TerminalWorkflowCycle_AnyFurtherAdvance_IsRejected` | Completed, no-action, safely-deferred, and failed cycles are immutable. |
| UT-031 | `Lifecycle_RollRequestedInMvp_IsRejectedAsOutOfScope` | No close, entry, intent, or broker write is created by an MVP roll request. |
| UT-032 | `StateTransition_DuplicateEvent_IsIdempotent` | Reprocessing an identical event produces no second effect. |

### 10.3 Workflow routing and normal-flow unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-040 | `NewEntryWorkflow_ValidInputsAndOneApprovedCandidate_TraversesApplicableNodesInDeclaredOrder` | The reference new-entry template reaches intent waiting-for-execution; every applicable predecessor is terminal and every inapplicable node is `SKIPPED`. |
| UT-041 | `NewEntryWorkflow_FilledOrderAndOpenCall_CompletesWithLifecycleScheduled` | Normal broker observations lead to a managed open obligation and durable next-review request. |
| UT-042 | `NewEntryWorkflow_AgentReturnsSkip_EndsNoActionWithoutCandidateOrIntent` | Valid `SKIP` is not a failure and produces no executable artifact. |
| UT-043 | `NewEntryWorkflow_AgentAbstains_EndsSafelyDeferredWithMissingInformation` | Valid `INSUFFICIENT_INFORMATION` remains distinct from failure. |
| UT-044 | `NewEntryWorkflow_AgentOperationalFailure_DefersAndDoesNotUseAblation` | Timeout/refusal/etc. cannot silently select a deterministic policy. |
| UT-045 | `NewEntryWorkflow_NoAdmissibleCandidate_EndsNoActionWithOrderedReasons` | A valid AI call policy does not force a trade. |
| UT-046 | `NewEntryWorkflow_RiskRejectsSet_EndsNoActionWithoutIntent` | Intent creation is impossible after rejection. |
| UT-047 | `ManagementWorkflow_AgentUnavailable_StillRunsDeterministicLifecycle` | Existing obligations are not abandoned because node 6 is unavailable. |
| UT-048 | `ManagementWorkflow_InvalidBrokerInputs_SafeHaltsRatherThanGuessing` | Management continuation still requires broker/account truth. |
| UT-049 | `WorkflowTemplate_InapplicableNode_IsSkippedAndCannotRun` | A skipped node has a routing reason and does not block its template's applicable successor; an unmet applicable dependency still blocks execution. |
| UT-050 | `WorkflowRetry_RetryableFailureWithinBudget_SchedulesNewAttempt` | Retry advances attempt identity, preserves prior evidence, and respects next-due time. |
| UT-051 | `WorkflowRetry_BudgetExhausted_EndsFailedAndEscalated` | No unbounded retry loop occurs. |
| UT-052 | `WorkflowDeadline_ExpiresDuringHandler_ResultCannotAdvanceCycle` | Late completion becomes cancellation/lost authority. |
| UT-053 | `WorkflowCancellation_PropagatesToPortAndPersistsClassifiedOutcome` | Cancellation is observable and cannot be rewritten as success. |
| UT-054 | `Scheduler_DuplicateScheduleKey_ReturnsExistingCycleIdentity` | Application behavior is idempotent; the database uniqueness proof is added later. |
| UT-055 | `CycleConflict_ManagementAndEntryOverlap_ManagementTakesPriority` | New exposure is deferred while the same underlying requires lifecycle action. |

### 10.4 Broker-state, market-data, and reconciliation unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-060 | `Freshness_SourceTimeAtBoundary_UsesDocumentedInclusiveRule` | Exactly-at-TTL behavior is fixed and tested; retrieval time does not replace source time. |
| UT-061 | `Freshness_SourceOldButRetrievedNow_IsStale` | A new download timestamp cannot freshen an old quote. |
| UT-062 | `QuoteQuality_MissingZeroNegativeCrossedOrInvalidQuote_IsRejected` | Theory covers each invalid bid/ask case with a stable reason. |
| UT-063 | `QuoteQuality_LockedQuote_FollowsFrozenPolicy` | The exact locked-market rule is explicit, not accidental. |
| UT-064 | `Snapshot_OutOfOrderObservation_DoesNotReplaceNewerSourceTruth` | Timestamp regression is ignored and audited. |
| UT-065 | `OptionChain_MissingPageTokenCompletion_IsIncomplete` | Partial pagination cannot masquerade as a complete universe. |
| UT-066 | `MarketFeed_IndicativeWhenOpraRequired_IsPolicyIneligible` | Feed downgrade is visible and blocks the affected decision. |
| UT-067 | `MarketSession_HolidayEarlyCloseOrDst_UsesClockAndCalendarInstant` | Theory uses real offset/early-close fixtures and no machine-local assumption. |
| UT-068 | `Account_OptionsLevelBelowOne_IsIneligible` | Covered-call entry requires the frozen minimum permission. |
| UT-069 | `Account_TradeBlockedOrBuyingPowerInsufficient_IsIneligible` | Account restriction wins over candidate attractiveness. |
| UT-070 | `Reconciler_NinetyNineShares_NoCoverage` | No contract is available. |
| UT-071 | `Reconciler_OneHundredFreeShares_OneCoverageBlock` | Exactly one standard contract may be covered. |
| UT-072 | `Reconciler_TwoHundredSharesAndOneShortCall_OneFreeBlock` | Existing obligations reduce free coverage. |
| UT-073 | `Reconciler_PendingShareSaleAndOptionOrders_ReserveCoverageOnce` | Reservations are deduplicated and subtracted once. |
| UT-074 | `Reconciler_NonStandardMultiplierOrAdjustedDeliverable_ManagementOnly` | Adjusted contracts do not enter the standard 100-share MVP path. |
| UT-075 | `Reconciler_ConflictingPositionOrderAndActivitySnapshots_SafeHalts` | Inconsistent broker truth cannot be resolved optimistically. |
| UT-076 | `Reconciler_DuplicateBrokerObservation_DoesNotDoubleCount` | Duplicate order/activity payloads are idempotent. |
| UT-077 | `CorporateAction_AffectsUnderlyingOrContract_InvalidatesEligibility` | Split, merger, symbol change, or special-dividend fixtures pause new entries. |
| UT-078 | `MarketHalt_OptionEntryRequested_IsRejectedWithMarketStateReason` | Halt/pause state blocks new options exposure. |

### 10.5 Feature and evidence unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-080 | `FeatureCalculator_FrozenObservations_ProducesGoldenFeatureVector` | Pure calculations reproduce the approved golden vector. |
| UT-081 | `FeatureCalculator_FutureObservation_IsExcluded` | No observation available after decision time enters a feature. |
| UT-082 | `FeatureCalculator_InsufficientLookback_ReturnsUnavailableNotZero` | Missing history is explicit. |
| UT-083 | `OptionMetrics_KnownInputs_ProduceGoldenPremiumYieldBreakevenAndUpside` | Decimal conversions and rounding match the declared formula. |
| UT-084 | `OptionMetrics_ThresholdMatrix_UsesDeclaredBoundarySemantics` | DTE, strike, spread, volume, open-interest, delta, and premium limits are tested just below, at, and above bounds. |
| UT-085 | `EvidenceBuilder_ApprovedFreshSources_ProducesDeterministicNeutralBundle` | Same inputs yield the same evidence IDs, order, hash, and deterministic scope flags without semantic supporting/opposing labels. |
| UT-086 | `EvidenceBuilder_DuplicateSyndicatedStory_KeepsCanonicalEvidenceOnce` | Duplicate content does not overweight one claim. |
| UT-087 | `EvidenceBuilder_FutureOrStaleEvidence_ExcludesOrMarksPerPolicy` | Decision-time and recency rules are explicit. |
| UT-088 | `EvidenceBuilder_UnavailablePaywalledOrInvalidBody_PreservesMissingness` | Missing body is not synthesized. |
| UT-089 | `EvidenceBuilder_OversizedBody_TruncatesAtDeterministicSafeBoundary` | Size caps preserve provenance and a truncation marker. |
| UT-090 | `EvidenceBuilder_InstructionLikeText_RemainsUntrustedEvidence` | Prompt-like text never enters trusted instructions. |
| UT-091 | `EvidenceBuilder_DistinctClaimsFromDifferentSources_PreservesBothNeutralRecords` | Deterministic bundling does not remove distinct source claims; the AI assessment, not the builder, owns semantic conflict classification. |
| UT-092 | `EvidenceBuilder_UnrelatedTickerMention_DoesNotExpandSymbolScope` | Evidence cannot silently add a tradable symbol. |
| UT-093 | `PolicyInputPackage_EvidenceOrderCanonicalization_IsStable` | Input hash is stable under source-return order changes after canonicalization. |

### 10.6 Policy-agent boundary and deterministic validator unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-100 | `PolicyValidator_AllowedCallPoliciesWithValidEvidence_AreAccepted` | Theory covers conservative, balanced, and income policies. |
| UT-101 | `PolicyValidator_ValidSkip_IsAcceptedAsNoTrade` | `SKIP` is completed, not failed. |
| UT-102 | `PolicyValidator_ValidInsufficientInformation_IsAcceptedAsAbstention` | Required missing-information evidence is present. |
| UT-103 | `PolicyValidator_TimeoutRefusalRateLimitOrTruncation_IsOperationalFailure` | Theory proves none is converted to abstention. |
| UT-104 | `PolicyValidator_MalformedJsonOrMissingRequiredField_IsRejected` | Strict schema failure is classified. |
| UT-105 | `PolicyValidator_AdditionalProperty_IsRejected` | Extra fields never survive deserialization/validation. |
| UT-106 | `PolicyValidator_UnknownPolicySymbolOrEvidenceId_IsRejected` | All three allowlists are enforced. |
| UT-107 | `PolicyValidator_ExecutableFieldPresent_IsRejected` | Contract, strike, expiry, quantity, side, price, TIF, and broker-operation cases are covered. |
| UT-108 | `PolicyValidator_InputHashOrSchemaVersionMismatch_IsRejected` | Output cannot bind to a different input/configuration. |
| UT-109 | `PolicyValidator_MaterialClaimWithoutEvidence_IsRejected` | Every required material claim has supplied evidence support. |
| UT-110 | `PolicyValidator_DuplicateEvidenceReferences_DoNotIncreaseSupport` | Duplicate IDs are normalized or rejected per schema. |
| UT-111 | `PolicyValidator_ContradictionOmittedWhenInputsConflict_IsRejected` | Frozen conflict fixtures require explicit conflict treatment. |
| UT-112 | `PolicyValidator_LowOrContradictoryConfidence_FollowsFrozenRule` | Confidence cannot contradict policy/uncertainty requirements. |
| UT-113 | `PolicyValidator_ToolRequestOrToolCall_IsCapabilityViolation` | Any tool behavior is rejected and recorded. |
| UT-114 | `PolicyValidator_SessionThreadOrPreviousResponseIdentifier_IsCapabilityViolation` | Stateful metadata is prohibited. |
| UT-115 | `PolicyValidator_ResolvedModelDiffersFromFrozenModel_IsRejected` | Silent model/provider substitution blocks normal execution. |
| UT-116 | `PolicyAssessor_CancellationAndTimeout_ArePropagatedAndBounded` | Fake provider observes cancellation; attempt is classified once. |
| UT-117 | `PolicyAttempt_RetryCreatesImmutableStartAndResultAndPreservesPriorFailure` | Attempt 1 start/result remain unchanged; retry creates attempt 2 records and a late result cannot replace the authoritative workflow outcome. |
| UT-118 | `PolicyAssessment_ReorderedOrDuplicatedEvidenceFixture_ProducesStabilityMeasurement` | Variation is recorded without relaxing deterministic safety. |
| UT-119 | `PolicyAssessment_PromptInjectionFixture_CannotChangeCapabilityOrPolicySet` | Retrieved instructions do not alter trusted rules. |
| UT-120 | `PolicyValidator_OutputAtSizeLimitAcceptedAndAboveLimitRejected` | The exact output bound is enforced. |

### 10.7 Policy mapping, candidate generation, ranking, and selection unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-130 | `PolicyProfile_EachInnerField_DoesNotExceedOuterLimit` | Property test covers all profile fields and permits equality where the policy declares it. |
| UT-131 | `PolicyProfile_AnyInnerLimitExceedsOuterLimit_IsRejected` | Theory mutates every bounded field beyond its outer limit. |
| UT-132 | `CandidateGenerator_SkipAbstentionOrAgentFailure_ReturnsEmpty` | No executable candidate is produced for any non-call outcome. |
| UT-133 | `CandidateGenerator_OneFreeBlock_ProducesAtMostOneAllocationPerBlock` | Coverage references are explicit. |
| UT-134 | `CandidateGenerator_AdjustedInactiveOrUnknownContract_IsRejected` | Only active standard MVP contracts qualify. |
| UT-135 | `CandidateGenerator_DuplicatePendingActionForContract_IsRejected` | Existing/pending exposure prevents duplicate action. |
| UT-136 | `CandidateAdmissibility_BoundaryMatrix_MatchesFrozenProfile` | Expiry, strike, delta, spread, liquidity, event, and quote boundaries are exhaustive. |
| UT-137 | `CandidateAdmissibility_InvalidMetric_IsRejectedNotRankedLast` | Invalid data cannot survive as a low score. |
| UT-138 | `Ranker_GoldenCandidates_ProducesGoldenScoresAndOrder` | Same candidates and versions reproduce exact ranking. |
| UT-139 | `Ranker_InputOrderOrCultureChanges_ResultIsUnchanged` | Stable sort and invariant parsing eliminate environmental drift. |
| UT-140 | `Ranker_EqualScores_UsesDeclaredTieBreakChain` | Every tie-break field is covered in order. |
| UT-141 | `Selector_TwoCandidatesShareOneCoverageBlock_SelectsOnlyWinner` | One share block cannot be allocated twice. |
| UT-142 | `Selector_ZeroAdmissibleCandidates_ReturnsNoTradeWithReasons` | Empty result is explicit and auditable. |
| UT-143 | `Selector_IdenticalInputs_RepeatedRuns_ProduceSameActionSetHash` | Selection is deterministic. |
| UT-144 | `Selector_AblationDiffersFromAiPolicy_RecordsMaterialContribution` | Difference is reported, not used as a silent fallback. |
| UT-145 | `Selector_ActionCountBoundary_NeverExceedsCycleLimit` | Exact maximum and one-above cases are tested. |

### 10.8 Final portfolio-risk and approval unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-150 | `RiskGate_AllRulesPass_IssuesApprovalBoundToExactInputs` | Approval contains action-set and account/market/policy/reconciliation identities plus expiry. |
| UT-151 | `RiskGate_AnySingleRuleFails_RejectsCompleteSet` | Theory independently fails every ordered rule. |
| UT-152 | `RiskGate_IndividuallyValidActionsCollectivelyOvercommitShares_Rejects` | Aggregate safety supersedes candidate validity. |
| UT-153 | `RiskGate_CombinedConcentrationExpirationOrPortfolioLimitExceeded_Rejects` | Aggregate exposure boundaries are covered. |
| UT-154 | `RiskGate_DuplicateOrConflictingOrderExists_Rejects` | Broker-visible conflicts block approval. |
| UT-155 | `RiskGate_DailyOrderLossErrorOrRetryLimitReached_RejectsNewEntry` | Lifecycle management remains separately allowed. |
| UT-156 | `RiskGate_PauseOrKillSwitchActive_Rejects` | Every control scope is covered. |
| UT-157 | `Approval_QuotePositionPermissionOrReservationChanges_IsInvalid` | Any bound state change invalidates the token. |
| UT-158 | `Approval_PolicyOrComponentVersionChanges_IsInvalid` | Rollout drift cannot reuse an old approval. |
| UT-159 | `Approval_ExactlyAtExpiry_IsInvalid` | Time boundary is unambiguous. |
| UT-160 | `Approval_ActionSetReorderedCanonically_SameHashButChangedActionDifferentHash` | Canonical ordering is stable; material mutation is detected. |
| UT-161 | `RiskGate_Rejection_StoresEveryRuleResultInStableOrder` | Evaluation is complete and auditable, not short-circuited invisibly. |
| UT-162 | `PreSubmitCheck_KillSwitchActivatesAfterApproval_BlocksExternalWrite` | Final control state wins. |
| UT-163 | `PreSubmitCheck_QuoteBecomesStale_BlocksAndRequiresRevalidation` | Old approval is not patched. |
| UT-164 | `ConcurrentReservation_LoserCannotProduceReadyIntent` | Application handles failed reservation as safe re-reconciliation; database atomicity is later integration-tested. |

### 10.9 Intent, submission, order-event, and reconciliation unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-170 | `IntentFactory_ApprovedAction_CreatesImmutableExactOrderFields` | Only approved fields enter intent. |
| UT-171 | `IntentFactory_SameLogicalAction_CreatesSameStableIntentAndClientOrderId` | Retry/restart identity is deterministic and within the 128-character limit. |
| UT-172 | `IntentFactory_ChangedExecutableField_ChangesIdentityAndRequiresNewApproval` | An old identity cannot disguise a changed order. |
| UT-173 | `ExecuteHandler_NoCommittedCheckpointOrReadyIntent_DoesNotCallBroker` | Fake broker observes zero writes. |
| UT-174 | `ExecuteHandler_ReadyIntent_CallsBrokerOnceWithStoredClientOrderId` | No generated-at-call identity is permitted. |
| UT-175 | `ExecuteHandler_DefinitiveAcceptance_RecordsAcceptedNotFilled` | Acceptance does not create a position. |
| UT-176 | `ExecuteHandler_DefinitiveRejection_RecordsReasonAndNoRetry` | Terminal rejection is not blindly retried. |
| UT-177 | `ExecuteHandler_TimeoutDisconnectOrAmbiguousBody_EntersSubmittedUnknown` | Ambiguous POST outcomes all choose reconciliation. |
| UT-178 | `ExecuteHandler_RateLimitBeforeKnownSubmission_FollowsFrozenAmbiguityRule` | 429 handling is explicit and cannot assume absence without proof. |
| UT-179 | `Reconciler_SubmittedUnknownLookupFindsOrder_AdoptsBrokerIdentityWithoutResubmit` | Accepted/open result prevents another POST. |
| UT-180 | `Reconciler_SubmittedUnknownLookupFindsFilledRejectedOrCanceled_MapsTerminalTruth` | Theory maps each result and writes no duplicate. |
| UT-181 | `Reconciler_SubmittedUnknownLookupAbsentButApprovalInvalid_SafeHalts` | Absence alone is insufficient for retry. |
| UT-182 | `Reconciler_SubmittedUnknownStillUnresolved_SchedulesBoundedPollAndNoWrite` | Unknown remains explicit. |
| UT-183 | `OrderObservation_DuplicateEvent_IsIdempotent` | No double fill or double reservation release. |
| UT-184 | `OrderObservation_OutOfOrderPartialAndFill_UsesMonotonicCumulativeTruth` | Event order cannot reduce filled quantity. |
| UT-185 | `PartialFill_RemainderRetainsCoverageAndOnlyRemainderMayBeCanceled` | Filled and working quantities are tracked independently. |
| UT-186 | `CancelHandler_FillWinsRace_RecordsFillAndDoesNotReleaseCoverageEarly` | Cancel acknowledgment/request is not terminal truth. |
| UT-187 | `ExternalReplaceObservation_OldOrderOrSuccessorSeen_ReconcilesWithoutCreatingIntent` | The MVP normalizes externally produced replacement statuses but never sends PATCH or assumes a successor. |
| UT-188 | `OrderChangeRequest_ConfirmedCancel_RequiresNewApprovalAndNewIntent` | Cancel-confirm-reapprove-new-submit remains separate controlled operations with a new identity. |
| UT-189 | `OrderStatus_UnknownFutureValue_RoutesToReconciliationAndAlert` | Forward compatibility fails safe. |
| UT-190 | `BrokerActivity_TradeCorrectOrTradeBust_ReconcilesPositionAndCashAgain` | Corrections create new observations and recalculate derived state. |

### 10.10 Open-position lifecycle, expiration, assignment, and corporate-action unit tests

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-200 | `Lifecycle_OpenCoveredCall_SchedulesNextReviewAndKeepsSharesReserved` | Normal position management begins from broker-confirmed fill. |
| UT-201 | `Lifecycle_ProfitTargetReached_ProducesCloseProposalThroughRiskPath` | It cannot bypass selection, approval, or intent controls. |
| UT-202 | `Lifecycle_UnderlyingSharesFallBelowCoverage_EntersCriticalSafeHalt` | The system alerts and does not pretend the call remains covered. |
| UT-203 | `Lifecycle_AgentOutage_ExistingObligationStillEvaluated` | Management remains deterministic. |
| UT-204 | `Lifecycle_MarketDataInvalid_CloseDecisionDefersAndEscalatesSafely` | Deterministic does not mean data-free. |
| UT-205 | `Assignment_ActivityObservedBeforeExpiry_ReconcilesShareDelivery` | Early assignment closes option obligation only with broker evidence. |
| UT-206 | `Assignment_ExDividendRiskWindow_TriggersConfiguredReviewOrCloseRule` | Dividend-risk rule is deterministic and versioned. |
| UT-207 | `Assignment_WebSocketHasNoEvent_RestActivityPollStillFindsOutcome` | Lifecycle correctness does not depend on option NTA WebSocket delivery. |
| UT-208 | `Expiration_ExactlyOneCentItm_StaysPendingUntilBrokerAssignmentTruth` | Broker behavior is anticipated but never inferred as final. |
| UT-209 | `Expiration_OtmAtLastQuote_StaysPendingUntilBrokerExpiryTruth` | Last quote alone cannot release shares. |
| UT-210 | `Expiration_ConfirmationDelayedAcrossWeekend_KeepsReservation` | Calendar delay does not produce premature reuse. |
| UT-211 | `PaperActivity_DelayedUntilNextDay_RemainsPendingWithoutDuplicateAction` | Paper NTA lag is handled explicitly. |
| UT-212 | `CorporateAction_SplitChangesContractDeliverable_PausesAndReconcilesSuccessor` | Standard candidate logic is disabled for adjusted obligations. |
| UT-213 | `CorporateAction_BrokerCancelsOrder_RecordsCanceledReasonAndReconcilesReservation` | Cancellation does not imply user intent or immediate replacement. |
| UT-214 | `Lifecycle_ClosePartiallyFilled_KeepsRemainingContractsAndSharesReserved` | Partial close is safe. |
| UT-215 | `Lifecycle_AssignmentAndLateFillConflict_SafeHaltsForBrokerReconciliation` | Contradictory truth is visible and not guessed. |
| UT-216 | `Lifecycle_NextReview_PersistsUtcInstantAndSurvivesClockZoneChange` | Review scheduling is durable and timezone-independent. |

### 10.11 Replay, explanation, controls, and cross-cutting invariants

| ID | Test method | Required assertion |
| --- | --- | --- |
| UT-220 | `FrozenReplay_StoredAcceptedAssessment_ReproducesExactDownstreamHashes` | Candidate, rank, set, risk, and intent identities match. |
| UT-221 | `FrozenReplay_LiveBrokerEvidenceOrAgentPortCalled_FailsTest` | Frozen replay uses immutable stored inputs only. |
| UT-222 | `FrozenReplay_CultureTimezoneAndInputEnumerationChange_ResultUnchanged` | Environmental variation does not alter deterministic output. |
| UT-223 | `FrozenReplay_CheckpointHashMismatch_FailsVisibly` | Corrupt or mismatched input cannot replay silently. |
| UT-224 | `AiReevaluation_NewAssessment_ProducesDiffWithoutMutatingProductionRecord` | Challenger output is evaluation-only. |
| UT-225 | `Simulation_FillPartialCancelExpireAssignMatrix_ProducesExpectedBrokerTruth` | Deterministic simulation covers the full declared outcome matrix. |
| UT-226 | `Simulation_FutureObservationRequested_IsRejectedAsLookAhead` | Historical decisions cannot see the future. |
| UT-227 | `ExplanationTemplate_CompletedDecision_ReferencesOnlyStoredFacts` | Baseline narrative is exact and reproducible. |
| UT-228 | `ExplanationAgent_MvpConfiguration_CannotBeInvokedOrAffectDecision` | The deferred component is disabled; deterministic templates remain authoritative. |
| UT-229 | `AuditRecord_SecretBearingInput_IsRedactedButCorrelationPreserved` | Secrets never enter audit/log payloads. |
| UT-230 | `RetryClassifier_ErrorMatrix_MatchesSafeRetryPolicy` | Read timeout, 429, 5xx, validation failure, ambiguous write, cancellation, and terminal rejection each map explicitly. |
| UT-231 | `Budget_TimeoutRetryAgentAttemptExternalCallAndActionLimitsAtBoundary_StopFurtherWork` | Exactly-at-limit and one-under cases are tested; model tool count remains zero. |
| UT-232 | `KillSwitch_Active_AllNewEntryPathsProduceZeroBrokerWrites` | Property test spans every node boundary after activation. |
| UT-233 | `SafetyInvariant_GeneratedCases_NeverExposeMoreCallsThanFreeCoverage` | Property-based core invariant with persisted failure seed. |
| UT-234 | `IdempotencyInvariant_RepeatedCommandOrEvent_ProducesAtMostOneLogicalEffect` | Property-based invariant covers workflow, intent, order observation, and lifecycle commands. |
| UT-235 | `AuditInvariant_EveryTerminalOutcomeHasReasonVersionsAndInputReferences` | No terminal state is unexplained or unreplayable. |

### 10.12 Unit-suite acceptance and traceability

The Phase 2A manifest therefore contains **180 stable unit-test IDs** (`UT-001` through `UT-235` with reserved gaps for local additions). Theory cases expand this to more individual discovered tests. The repository must include a machine-readable manifest mapping each stable ID to:

- owning phase and production component;
- fixture IDs;
- normal-flow or edge-case classification;
- exact expected public outcome and safety invariant;
- implementation status and last passing commit.

Completion rules:

1. all stable IDs in this section are discoverable;
2. no required test is skipped or quarantined;
3. all deterministic tests pass twice with randomized test order;
4. property tests meet the frozen case count and report replayable seeds;
5. mutation testing is required for coverage, policy mapping, candidate admissibility, portfolio risk, approval invalidation, and `SUBMITTED_UNKNOWN` decisions; surviving safety-critical mutants block completion;
6. line or branch coverage is diagnostic only and cannot replace the behavior manifest;
7. Phase 14 proves database and recorded SDK/HTTP boundaries; Phase 16 proves the real paper-broker boundary that unit tests intentionally do not claim to prove.

## 11. Required Scenario Catalogue

At minimum, test:

1. 99 shares: no candidate.
2. 100 free shares: at most one short-call contract.
3. 200 shares with one existing short call: at most one additional contract.
4. Shares reserved by a pending sale: reserved quantity is not coverage.
5. Adversarial risk-gate input bypasses node-12 allocation and collectively exceeds free shares: node 13 rejects the exact set, proving defense in depth without mutating it.
6. Existing pending option order: duplicate action rejected.
7. Missing, stale, crossed, or inconsistent quote: safe deferral.
8. Illiquid contract: deterministic rejection.
9. Options market closed: no new option order; no policy override exists.
10. Quote changes after approval: approval invalidated and action revalidated.
11. Partial fill: remainder tracked without duplicate submission.
12. Submission timeout with unknown result: reconcile before retry.
13. Restart after submission: recover from durable intent and broker truth.
14. Underlying shares change while a call is open: critical alert and safe lifecycle handling.
15. Near-expiration in-the-money call: deterministic close/hold/escalation policy executes; assignment itself is recognized only from broker truth.
16. Expiration: release obligation only after broker confirmation.
17. Assignment: reconcile shares and proceeds in simulation/replay.
18. Daily limit: stop new entries while allowed lifecycle management continues.
19. Valid `SKIP`: explicit no-trade result.
20. Valid `INSUFFICIENT_INFORMATION`: explicit AI abstention.
21. AI timeout or service failure: operational safe deferral, not valid abstention.
22. Missing required field in otherwise valid JSON: strict schema validation rejects safely.
23. Bullish trend, high volatility, negative news, and approaching earnings: expose conflict and select one allowed policy or abstain.
24. Attractive premium with excessive concentration: final portfolio risk rejects.
25. Filing contradicts a positive headline: cite both and lower confidence or abstain.
26. Stale or irrelevant negative article: exclude or prevent it from controlling policy.
27. Volatility spike or trading halt: deterministic controls pause new entries.
28. Prompt injection in retrieved text: treat source text as evidence, never instructions; the capability set and policy allowlist do not change.
29. Frozen replay: exact downstream decision reproduction.
30. AI reevaluation replay: record policy and evidence variation without allowing changed safety outcomes.
31. MVP configuration attempts to invoke an Explanation Agent: invocation is unavailable and deterministic reporting continues.
32. Duplicate scheduler tick: the unique schedule key creates only one workflow cycle.
33. Two workers claim the same due step: row locking and fencing allow only one current owner.
34. Worker lease expires during a slow handler: a newer owner may retry, while the stale worker cannot finalize or emit an outbox message.
35. Process stops after `SUBMISSION_PENDING` but before the Alpaca call: recovery reconciles first and does not submit blindly; this is the pre-call crash boundary, distinct from scenario 12's live timeout.
36. Process stops after Alpaca accepts the order but before response persistence: recovery finds the order by `client_order_id` and records one accepted order.
37. Outbox item is delivered again after completed broker submission: deduplication and intent state prevent a second broker-visible action.
38. PostgreSQL becomes unavailable after a broker response: the intent remains ambiguous until broker-first reconciliation succeeds; no optimistic retry occurs.
39. Agent returns syntactically valid content that semantically omits a material conflict: deterministic grounding validation rejects it.
40. Agent returns an unknown evidence identifier: deterministic validation rejects the assessment.
41. Agent returns an additional executable field: strict schema or deterministic validation rejects the assessment.
42. Agent response is refused, incomplete, or truncated: classify operational failure and safely defer new entry.
43. Agent runtime attempts a tool call or returns a tool request: reject and record a capability-contract violation.
44. Reordered or duplicated evidence changes the policy without a material semantic change: record a stability failure.
45. Runtime/model configuration differs from the frozen production version: block normal execution unless an explicit versioned rollout authorizes it.
46. A configured challenger passes schema but fails grounding or latency threshold: it remains offline-only; no local adapter is required merely to run this case.
47. `PolicyAssessmentV1` attempts to self-attest that no tool/session was used: operational acceptance ignores that claim and relies only on the system-owned attempt record.
48. Evidence builder attempts to label a source as bullish/supporting: contract validation rejects the semantic label because deterministic evidence records must remain neutral.
49. Complete normal new-entry flow: one valid assessment, one admissible candidate, one approved intent, broker acceptance, fill, open-call reconciliation, lifecycle scheduling, and terminal cycle audit.
50. Complete normal `SKIP`, abstention, deterministic-rejection, and management-during-agent-outage branches, each with its distinct terminal outcome and zero unintended broker writes.
51. Option-chain pagination stops early or repeats a page token: snapshot is incomplete and no candidate is generated.
52. Account receives indicative option data while the frozen policy requires OPRA: new entry defers with a feed-eligibility reason.
53. Holiday, early close, and daylight-saving transition: the decision follows broker calendar/clock instants rather than local machine time.
54. Fractional holdings total less than the next complete 100-share block: fractional remainder is never counted as coverage.
55. An adjusted contract has a non-standard multiplier or deliverable after a split: standard MVP generation pauses and lifecycle reconciliation continues.
56. Split, merger, special dividend, or symbol/CUSIP change affects an underlying with an order or position: stale symbols, contracts, and reservations are invalidated and successor activity is reconciled.
57. Options permission, account restriction, buying power, or configuration changes after approval: approval is invalidated before submission.
58. Two cycles attempt to reserve the same share block: exactly one commits an intent; the other re-reconciles without external write.
59. Broker emits `held`, `accepted_for_bidding`, `stopped`, `suspended`, or `calculated`: status normalizes to an explicit non-terminal/reconciliation action rather than an unknown success.
60. Broker introduces an unknown future status: order enters reconciliation and alerts; it is not treated as filled, canceled, or absent.
61. Duplicate or out-of-order stream updates arrive around a partial fill: cumulative fill and reservations never regress or double count.
62. A trade correction or trade bust changes an earlier fill: append a new broker observation and recompute position/cash truth.
63. Cancel request races with a fill: the fill wins broker truth and coverage is not released prematurely.
64. An external actor initiates replacement and the predecessor fills: both observed identities enter reconciliation; the MVP creates no successor intent and sends no replace request.
65. Early call assignment occurs well before expiration or during an underlying halt: activity polling discovers it and share delivery is reconciled before new coverage is calculated.
66. Ex-dividend risk window is entered with an in-the-money short call: configured deterministic close/review rule triggers regardless of AI availability.
67. Last expiry quote is OTM or exactly $0.01 ITM but broker activity is delayed: obligation remains pending confirmation and shares stay reserved.
68. Paper NTA assignment/expiration activity appears the next day: repeated management polls do not create a duplicate close or release.
69. Paper fill appears favorable despite absent queue position, market impact, or realistic displayed liquidity: evaluation flags the simulation limitation and does not promote a live-performance claim.
70. Culture, timezone, database enumeration order, or JSON property order changes during frozen replay: all deterministic hashes and downstream decisions remain exact.

## 12. Dependency Order and Parallel Work

Critical dependency chain:

`experiment contract → state-machine and cycle-template specifications → .NET solution/contract skeleton and strict JSON schemas → complete red unit-test acceptance baseline → PostgreSQL schema → leases/fencing/immutable attempts and results → transactional outbox/checkpoints/reservations → data normalization and reconciliation → features and bounded context → agent-runtime/direct-SDK spike and frozen narrow AI path → profile mapping and candidate generation → deterministic ranking and proposed action set → exact-set portfolio risk approval → unique persisted intent → crash-window integration proof → paper-enabled Alpaca execution and SUBMITTED_UNKNOWN reconciliation → deterministic lifecycle → replay and evaluation`

After schemas and the durable workflow foundation are stable, the following may proceed in parallel:

- data adapters and recorded fixtures;
- feature calculations;
- bounded context acquisition and agent evaluation;
- candidate calculations and ranking;
- risk rules and invariant scenarios;
- deterministic explanation templates and reporting;
- replay fixtures and execution simulation.

Execution integration must not begin until durable intent, idempotency, approval, reconciliation, and restart contracts are stable.

Broker-write implementation may be built in Phase 10 behind a non-network recorder. Real paper-broker submission remains disabled until the PostgreSQL outbox, intent and reservation uniqueness constraints, pre-write checkpoint, stale-fence rejection, and all four broker-submission crash-window tests pass in Phase 10.

No behavioral production phase may begin until Phase 2A has produced the complete discoverable unit-test baseline. No phase may be declared complete unless its Section 10 test IDs and all previously green unit tests pass.

## 13. MVP Completion Definition

The MVP is complete only when one evidence bundle shows all of the following without redefining the normative rules:

1. **Test gate:** all 180 stable IDs in Section 10 are present, unskipped, traceable, and green; required safety-critical mutation gates and Phase 14 boundary suites pass.
2. **Workflow gate:** every scheduled cycle uses a Section 5.2 template; all 18 node identities have a persisted `COMPLETED`, `SKIPPED`, `SAFELY_DEFERRED`, or `FAILED` outcome as applicable; lease/fencing recovery and atomic state/outbox behavior satisfy Sections 4 and 5.4.
3. **AI gate:** every eligible `NEW_ENTRY` cycle that reaches node 6 uses the one frozen stateless, zero-tool `IPolicyAssessor` path; early deterministic deferrals do not invoke AI. Accepted semantic output and system-owned attempt evidence satisfy Sections 6 and 7, with no silent provider/model fallback.
4. **Trading-safety gate:** profile bounds, coverage, deterministic ranking/selection, exact-set approval, reservations, intents, checkpointing, cancellation, execution, and ambiguous-result reconciliation satisfy Sections 4, 5.5, and 8; deferred scope satisfies Section 3.2.
5. **Lifecycle gate:** broker-confirmed entry plus close or expiration operates in paper trading; expiration and assignment pass simulation/replay; separate management cycles continue safely during AI outage.
6. **Audit and evaluation gate:** frozen replay is exact; AI reevaluation, ablation, grounding/stability results, every decision/failure/action/outcome, and paper-environment limitations are reproducible in the submission package.

## 14. Post-MVP Extension Order

1. Run challenger provider/model/runtime implementations behind frozen schemas and the same labelled evaluation set; keep local models offline until all live-decision gates pass.
2. Split the combined agent into Evidence Extraction and Overlay Policy agents only if measured grounding or policy-quality improvement justifies added latency, cost, and failure surface.
3. Add a read-only Grounding Critic offline first; promote it only for incremental detection, and keep it rejection/defer-only.
4. Add an Explanation Agent only after deterministic templates are insufficient and closed-fact validation is proven.
5. Improve deterministic covered-call ranking behind the same contracts.
6. Add cash-secured puts with verified cash reservation.
7. Add wheel transitions only after calls and puts are independently stable.
8. Add portfolio-aware allocation across multiple underlyings.
9. Add multi-leg structures only after atomicity and leg-risk handling are specified.
10. Add more realistic historical simulation and stress testing.
11. Evaluate Temporal, Durable Task, Dapr, or another external workflow platform only if post-MVP requirements exceed the deliberately small PostgreSQL kernel; migration must preserve the Domain contracts and PostgreSQL audit record.
12. Treat any live-capital design as a separate governed project.

## 15. Platform Decision Reference

Section 1.1 is the sole normative platform decision, Sections 5.4-5.6 own durability and topology, and Section 14 owns extension triggers. This section intentionally adds no duplicate platform requirement.

## 16. Implementation Handoff Rule

Implement in the dependency order of Section 12 and use the Section 10 tests as the acceptance baseline. Sections 4-8 remain authoritative for behavior; phases and scenarios are implementation and verification traceability, not competing specifications. A requested change that would alter those sections requires a reviewed plan-version update before production code or acceptance tests are weakened.