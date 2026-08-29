param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$scenarios = @(
    @('FX-VALID-COVERED-CALL','valid-covered-call-evaluation','COMPLETED','normal-flow',@('one covered candidate','exact-set approval'),'UT-040'),
    @('FX-SKIP-NO-ACTION','skip-no-action','NO_ACTION','valid-assessment',@('no candidate','no intent','no outbox'),'UT-042'),
    @('FX-INSUFFICIENT-INFORMATION','grounded-insufficient-information','SAFELY_DEFERRED','valid-abstention',@('missing evidence retained','no intent'),'UT-043'),
    @('FX-MODEL-TIMEOUT','model-timeout','SAFELY_DEFERRED','operational-failure',@('not abstention','attempt preserved'),'UT-044'),
    @('FX-MODEL-REFUSAL','model-refusal','SAFELY_DEFERRED','operational-failure',@('not abstention','attempt preserved'),'UT-045'),
    @('FX-MODEL-RATE-LIMIT','model-rate-limit','SAFELY_DEFERRED','operational-failure',@('bounded retry','no intent'),'UT-045'),
    @('FX-MODEL-TRUNCATION','model-truncation','SAFELY_DEFERRED','operational-failure',@('schema not repaired','no intent'),'UT-046'),
    @('FX-CANDIDATE-REJECTION','deterministic-candidate-rejection','NO_ACTION','deterministic-rejection',@('ordered reasons','no intent'),'UT-046'),
    @('FX-PORTFOLIO-RISK-REJECTION','portfolio-risk-rejection','NO_ACTION','risk-rejection',@('exact rule evidence','no intent'),'UT-153'),
    @('FX-PARTIAL-FILL-CANCEL-RACE','partial-fill-and-cancel-fill-race','RECONCILIATION_REQUIRED','broker-race',@('append observations','remaining quantity retained'),'UT-186'),
    @('FX-CRASH-RESTART-NODES','crash-and-restart-each-node','RESUMED','failure-injection',@('durable checkpoint','no duplicate effect'),'P3-DB-014'),
    @('FX-EXPIRY-PENDING-CONFIRMATION','expiration-pending-broker-confirmation','WAITING','lifecycle',@('coverage retained','quote is not truth'),'UT-028'),
    @('FX-EARLY-ASSIGNMENT','early-assignment','ASSIGNED_PENDING_RECONCILIATION','lifecycle',@('accepted before expiry','broker reconciliation required'),'UT-027'),
    @('FX-ADJUSTED-CONTRACT','split-or-adjusted-contract','REJECTED','contract-safety',@('not standard deliverable','no candidate'),'UT-134'),
    @('FX-DUPLICATE-OUT-OF-ORDER','duplicate-and-out-of-order-observations','UNCHANGED','broker-ordering',@('duplicate idempotent','terminal truth not regressed'),'UT-024'),
    @('FX-STALE-INCOMPLETE-DOWNGRADE','stale-quote-incomplete-pagination-feed-downgrade','SAFELY_DEFERRED','data-quality',@('no partial chain accepted','feed provenance retained'),'UT-069'),
    @('FX-PROMPT-INJECTION-CONFLICT','prompt-injection-and-conflicting-sources','INSUFFICIENT_INFORMATION','grounding-safety',@('evidence cannot instruct','conflicts cited'),'UT-107')
)

$entries = foreach ($scenario in $scenarios) {
    $payload = [ordered]@{ expectedOutcome=$scenario[2]; classification=$scenario[3]; assertions=[string[]]$scenario[4] }
    $payloadJson = $payload | ConvertTo-Json -Compress -Depth 10
    $hashBytes = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($payloadJson))
    [ordered]@{
        fixtureId=$scenario[0]; schemaVersion='1.0'; scenario=$scenario[1]
        decisionTime='2026-08-29T12:00:00Z'; sourceTimes=[string[]]@('2026-08-29T11:55:00Z')
        canonicalOrderingRule='ordinal-property-order;preserve-array-order;invariant-numbers;utc-instants'
        canonicalContentHash=([Convert]::ToHexString($hashBytes).ToLowerInvariant())
        provenance='review-owned-remediation-fixture'; expectedValidationState='VALID'; owningTests=[string[]]@($scenario[5])
        immutable=$true; payload=$payload
    }
}
$json = ($entries | ConvertTo-Json -Depth 10) -replace "`r`n", "`n"
$path = Join-Path $RepositoryRoot 'fixtures/fixture-catalog.json'
[IO.File]::WriteAllText($path, $json.TrimEnd() + "`n", [Text.UTF8Encoding]::new($false))
