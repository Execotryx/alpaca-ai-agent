# Alpaca AI Agent — Phases 1–3 Compliance Remediation Implementation Plan

**Document version:** 1

**Created:** 29 August 2026

**Applies to repository baseline:** `bf335bc`

**Governing specification:** `Alpaca_AI_Agent_Framework_Neutral_Implementation_Plan.md`, version 7

## 1. Purpose

This plan defines the work required to bring the repository into demonstrable compliance with Phases 1, 2, 2A, and 3 of the governing implementation plan.

The current repository is not treated as an empty starting point. It contains:

- the .NET 10 modular-monolith solution skeleton;
- preliminary Domain and Application records and ports;
- the 180-ID Section 10 acceptance catalogue;
- 164 generated placeholder tests;
- 16 hand-written workflow tests for `UT-040` through `UT-055`;
- an in-memory workflow coordinator, catalogue, retry policy, finalization evaluator, scheduler, and conflict policy.

These assets may be retained when they satisfy the repaired acceptance contracts. They do not count as completed behavior merely because they compile or because their current tests pass. The remediation must establish traceable tests first, expose missing behavior as intentional failures, and then implement the smallest compliant production slices.

## 2. Required outcome

At completion:

1. Phase 1 state machines, strategy policies, outer limits, inner profiles, transition rules, and safe-stop rules are explicit, executable, and covered by the corresponding stable unit-test IDs.
2. Phase 2 contracts are complete, immutable, versioned, machine-validatable, infrastructure-independent, and represented by matching JSON Schema artifacts and valid fixtures.
3. Phase 2A is a genuine test-first acceptance baseline rather than a catalogue of generic placeholders.
4. Phase 3 workflow state is durable in PostgreSQL; in-memory state is disposable and never authoritative.
5. Every cycle persists all 18 node identities, applies one declared template, and records terminal outcomes or explicit skips.
6. Repeated scheduling, claiming, finalization, restart, or outbox dispatch cannot create duplicate durable effects.
7. Lease loss, fencing-token staleness, cancellation, timeout, deadline expiry, and database failure produce explicit safe outcomes.
8. PostgreSQL-backed pause controls and the global kill switch are checked at the required boundaries.
9. Test results and traceability evidence are reproducible from a clean checkout.

## 3. Scope

### 3.1 Included

- Repair of the Phase 2A acceptance harness and manifest.
- Replacement of generic placeholder tests with real public-behavior tests and controlled fixtures.
- Completion of Phase 1 state-machine and strategy-policy behavior.
- Completion of the Phase 2 contract skeleton, JSON Schemas, ports, fixtures, and persistence mapping definitions.
- Implementation of the Phase 3 PostgreSQL schema and durable workflow kernel.
- Refactoring or replacement of the existing in-memory workflow prototype where required.
- Unit, contract, PostgreSQL integration, restart, concurrency, and failure-injection tests needed to prove Phases 1–3.
- Documentation and traceability updates required by the governing plan and `AGENTS.md`.

### 3.2 Excluded

- Alpaca network adapters and broker-data acquisition from Phase 4.
- Feature calculation and evidence acquisition from Phase 5.
- A production AI/provider adapter from Phase 6.
- Candidate, ranking, risk, and execution behavior from later phases, except for compile-time contracts and test doubles required by Phase 2A.
- Any real paper-broker write.
- Native order replacement, rolling, cash-secured puts, multi-leg strategies, live-capital trading, and other deferred scope.

### 3.3 Safety boundary

The remediation must not make any network-capable broker-write adapter reachable. Any execution-facing port remains a recorder or controlled fake until the later Phase 10 crash-window gates pass.

## 4. Baseline defects to close

### 4.1 Phase 1 defects

- `PortfolioEligibilityState` does not represent all specified eligibility states.
- `OrderState` is not the Section 8 normalized order state machine and lacks required replacement/reconciliation behavior.
- `OptionLifecycleState` does not represent the complete option-position lifecycle.
- No state machine defines allowed source/event/target transitions.
- Invalid transitions, duplicate events, out-of-order events, and safe stops are not implemented.
- Covered-call entry, hold, close, expiration, assignment, and roll-rejection policies are not executable.
- Immutable outer limits and policy-specific inner profiles are absent.
- No field-by-field proof or executable invariant shows that every inner profile stays within outer limits.

### 4.2 Phase 2 defects

- Several Section 7 records are partial projections rather than complete contracts.
- `PolicyInputPackageV1` is absent.
- `EvidenceBundleV1` is not defined explicitly as a versioned contract.
- Unknown, invalid, unavailable, and numeric zero are not distinct domain values.
- Workflow, policy, profile, schema, feature, prompt, model, runtime, and component versions are not all independently represented.
- Contract field ownership and provenance are incomplete.
- JSON Schema artifacts for Section 7 contracts are absent.
- Strict AI output constraints are not machine-validatable.
- Required scenario fixtures are absent.
- Application ports are incomplete for persistence, workflow execution, outbox dispatch, and alerts.
- PostgreSQL mappings, JSONB version rules, relational indexes, and migration ownership are undefined in code.
- The API and Worker still contain default template behavior that is unrelated to the application contract.

### 4.3 Phase 2A defects

