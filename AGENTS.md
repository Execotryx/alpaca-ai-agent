# AGENTS.md

## Scope

These instructions apply to the entire repository. Read `Alpaca_AI_Agent_Framework_Neutral_Implementation_Plan.md` before changing code, tests, schemas, or deployment assets. Implement only the phase or slice requested by the user; do not silently expand MVP scope.

## Source of truth

- Sections 4 through 8 of the implementation plan are the normative architecture.
- Section 10 is the required unit-test acceptance baseline.
- Sections 9, 11, 12, and 13 provide implementation order, verification traceability, dependencies, and completion gates; they do not override the normative architecture.
- If code, a test, or a later plan section conflicts with Sections 4 through 8, stop and propose a versioned plan correction before weakening a safety rule or test.

## Architecture constraints

- Target C# on .NET 10 and PostgreSQL in the modular-monolith project boundaries defined by the plan.
- PostgreSQL is authoritative for workflow progress, immutable audit evidence, intents, reservations, outbox work, broker observations, and lifecycle state.
- Use the declared cycle templates. Persist all 18 node identities per cycle, execute only applicable nodes, and mark every other node `SKIPPED` with a reason.
- Keep Domain and Application free of Agent Framework, provider SDK, Alpaca SDK, and persistence-specific types.
- AI is allowed only at node 6 behind `IPolicyAssessor`. It receives immutable bounded inputs, has zero tools, no sessions, memory, handoffs, MCP, or previous-response chaining, and never constructs an order or calls the broker.
- Broker truth, validation, calculations, candidate construction, ranking, selection, risk approval, sizing, execution, reconciliation, and lifecycle management remain deterministic.
- The MVP sends no native replace request and implements no rolling. Order changes use cancel, broker confirmation, fresh exact-set approval, and a new intent/client order identity.
- Never enable a real paper-broker write before the Phase 10 crash-window, uniqueness, fencing, checkpoint, reservation, and `SUBMITTED_UNKNOWN` integration gates pass.

## Test-first workflow

1. Identify the Section 10 stable test IDs owned by the requested slice.
2. Add or verify those tests and fixtures before production behavior.
3. Run the focused tests and preserve evidence that each new test fails at the intended assertion.
4. Implement the smallest behavior that satisfies the tests.
5. Run the focused tests, all previously green unit tests, and the phase-specific boundary suites.

Do not delete, skip, quarantine, loosen, or rewrite an acceptance test to match an implementation. A test-contract change requires a reviewed implementation-plan version and rationale. Tests must assert public Domain/Application behavior, not private methods, SQL text, SDK internals, or model prose.

## Validation commands

Once the solution exists, run from the repository root:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Use stable-ID or phase filters for the red/green loop, then run the full command above before declaring the slice complete. Run Testcontainers, contract, failure-injection, replay, or paper suites when the plan assigns them to the phase. If a command cannot run because the corresponding project has not been created yet, report that fact; do not invent a passing result.

## Change discipline

- Preserve unrelated user changes and avoid destructive Git operations.
- Never commit secrets, tokens, raw sensitive responses, local settings, or generated credentials.
- Keep executable order fields immutable after readiness. A changed executable field requires fresh approval and a new identity.
- Keep workflow, agent, and broker request-start records separate from immutable result/observation records; never update append-only evidence in place.
- Use UTC instants, invariant-culture parsing, stable ordering, deterministic IDs, and canonical serialization at identity boundaries.
- Prefer safe deferral or visible reconciliation to guessing, coercion, silent fallback, or blind retry.

## Changelog and handoff

Document every requested repository change in `changes/` using:

`changes-dd-MM-yyyy-HH-mm-ss(brief sentence about changes made).md`

Use hyphens in the time component. Colons and other Windows-invalid filename characters are forbidden.

Each changelog must state the scope, key decisions, files changed, and validation performed. In the final handoff, report the tests and checks actually run, any tests not run, and any remaining risk or deferred work. Use a concise commit message that describes the behavior or documentation change.

## Current implementation baseline

- The repository has the Phase 2/2A skeleton plus the first behavioral workflow-kernel slice (UT-040 through UT-055). Later Phase 3 PostgreSQL durability work remains incomplete.
- Regenerate the Section 10 catalogue after an approved plan change with `./tools/Generate-AcceptanceCatalog.ps1` and require exactly 180 unique stable IDs.
- `AcceptanceCatalog_SpecificationManifestAndDiscovery_AreComplete` must stay green. Stable-ID placeholders remain intentionally red with `NOT_IMPLEMENTED` until their owning production phases are implemented test-first; implemented IDs must be real public behavior tests and green.
- Do not make the catalogue green by changing `SpecificationAcceptanceSkeleton`, weakening `AssertImplemented`, or routing IDs through a generic fake result. Replace each generated contract assertion with its owning public Domain/Application behavior as that phase is implemented.
