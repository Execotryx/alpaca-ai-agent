param(
    [Parameter(Mandatory)][string]$ResultsPath,
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$manifest = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'tests/AlpacaAgent.UnitTests/acceptance-manifest.json') -Raw | ConvertFrom-Json
$byMethod = @{}; foreach ($entry in $manifest) { $byMethod[$entry.method] = $entry }
[xml]$trx = Get-Content -LiteralPath $ResultsPath -Raw
$manager = [Xml.XmlNamespaceManager]::new($trx.NameTable)
$manager.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$results = @($trx.SelectNodes('//t:UnitTestResult', $manager) | ForEach-Object {
    $method = ($_.testName -split '\.')[-1]
    if ($byMethod.ContainsKey($method)) { [pscustomobject]@{ Node=$_; Method=$method; outcome=$_.outcome; Output=$_.Output } }
})
if ($results.Count -ne 180) { throw "Expected 180 acceptance results, found $($results.Count)." }

$errors = [Collections.Generic.List[string]]::new()
foreach ($result in $results) {
    $entry = $byMethod[$result.Method]
    if ($result.outcome -in @('NotExecuted','Skipped')) { $errors.Add("$($entry.stableId) was skipped or not executed."); continue }
    if ($entry.implementationStatus -eq 'IMPLEMENTED') {
        if ($result.outcome -ne 'Passed') { $errors.Add("$($entry.stableId) expected active pass, observed $($result.outcome).") }
        continue
    }
    if ($result.outcome -ne 'Failed') { $errors.Add("$($entry.stableId) expected approved red failure, observed $($result.outcome).") ; continue }
    $message = $result.Output.ErrorInfo.Message
    if ($message -notmatch 'NOT_IMPLEMENTED' -or $message -notmatch [regex]::Escape($entry.stableId)) {
        $errors.Add("$($entry.stableId) failed outside its approved missing-behavior assertion.")
    }
}
if ($errors.Count) { throw ($errors -join [Environment]::NewLine) }

[pscustomobject]@{
    total = $results.Count
    activePassed = @($results | Where-Object { $byMethod[$_.Method].implementationStatus -eq 'IMPLEMENTED' -and $_.outcome -eq 'Passed' }).Count
    expectedRed = @($results | Where-Object { $byMethod[$_.Method].implementationStatus -eq 'NOT_IMPLEMENTED' -and $_.outcome -eq 'Failed' }).Count
    skipped = 0
    unexpectedGreen = 0
    unexpectedFailure = 0
} | ConvertTo-Json