- 164 stable-ID tests call one generic `AssertImplemented` method rather than their owning public contracts.
- Test bodies do not establish real setup, inputs, outputs, or safety assertions.
- Required fixtures, boundary matrices, fake-clock rules, canonical outputs, and golden hashes are absent.
- The manifest's `fixtureIds` values are not arrays and therefore violate the repository's own schema.
- Catalogue integrity does not validate the manifest against its JSON Schema.
- `lastPassingCommit` contains `WORKTREE` rather than an actual tested commit reference.
- Some current green workflow tests are weaker than their stable-ID assertions.
- Empty placeholder tests in the non-unit test projects can pass without testing a boundary.

### 4.4 Phase 3 defects

- No PostgreSQL migrations or Persistence implementation exist.
- The current coordinator and scheduler keep authoritative state in memory.
- Duplicate schedule protection does not survive restart or multiple processes.
- Node dependency, input-version, timeout, retry, and terminal-outcome metadata are not durably registered.
- Attempts and results are not append-only database records.
- Claiming, leases, database time, and monotonically increasing fencing tokens are not implemented.
- Step handlers are not separated from short claim/finalization transactions.
- Finalization is not conditioned by an atomic lease-owner and fencing-token check.
- State, output references, dependent activation, domain events, and outbox messages are not committed atomically.
- Checkpoints, transactional outbox dispatch, recovery, controls, and health checks are absent.
- `REPLAY_SIMULATION` is rejected rather than instantiated from a recorded source template.
- The Phase 3 exit criterion cannot be demonstrated at every node and restart boundary.

## 5. Governing implementation rules

1. Preserve every Section 10 stable ID and test name unless a reviewed version of the governing plan changes it.
2. Do not make a test green by weakening its assertion, replacing its input with a trivial value, or routing it through a generic success object.
3. Tests target public Domain/Application behavior. Persistence tests target public repository/kernel operations and observable database effects, not literal SQL text.
4. Write or strengthen the test before changing the corresponding production behavior.
5. Record the focused red result and prove it is caused by missing behavior rather than a broken fixture or setup.
6. Keep Domain and Application free of Npgsql, EF Core, Alpaca SDK, Agent Framework, OpenAI SDK, and provider-specific models.
7. Use PostgreSQL as the only workflow authority. An in-memory implementation may exist only as a test double or frozen replay adapter.
8. Use database UTC time for leases and claim ordering. Application-local time cannot grant authority.
9. Do not hold a database transaction open during a node handler or external network call.
10. Treat attempt starts, attempt results, checkpoints, domain events, broker operations, and broker observations as immutable append-only evidence.
11. A stale worker may record a diagnostic late observation but cannot insert the authoritative result or activate successors.
12. Every state change and outbox item caused by that change commits in the same transaction.
13. Use stable ordering, invariant-culture conversion, canonical serialization, and explicit contract versions at identity boundaries.
14. Preserve the existing 16 workflow implementations only where repaired tests prove their complete public contract.

## 6. Delivery sequence

The work is divided into remediation stages `R0` through `R8`. They must execute in order unless a stage explicitly permits parallel work.

```text
R0 baseline freeze
  -> R1 acceptance infrastructure repair
  -> R2 Phase 1 specification freeze, Phase 2 contract skeleton, and real Phase 2A red baseline
  -> R3 Phase 1 state machines and policies
  -> R4 Phase 2 contract semantics and validation
  -> R5 Phase 3 PostgreSQL schema
  -> R6 durable workflow operations
  -> R7 recovery, controls, dispatch, and health
  -> R8 compliance evidence and handoff
```

No Phase 3 persistence behavior may be counted as complete before `R2`. Existing behavior may remain in the tree during `R1` and `R2`, but repaired acceptance tests must be allowed to fail against it honestly.

## 7. R0 — Freeze and inventory the baseline

### 7.1 Tasks

1. Record the starting commit, plan version, .NET SDK version, package-lock state, and PostgreSQL test image version.
2. Produce a machine-readable inventory containing:
   - all 180 stable IDs;
   - current test method and class;
   - expected assertion from Section 10;
   - current production target;
   - current fixture IDs;
   - current implementation status;
   - current evidence quality: placeholder, partial, or complete.
3. Mark `UT-040` through `UT-055` as `PARTIAL_REVIEW_REQUIRED` in a remediation ledger without changing the governing manifest enum prematurely.
4. Record the precise behavior already present in `WorkflowCoordinator`, `WorkflowCatalog`, `WorkflowRetryPolicy`, `WorkflowFinalizer`, `IdempotentCycleScheduler`, and `CycleConflictPolicy`.
5. Identify default template files and empty tests that must be removed or replaced when their owning stage begins.
6. Confirm that no secrets, broker credentials, or network-capable broker-write implementation exist.

### 7.2 Deliverables

- `docs/compliance/phases-1-3-baseline-inventory.md`.
- `tests/AlpacaAgent.UnitTests/acceptance-remediation-ledger.json` with a schema.
- Initial validation transcript stored as a CI artifact, not committed if it contains machine-specific paths.

### 7.3 Exit criteria

- Every stable ID and existing implementation has one recorded owner.
- No current green test is accepted without an assertion-by-assertion review.
- The worktree is clean and reproducible before test infrastructure changes begin.

## 8. R1 — Repair acceptance infrastructure and traceability

### 8.1 Manifest model

