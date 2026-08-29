param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

function Write-Utf8Lf([string]$Path, [string]$Content) {
    $normalized = ($Content -replace "`r`n", "`n").TrimEnd("`r", "`n") + "`n"
    [System.IO.File]::WriteAllText($Path, $normalized, [System.Text.UTF8Encoding]::new($false))
}

$planPath = Join-Path $RepositoryRoot 'Alpaca_AI_Agent_Framework_Neutral_Implementation_Plan.md'
$matches = Select-String -LiteralPath $planPath -Pattern '^\| (UT-\d{3}) \| `([^`]+)` \| (.+) \|$'
if ($matches.Count -ne 180) { throw "Expected 180 acceptance rows, found $($matches.Count)." }

$entries = foreach ($match in $matches) {
    $groups = $match.Matches[0].Groups
    [pscustomobject]@{ id = $groups[1].Value; method = $groups[2].Value; assertion = $groups[3].Value }
}
if (($entries.id | Sort-Object -Unique).Count -ne 180) { throw 'Stable acceptance IDs must be unique.' }

$phase = {
    param($number)
    switch ($number) {
        { $_ -lt 20 } { 'Phase 2'; break }
        { $_ -lt 40 } { 'Phase 1'; break }
        { $_ -lt 60 } { 'Phase 3'; break }
        { $_ -lt 80 } { 'Phase 4'; break }
        { $_ -lt 100 } { 'Phase 5'; break }
        { $_ -lt 130 } { 'Phase 6'; break }
        { $_ -lt 150 } { 'Phase 7-8'; break }
        { $_ -lt 170 } { 'Phase 9'; break }
        { $_ -lt 200 } { 'Phase 10'; break }
        { $_ -lt 220 } { 'Phase 11'; break }
        default { 'Phase 12-14' }
    }
}

$classFor = {
    param($number)
    switch ($number) {
        { $_ -lt 20 } { 'ContractAcceptanceTests'; break }
        { $_ -lt 40 } { 'StateMachineAcceptanceTests'; break }
        { $_ -lt 60 } { 'WorkflowAcceptanceTests'; break }
        { $_ -lt 80 } { 'BrokerAndMarketAcceptanceTests'; break }
        { $_ -lt 100 } { 'FeatureAndEvidenceAcceptanceTests'; break }
        { $_ -lt 130 } { 'PolicyAcceptanceTests'; break }
        { $_ -lt 150 } { 'CandidateAcceptanceTests'; break }
        { $_ -lt 170 } { 'RiskAcceptanceTests'; break }
        { $_ -lt 200 } { 'ExecutionAcceptanceTests'; break }
        { $_ -lt 220 } { 'LifecycleAcceptanceTests'; break }
        default { 'CrossCuttingAcceptanceTests' }
    }
}

$testRoot = Join-Path $RepositoryRoot 'tests/AlpacaAgent.UnitTests/Acceptance'
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
$behavioralIds = @(
    1..12
    20..32
    40..55
    130..131
) | ForEach-Object { 'UT-{0:000}' -f $_ }
$groups = $entries | Where-Object id -NotIn $behavioralIds | Group-Object { & $classFor ([int]$_.id.Substring(3)) }
$applicationAcceptanceRoot = Join-Path $RepositoryRoot 'src/AlpacaAgent.Application/Acceptance'
New-Item -ItemType Directory -Force -Path $applicationAcceptanceRoot | Out-Null
$boundaryBody = @('using AlpacaAgent.Domain.Acceptance;', '', 'namespace AlpacaAgent.Application.Acceptance;', '')
foreach ($group in $groups) {
    $generatedClass = "Generated$($group.Name)"
    $boundaryClass = "$($group.Name -replace 'AcceptanceTests$','')FutureBoundary"
    $body = @('using AlpacaAgent.Application.Acceptance;', 'using Xunit;', '', 'namespace AlpacaAgent.UnitTests.Acceptance;', '', "public sealed class $generatedClass : AcceptanceTestBase", '{', "    private readonly $boundaryClass boundary = new();", '')
    $boundaryBody += "public sealed class $boundaryClass"
    $boundaryBody += '{'
    foreach ($entry in $group.Group) {
        $assertion = $entry.assertion.Replace('"', '""')
        $body += '    [Fact]'
        $body += "    [Trait(`"StableId`", `"$($entry.id)`")]"
        $body += '    [Trait("GateStatus", "ExpectedRedFuture")]'
        $body += ('    public void {0}() => AssertExpectedRed("{1}", @"{2}", boundary.{0}());' -f $entry.method, $entry.id, $assertion)
        $body += ''
        $boundaryBody += ('    public AcceptanceOutcome {0}() =>' -f $entry.method)
        $boundaryBody += ('        AcceptanceOutcome.NotImplemented("{0}", @"{1}", @"{1}");' -f $entry.id, $assertion)
        $boundaryBody += ''
    }
    $body += '}'
    $boundaryBody += '}'
    $boundaryBody += ''
    Write-Utf8Lf (Join-Path $testRoot "$generatedClass.cs") ($body -join "`n")
}
Write-Utf8Lf (Join-Path $applicationAcceptanceRoot 'GeneratedFutureAcceptanceBoundaries.cs') ($boundaryBody -join "`n")

$manifest = foreach ($entry in $entries) {
    $number = [int]$entry.id.Substring(3)
    $fixtureIds = [System.Collections.ArrayList]::new()
    if ($entry.id -in (40..55 | ForEach-Object { 'UT-{0:000}' -f $_ })) {
        [void]$fixtureIds.Add('WorkflowAcceptanceTests.ScriptedExecutor')
    }
    [ordered]@{
        stableId = $entry.id
        method = $entry.method
        owningPhase = & $phase $number
        productionComponent = (& $classFor $number) -replace 'AcceptanceTests$',''
        fixtureIds = $fixtureIds
        classification = if ($entry.method -match 'Valid|Normal|Known|Golden|Allowed|Approved|Completed') { 'normal-flow' } else { 'edge-case' }
        expectedPublicOutcome = $entry.assertion
        safetyInvariant = $entry.assertion
        implementationStatus = if ($entry.id -in $behavioralIds) { 'IMPLEMENTED' } else { 'NOT_IMPLEMENTED' }
        lastPassingCommit = $null
    }
}
Write-Utf8Lf (Join-Path $RepositoryRoot 'tests/AlpacaAgent.UnitTests/acceptance-manifest.json') ($manifest | ConvertTo-Json -Depth 5)
