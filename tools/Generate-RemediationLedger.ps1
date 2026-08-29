param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$manifestPath = Join-Path $RepositoryRoot 'tests/AlpacaAgent.UnitTests/acceptance-manifest.json'
$ledgerPath = Join-Path $RepositoryRoot 'tests/AlpacaAgent.UnitTests/acceptance-remediation-ledger.json'
$implementedWorktree = @(
    1..12
    20..32
    40..55
    130..131
) | ForEach-Object { 'UT-{0:000}' -f $_ }

$ledger = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json | ForEach-Object {
    $workflowPrototype = [int]$_.stableId.Substring(3) -in 40..55
    [ordered]@{
        stableId = $_.stableId
        testMethod = $_.method
        expectedAssertion = $_.expectedPublicOutcome
        productionTarget = $_.productionComponent
        fixtureIds = [System.Collections.ArrayList]::new()
        initialStatus = if ($workflowPrototype) { 'PARTIAL_REVIEW_REQUIRED' } else { 'NOT_IMPLEMENTED' }
        initialEvidenceQuality = if ($workflowPrototype) { 'partial' } else { 'placeholder' }
        remediationStatus = if ($_.stableId -in $implementedWorktree) { 'IMPLEMENTED_WORKTREE' } else { 'EXPECTED_RED' }
    }
}

$json = ($ledger | ConvertTo-Json -Depth 5) -replace "`r`n", "`n"
[System.IO.File]::WriteAllText($ledgerPath, $json.TrimEnd("`r", "`n") + "`n", [System.Text.UTF8Encoding]::new($false))