Replace loosely generated JSON with a typed manifest model and deterministic serializer. Keep the published JSON Schema authoritative.

Each entry must contain:

- `stableId`;
- exact `method`;
- owning plan phase and component;
- `fixtureIds` as an array, including an empty array when no fixture is needed;
- normal-flow or edge-case classification;
- exact public outcome;
- separate safety invariant;
- implementation status;
- last passing commit or `null`.

The generator must never convert empty arrays to `null` or one-element arrays to strings.

### 8.2 Integrity gate

Extend `AcceptanceCatalogIntegrityTests` to prove:

1. the manifest validates against `acceptance-manifest.schema.json`;
2. exactly 180 unique stable IDs are present;
3. each ID has exactly one discovered test method;
4. every method name matches the specification;
5. every entry has a real owning component and phase;
6. every referenced fixture exists;
7. every `IMPLEMENTED` entry has a non-placeholder test body and a non-null tested commit reference;
8. no required test carries skip, explicit, quarantine, category-exclusion, or conditional-disable metadata;
9. no generated acceptance class derives from the generic placeholder boundary after `R2` completes;
10. the list of IDs in the repository matches the list parsed from plan version 7.

Add an explicit manifest-derived gate trait that distinguishes behavior activated through the current completed phase from future-phase expected-red behavior. This trait is scheduling metadata, not a skip: every test remains discoverable and runnable, and the unfiltered baseline audit still executes all 180 IDs.

### 8.3 Passing-commit workflow

Avoid the impossible requirement for a commit to contain its own hash:

1. commit the production/test slice;
2. run the required clean-checkout verification against that commit;
3. create a traceability-only follow-up commit that stamps the verified code commit into `lastPassingCommit`;
4. rerun the integrity and focused tests;
5. never stamp `WORKTREE`, a branch name, or an untested commit.

### 8.4 Generator rules

- Generation may create a manifest entry and an explicitly failing test scaffold only for a newly approved stable ID.
- Generation must not overwrite a hand-written test.
- The scaffold must identify the required public contract and fixture, not call a universal fake contract.
- The generator must fail if a fixture reference cannot be resolved.
- Output must be byte-stable under repeated generation.

### 8.5 Exit criteria

- The manifest validates against its schema.
- Regeneration produces no diff.
- Integrity checks fail when an ID, fixture, trait, method, or commit reference is deliberately corrupted.
- Placeholder success cannot be introduced through `SpecificationAcceptanceSkeleton`.

## 9. R2 — Freeze Phase 1–2 test targets and establish the genuine Phase 2A red baseline

### 9.1 Prerequisite specification and compile-time contract freeze

The original ordering still applies during remediation: Phase 1 specifications and Phase 2 published contract shapes must exist before the Phase 2A baseline is accepted. Complete these items without adding behavioral implementations:

1. Freeze the three Phase 1 state lists, typed event lists, transition-matrix rows, invalid-transition outcomes, covered-call rules, outer-limit fields, and inner-profile fields as review-owned specification artifacts.
2. Freeze ownership, provenance, missingness, and independent-version rules for every Section 7 field.
3. Add or correct immutable C# record shapes for every Section 7 contract, including `EvidenceBundleV1`, `PolicyInputPackageV1`, accepted-assessment envelopes, checkpoints, reservations, and immutable attempt/result pairs.
4. Add source-generated serialization registrations and versioned draft JSON Schemas that express the published shape. Schema-validation services and behavioral factories may remain explicitly unimplemented.
5. Define all required Application port signatures and typed outcomes without adding Infrastructure or Persistence implementations.
6. Define the Persistence mapping specification—tables, keys, JSONB versions, indexes, immutability, and ownership—without creating migrations yet.
7. Freeze fixture schemas, boundary-matrix schemas, fake-clock behavior, canonical ordering, and hash rules.
8. Review the resulting public surface for forbidden infrastructure types before tests are generated or rewritten against it.

After this prerequisite, `R2` tests must compile against the real intended public contracts. Later `R4` work implements validation, canonicalization, migration, and serialization semantics without casually changing these frozen shapes.

### 9.2 Test organization

Retain the Section 10 domains but replace generic generated bodies with explicit tests:

- `Contracts` — `UT-001` through `UT-012`;
- `StateMachines` — `UT-020` through `UT-032`;
- `Workflow` — `UT-040` through `UT-055`;
- `Reconciliation` — `UT-060` onward as assigned by Section 10;
- `Features` and `Evidence`;
- `PolicyAssessment`;
- `Candidates`, `Selection`, and `Risk`;
- `Execution`;
- `Lifecycle`;
- `Replay` and `Reporting`;
- cross-cutting controls and invariants.

Tests for later behavior remain red, but each must call a real compile-time port, service, validator, factory, state machine, or explicitly named unimplemented stub owned by that component. A shared `stableId -> NOT_IMPLEMENTED` router is prohibited.

### 9.3 Fixture catalogue

Create immutable, review-owned fixtures for at least:

- valid covered-call evaluation;
- `SKIP` no-action result;
- grounded `INSUFFICIENT_INFORMATION` abstention;
- model timeout/refusal/rate-limit/truncation;
- deterministic candidate rejection;
- portfolio-risk rejection;
- partial fill and cancel/fill race;
- crash and restart at each workflow node;
- expiration pending broker confirmation;
- early assignment;
- split or adjusted contract;
- duplicate and out-of-order broker observations;
- stale quote, incomplete option-chain pagination, and feed downgrade;
- prompt-injection evidence and conflicting sources.

Every fixture must have:

- a stable fixture ID;
- schema version;
- decision time and source times where relevant;
- canonical ordering rule;
- canonical content hash;
- provenance and expected validation state;
- an owning test list;
- a rule forbidding production code from rewriting it.

### 9.4 Boundary matrices

Create machine-readable matrices for:

- every declared state transition;
- raw broker status normalization;
- inclusive/exclusive numeric thresholds;
- retry classification;
- workflow template applicability;
- lifecycle assignment and expiration events;
- pause and kill-switch boundaries.

Tests must enumerate all matrix rows. The integrity gate must compare the discovered theory-case count with the matrix row count.

### 9.5 Repair the existing 16 workflow tests

Strengthen `UT-040` through `UT-055` before accepting their current green state:

- `UT-040`: supply typed valid node outputs and assert dependency order, exact node states, output references, and pending execution state.
- `UT-041`: require a broker-confirmed fill observation and assert a durable lifecycle obligation plus due management-cycle record.
- `UT-042`–`UT-046`: assert absence of candidate, approval, intent, reservation, and outbox artifacts at the public repository boundary.
- `UT-047`: use an agent port that fails if invoked and prove management still reaches its deterministic lifecycle path.
- `UT-048`: provide inconsistent broker inputs and assert persisted safe halt plus reason.
- `UT-049`: attempt to execute an inapplicable node and an applicable node with an unmet dependency; prove both are blocked for distinct reasons.
- `UT-050`–`UT-051`: assert append-only attempt evidence, next due time, budget consumption, and terminal escalation.
- `UT-052`: complete a handler after its deadline and prove no successor or outbox activation occurs.
- `UT-053`: prove cancellation reaches the port and is persisted as a classified attempt result.
- `UT-054`: keep a unit-level repository behavior test, then add a PostgreSQL uniqueness proof in `R6`.
- `UT-055`: assert management priority through the persisted conflict/locking boundary, not only a Boolean helper.

### 9.6 Expected red-baseline procedure

For every stable ID:

1. compile successfully;
2. discover the test and all declared theory cases;
3. load and validate its fixtures;
4. reach the owning public boundary;
5. fail at the exact missing outcome assertion;
6. emit no unexpected exception from setup or shared infrastructure.

Record one structured red-baseline result per ID. Tests already backed by correct behavior may remain green only after the assertion review confirms they are non-vacuous.

### 9.7 Exit criteria

- All 180 IDs and all theory cases are discoverable.
- No generic acceptance router remains.
- Every fixture validates and every canonical hash is stable.
- Every red result is attributable to missing behavior.
- The 16 formerly green workflow IDs have been independently accepted, strengthened and green, or exposed as red gaps.
- No production behavior is added during this stage; only frozen specification artifacts, contract/schema shapes, port signatures, fixtures, and explicitly unimplemented public boundaries may be added.

## 10. R3 — Implement Phase 1 state machines and policies

### 10.1 Portfolio eligibility state machine

Define explicit states matching Section 8.1:

- `NoApprovedHolding`;
- `ApprovedButInsufficientShares`;
- `EligibleSharesAvailable`;
- `SharesPartiallyReserved`;
- `NoFreeCoverage`;
- `InconsistentOrUnreconciled`;
- `Paused`.

Define typed events such as approval added/removed, broker snapshot reconciled, shares reserved/released, obligation observed, inconsistency detected/resolved, and pause activated/cleared.

For every state/event pair, declare:

- allowed target state;
- required guard inputs;
- emitted domain facts;
- whether the event is idempotent;
- safe-stop behavior for invalid or contradictory input.

An invalid transition must return a typed rejection or safe-halt result without mutating the prior state.

### 10.2 Order state machine

Implement the exact normalized states in Section 8.2 and keep raw Alpaca status strings outside Domain.

Create a normalization table covering:

- working statuses;
- partial fill;
- pending cancel;
- filled, canceled, rejected, and expired;
- externally observed pending replacement;
- externally observed completed replacement;
- uncommon nonterminal statuses requiring reconciliation;
- unknown future statuses.

The reducer must process append-only observations using deterministic ordering. Duplicate observations are idempotent. Older observations cannot regress terminal truth. Trade corrections and busts must create a newly derived state rather than modifying historical evidence.

### 10.3 Option-position lifecycle state machine

Represent all Section 8.3 states separately, including entry order open, short call open, close review, close order open, near expiration, assignment possible, expired pending confirmation, assigned pending reconciliation, closed, and exception/safe halt.

Assignment may occur before expiration. Expiration quotes cannot release coverage without broker confirmation. Partial close releases only broker-confirmed closed quantity.

### 10.4 Covered-call policy contracts

Define immutable outer limits for:

- allowed structures and symbols;
- standard contract multiplier and deliverable requirements;
- maximum contracts per verified share block;
- DTE and strike boundaries;
- liquidity and spread rules;
- event and ex-dividend constraints;
- concentration and exposure limits;
- action, retry, loss, and error budgets;
- entry, close, expiration, and assignment rules.

Define `CONSERVATIVE_CALL`, `BALANCED_CALL`, and `INCOME_CALL` inner profiles. Add a deterministic validator that evaluates every bounded field against the outer limits and reports all violations in stable order.

MVP roll requests must produce one explicit out-of-scope result and no close, entry, intent, or broker-write artifact.

### 10.5 Tests and exit criteria

Required stable IDs: `UT-020` through `UT-032`, plus profile-bound tests `UT-130` and `UT-131` when the profile records become executable.

Phase 1 remediation exits only when:

- every declared transition matrix row is covered;
- invalid and duplicate events are covered;
- raw broker statuses normalize exactly once;
- lifecycle rules cover early assignment and delayed expiration confirmation;
- every inner profile passes the subset validator;
- a mutation of every bounded profile field outside the outer limit is rejected.

## 11. R4 — Implement Phase 2 contract semantics and validate component boundaries

### 11.1 Value types

Introduce validated value types for identifiers, symbols, contract identifiers, quantities, money, ratios, UTC instants, version identifiers, hashes, and provenance references.

Introduce an explicit optional/observed value union with distinct states:

- known value, including numeric zero;
- unknown;
- unavailable;
- invalid with a stable reason.

Serialization and persistence mapping must preserve these distinctions.

### 11.2 Complete Section 7 contract semantics

Using the public shapes frozen in `R2`, implement factories, validators, canonicalization, serialization behavior, and supported-version migration for:

- workflow cycle and budgets;
- market and account snapshots;
- reconciled portfolio;
- `EvidenceBundleV1` and evidence records;
- `PolicyInputPackageV1`;
- model-produced `PolicyAssessmentV1`;
- system-owned accepted-assessment envelope;
- agent attempt start and result;
- strategy policy and profiles;
- candidate and ranked candidate;
- proposed action set;
- portfolio risk decision and approval token;
- order intent;
- decision record;
- workflow step attempt and attempt result;
- checkpoint;
- outbox message;
- share reservation;
- broker request attempt, observation, and reconciliation result;
- domain event and immutable snapshot/decision references.

Do not merge attempt starts with results. Do not place provider/runtime facts inside model-produced output.

### 11.3 Version model

Represent independently:

- workflow definition;
- contract schema;
- policy;
- inner profile;
- feature calculation;
- prompt;
- requested and resolved provider/model;
- reasoning configuration;
- runtime/connector/provider SDK;
- deterministic decision logic;
- persistence mapping.

A change in one version cannot silently imply or overwrite another.

### 11.4 JSON Schema enforcement

Finalize and enforce the draft 2020-12 schemas introduced in `R2` under versioned paths such as:

```text
schemas/contracts/v1/
  evidence-bundle.schema.json
  policy-input-package.schema.json
  policy-assessment.schema.json
  agent-attempt.schema.json
  agent-attempt-result.schema.json
  workflow-cycle.schema.json
  workflow-step-attempt.schema.json
  workflow-step-attempt-result.schema.json
  order-intent.schema.json
  portfolio-risk-decision.schema.json
```

External and AI schemas must use required fields, `additionalProperties: false`, bounded arrays and strings, allowlisted enums, and evidence-reference constraints. `PolicyAssessmentV1` must make executable order fields structurally impossible.

Add round-trip tests between C# source-generated serialization and approved JSON fixtures. Add negative tests for unknown fields, missing fields, unknown enums, malformed quantities, and unsupported versions.

### 11.5 Application ports

Define narrow ports for:

- broker reads;
- broker writes;
- evidence retrieval;
- policy assessment;
- clock and database-time abstraction;
- workflow cycle creation and lookup;
- due-step claiming and finalization;
- checkpoint and immutable evidence append;
- outbox claiming and completion;
- reconciliation dispatch;
- control-state queries;
- alerts and health observations.

Ports must express typed results and failure classifications. They must not expose Npgsql commands, EF entities, HTTP responses, Alpaca DTOs, or provider SDK types.

### 11.6 Persistence mapping specification

Before migrations, document for every record:

- table ownership;
- primary and foreign keys;
- immutable versus projection columns;
- JSONB payload and contract version;
- unique constraints;
- claim/due indexes;
- append-only enforcement;
- terminal-state immutability;
- retention and diagnostic references.

### 11.7 Skeleton cleanup

- Remove the random weather endpoint.
- Replace the logging-only Worker loop with disabled composition roots that register no scheduler until Persistence is configured.
- Replace vacuous `UnitTest1` methods with real boundary-suite scaffolds or remove them when the project legitimately has no Phase 1–3 test yet.
- Keep Infrastructure and Persistence free of unused SDK dependencies.

### 11.8 Exit criteria

- All required contracts compile and are immutable.
- Every schema validates its positive fixtures and rejects its negative fixtures.
- Domain and Application dependency checks reject forbidden infrastructure assemblies.
- Every workflow node can be replaced by a controlled test double using published contracts.
- Phase 2 contract tests `UT-001` through `UT-012` are green without weakening the Phase 2A baseline.

## 12. R5 — Implement the Phase 3 PostgreSQL schema

### 12.1 Package and migration setup

- Add pinned Npgsql and EF Core migration packages only to Persistence.
- Use EF Core migrations for schema evolution.
- Use explicit Npgsql transactions or targeted SQL for claim, fencing, append-only, and uniqueness-critical operations.
- Add a Testcontainers PostgreSQL dependency to PersistenceTests.
- Pin the PostgreSQL container version used in CI.

### 12.2 Required tables

Create migrations for:

- `workflow_cycles`;
- `workflow_steps`;
- `workflow_step_attempts`;
- `workflow_step_attempt_results`;
- `workflow_checkpoints`;
- `outbox_messages`;
- `order_intents`;
- `share_reservations`;
- `agent_attempts`;
- `agent_attempt_results`;
- `broker_operations`;
- `broker_observations`;
- `domain_events`;
- immutable market/account/evidence/assessment/decision snapshots;
- lifecycle obligations and durable review schedules;
- control state for manual, symbol, strategy, and global pauses.

### 12.3 Minimum constraints

- Unique cycle primary key and unique scheduled-cycle key.
- Unique `(cycle_id, step_id)` across all 18 nodes.
- Unique `(cycle_id, step_id, attempt_number)` for step attempts.
- One authoritative final result per attempt, while diagnostic late results remain insert-only.
- Monotonically increasing fencing token on each newly acquired claim.
- Unique checkpoint identity and content/version hash.
- Unique outbox deduplication key.
- Unique stable intent key and unique Alpaca `client_order_id`.
- Reservation constraints preventing the same verified share block from being committed twice.
- Foreign keys linking every result, checkpoint, outbox item, operation, and observation to its owning cycle/step/intent where applicable.
- Check constraints for valid state transitions, nonnegative budgets, bounded attempts, and lease consistency.
- Terminal cycle and terminal authoritative result immutability.

### 12.4 Append-only enforcement

Application code must expose insert operations only for append-only tables. Add database protection against update/delete for append-only evidence, using privileges or triggers where compatible with migration and test environments.

Projection rows such as `workflow_steps`, current outbox status, and normalized intent state may update only through guarded repository operations. Historical source evidence is never overwritten.

### 12.5 Migration tests

Prove:

- clean database migration from zero;
- idempotent startup migration check without destructive downgrade;
- all required tables, indexes, foreign keys, unique constraints, and checks exist;
- duplicate schedule, attempt, authoritative result, intent key, client order ID, reservation, checkpoint, and outbox key are rejected;
- append-only rows cannot be updated or deleted through the application role;
- old supported JSON contract versions can be read or explicitly migrated;
- unsupported versions fail visibly.

### 12.6 Exit criteria

- A clean PostgreSQL container reaches the expected schema.
- Constraint tests prove the invariants using real concurrent transactions.
- Persistence contains no broker network calls.

## 13. R6 — Implement durable workflow operations

### 13.1 Scheduled-cycle creation

In one short transaction:

1. read database time and required control state;
2. reject or defer scheduling when the applicable pause or kill switch is active;
3. insert or retrieve the cycle by unique schedule key;
4. persist all 18 workflow steps;
5. mark inapplicable nodes `SKIPPED` with the template routing reason;
6. append node 1 attempt-start and authoritative result records;
7. activate the first applicable successor;
8. commit.

Repeated ticks must return the same cycle identity. Concurrent schedulers must produce one durable cycle.

For `REPLAY_SIMULATION`, require the recorded source template and persist the copied applicable-node set. Live broker, evidence, and policy ports remain forbidden in frozen replay.

### 13.2 Due-step claiming

Use a short transaction and database locking, such as `FOR UPDATE SKIP LOCKED`, to:

1. select due `PENDING` work whose dependencies are terminal and whose cycle is runnable;
2. check control state and deadline;
3. assign lease owner and database-time expiry;
4. increment the fencing token monotonically;
5. increment the attempt number;
6. append the attempt-start record with immutable input references and versions;
7. commit before invoking the handler.

Two workers must never both hold current authority for the same step. The losing worker may receive no work or a typed conflict result.

### 13.3 Handler execution

- Load immutable input references after claim commit.
- Execute the node handler without an open PostgreSQL transaction.
- Pass cancellation and deadline information through every port.
- Capture a typed result: completed, no action, safe deferral, retryable failure, terminal failure, canceled, waiting, or lost authority.
- Never let a handler directly activate a successor or insert an outbox item outside finalization.

### 13.4 Guarded finalization

In one short transaction:

1. lock the current step projection;
2. compare lease owner, fencing token, lease expiry, cycle deadline, and cancellation/control state using database time;
3. insert the authoritative attempt result only if authority remains valid;
4. otherwise insert a diagnostic late result without advancing the step;
5. persist output references and immutable checkpoints;
6. append domain events;
7. insert every caused outbox item with a stable deduplication key;
8. transition the step and activate eligible dependents;
9. finalize the cycle when all applicable nodes are terminal;
10. commit atomically.

No outbox item may exist without the state that caused it, and no caused state may commit without its outbox item.

### 13.5 Retry and terminal behavior

- Retry only classified safe units.
- Persist next due time, consumed budget, failure class, and prior attempt references.
- Do not overwrite prior attempts.
- At exact deadline or exhausted budget, prevent successor activation.
- Classify cancellation separately from model abstention, no action, and operational failure.
- Mark every remaining applicable step `SKIPPED` only when the declared terminal routing rule requires it.

### 13.6 Required tests

Unit tests:

- repaired `UT-040` through `UT-055`;
- any Phase 3-relevant cross-cutting test selected from `UT-230` through `UT-235`.

PostgreSQL tests with local IDs:

- `P3-DB-001` duplicate schedule ticks return one cycle;
- `P3-DB-002` all 18 nodes persist with exact applicability;
- `P3-DB-003` node 1 attempt/result is immutable and scheduler-owned;
- `P3-DB-004` concurrent claims produce one current owner;
- `P3-DB-005` fencing tokens increase across reclaim;
- `P3-DB-006` stale finalization cannot advance state;
- `P3-DB-007` valid finalization atomically activates the successor;
- `P3-DB-008` state and outbox rollback together;
- `P3-DB-009` retry appends a new attempt and preserves the old result;
- `P3-DB-010` exact-deadline completion loses authority;
- `P3-DB-011` cancellation persists a classified result;
- `P3-DB-012` terminal cycles reject further advancement;
- `P3-DB-013` process restart resumes from durable state;
- `P3-DB-014` every template restarts at every applicable node;
- `P3-DB-015` replay uses its recorded source template.

### 13.7 Exit criteria

- The in-memory scheduler is removed from production composition or retained only as a test fake.
- PostgreSQL is the sole source of workflow truth.
- All Phase 3 unit and PostgreSQL tests pass twice, including concurrent runs.

## 14. R7 — Recovery, outbox, controls, and health

### 14.1 Expired-lease recovery

Implement a recovery scan that:

- uses database time;
- finds expired `RUNNING` steps;
- classifies the prior attempt as lost/expired without overwriting it;
- applies bounded retry policy;
- makes safe work due again with a newer fencing token;
- safe-halts work that cannot be repeated safely;
- prevents a stale worker from finalizing afterward.

### 14.2 Transactional outbox dispatcher

Implement due-message claiming with the same bounded lease and fencing model used for workflow steps.

The Phase 3 dispatcher may deliver only non-broker test messages, alerts, and recorder-bound messages. Broker-write delivery remains disabled.

Required behavior:

- unique deduplication key;
- append-only payload;
- bounded attempts and next due time;
- claim outside the state-producing transaction;
- delivery outside every database transaction;
- guarded completion using current fencing authority;
- redelivery without duplicate logical effects;
- visible dead-letter or safe-halt outcome after budget exhaustion.

### 14.3 Reconciliation dispatcher skeleton

Create the durable dispatcher contract and due-work scheduling needed by later phases. It may call only controlled test ports in Phase 3. It must not invent broker truth or enable paper writes.

### 14.4 Controls

Implement PostgreSQL-backed:

- manual global pause;
- symbol pause;
- strategy pause;
- global kill switch.

Check controls:

1. before cycle scheduling;
2. before step claiming where applicable;
3. during guarded finalization if an external side effect would be created;
4. immediately before every later external write.

Activation after approval must still prevent an outbox item or external call. Control changes must be audited with actor/source, time, scope, reason, and prior/new value.

### 14.5 Health checks

Expose health observations for:

- PostgreSQL connectivity;
- claim-loop heartbeat and oldest due-step age;
- expired lease count;
- stale-fence rejection count;
- outbox depth and oldest due-message age;
- repeated recovery or dead-letter state;
- kill-switch state.

Health endpoints are read-only. They cannot mutate workflow or control state.

### 14.6 Recovery and failure-injection tests

Test process termination or simulated crash:

- before claim commit;
- after claim commit but before handler start;
- during handler execution;
- after handler completion but before finalization;
- during finalization before commit;
- after finalization commit but before acknowledgment;
- after outbox claim but before dispatch;
- after dispatch but before guarded completion;
- after lease expiry and takeover by a second worker.

Every case must yield one of:

- one committed durable effect;
- a bounded retry with preserved history;
- a visible safe halt.

No case may activate a successor twice, finalize with a stale fence, or duplicate an outbox effect.

### 14.7 Exit criteria

- Recovery works after complete process disposal and a new process start.
- Pause and kill-switch tests pass at every required boundary.
- Outbox redelivery is effectively once at the logical-effect boundary.
- Health checks report intentionally injected unhealthy states.

## 15. R8 — Final compliance verification and evidence

### 15.1 Clean-checkout validation

From a fresh clone at the candidate commit:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test tests/AlpacaAgent.UnitTests/AlpacaAgent.UnitTests.csproj --configuration Release --no-build --filter "GateStatus=ActiveThroughPhase3"
dotnet test tests/AlpacaAgent.ContractTests/AlpacaAgent.ContractTests.csproj --configuration Release --no-build
dotnet test tests/AlpacaAgent.PersistenceTests/AlpacaAgent.PersistenceTests.csproj --configuration Release --no-build
dotnet test tests/AlpacaAgent.FailureInjectionTests/AlpacaAgent.FailureInjectionTests.csproj --configuration Release --no-build
```

Also run the complete 180-ID unit project without a filter and feed its result file to a baseline-audit utility. At this stage the command is expected to return nonzero because future-phase acceptance tests remain intentionally red. The audit passes only when every Phase 1–3 active ID is green, every future ID fails at its approved missing-behavior assertion, and no ID is missing, skipped, unexpectedly green, or failing in setup. Do not report the unfiltered `dotnet test` command itself as passing.

Run deterministic active suites twice with randomized test order. Record replayable property-test seeds on failure.

### 15.2 Compliance matrix

Create a matrix mapping every Phase 1–3 requirement to:

- governing plan section;
- production type or migration;
- stable unit-test IDs;
- contract or PostgreSQL test IDs;
- fixture IDs;
- last passing commit;
- evidence location;
- status and any accepted limitation.

No requirement may be marked complete using only a source-code reference. Each needs executable evidence.

### 15.3 Required final demonstrations

1. Every workflow template persists all 18 nodes with exact `SKIPPED` reasons.
2. A cycle restarts before, during, and after each applicable node.
3. Duplicate scheduler ticks create one cycle.
4. Concurrent workers produce one current lease owner.
5. A stale worker cannot finalize or emit outbox work.
6. State and outbox records commit or roll back together.
7. Expired leases recover with bounded attempts.
8. Cancellation and deadline loss cannot advance a cycle.
9. Controls block scheduling and side-effect creation at the required boundaries.
10. All append-only evidence remains intact across retries and restart.

### 15.4 Final exit criteria

Phases 1–3 are compliant only when:

- the repaired Phase 2A gate is valid and reproducible;
- Phase 1 and Phase 2 requirements have executable tests and green results;
- all Phase 3 unit, PostgreSQL, restart, concurrency, and failure-injection tests pass;
- all previously green unit tests remain green;
- no required test is skipped or quarantined;
- no real broker-write path is enabled;
- the compliance matrix has no unexplained gaps;
- `AGENTS.md` and `README.md` describe the verified state rather than planned or prototype behavior.

## 16. Recommended pull-request and commit slices

Keep each slice independently reviewable and preserve test-first evidence.

1. **PR 1 — Repair acceptance metadata**

   Typed manifest, JSON Schema validation, deterministic generator, fixture-reference validation, and no behavioral production changes.
2. **PR 2 — Replace placeholder acceptance tests**

   Real fixtures, matrices, explicit test bodies, and recorded red baseline.
3. **PR 3 — Implement Phase 1 state machines**

   Portfolio, order, and lifecycle reducers plus `UT-020`–`UT-032`.
4. **PR 4 — Implement policies and contract values**

   Outer limits, inner profiles, optional value states, canonical identity, and `UT-001`–`UT-012`, `UT-130`, and `UT-131`.
5. **PR 5 — Complete Section 7 contracts and schemas**

   Versioned records, source-generated serializers, strict schemas, fixtures, ports, and dependency checks.
6. **PR 6 — Add PostgreSQL migrations and constraints**

   Schema, mappings, Testcontainers, and constraint/append-only tests.
7. **PR 7 — Add durable scheduling and claiming**

   All-node creation, unique schedules, dependency checks, leases, fencing, and attempts.
8. **PR 8 — Add guarded finalization and restart**

   Atomic outputs/events/outbox, retries, deadlines, cancellation, and restart matrix.
9. **PR 9 — Add recovery, controls, outbox, and health**

   Expired-lease recovery, safe dispatch, pauses, kill switch, and health observations.
10. **PR 10 — Final compliance evidence**

    Clean-checkout results, stamped passing commits, compliance matrix, and status-document updates.

Each PR must contain its own timestamped changelog under `changes/`, list every test actually run, and state all tests not run.

## 17. Risks and mitigations

| Risk | Consequence | Mitigation |
| --- | --- | --- |
| Placeholder replacement becomes a large mechanical exercise | Tests remain shallow despite looking complete | Review by behavioral domain; prohibit universal routers; require fixture and public-boundary mapping. |
| Existing green workflow tests bias implementation | Prototype behavior is preserved despite violating durability | Strengthen tests first and accept red regressions when they reveal missing guarantees. |
| Domain records become persistence-shaped | PostgreSQL concerns leak across boundaries | Keep separate Persistence mappings and test assembly dependencies. |
| Lease logic is tested only serially | Duplicate ownership appears in production | Use concurrent real-PostgreSQL tests with controlled barriers. |
| Database transactions surround handlers | Locks, deadlocks, and external-call coupling | Enforce short claim/finalization transactions and test transaction visibility. |
| Append-only evidence is accidentally updated | Audit and replay become unreliable | Insert-only repositories plus database enforcement and mutation tests. |
| `lastPassingCommit` becomes stale or fictional | Traceability cannot be trusted | Use the two-commit verification/stamping workflow. |
| Default API/Worker templates become accidental product behavior | Misleading health or runtime demonstrations | Remove templates and add only specified composition and endpoints. |
| Phase 3 accidentally enables broker writes | Unsafe action before Phase 10 proofs | Recorder-only write port, configuration hard stop, and zero-write tests. |

## 18. Definition of ready for Phase 4

Phase 4 may begin only when the Phase 1–3 final exit criteria are met and the evidence shows:

- a valid real acceptance baseline;
- complete versioned application-owned contracts;
- explicit state machines and policies;
- a clean PostgreSQL migration path;
- durable scheduling, claiming, finalization, retry, recovery, outbox, and controls;
- restart safety at every applicable node;
- no authoritative in-memory workflow state;
- no network-capable broker-write path.

At that point Phase 4 can add broker-read and market-data adapters without redesigning the workflow authority, contracts, or safety boundaries.
