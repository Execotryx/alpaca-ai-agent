param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

function Scalar([string]$Type) { [ordered]@{ type = $Type } }
function NullableScalar([string]$Type) { [ordered]@{ type = @($Type, 'null') } }
function ArrayOf($Items, [int]$MaxItems = 256) { [ordered]@{ type = 'array'; maxItems = $MaxItems; items = $Items } }
function StringValue([int]$MaxLength = 512) { [ordered]@{ type = 'string'; minLength = 1; maxLength = $MaxLength } }
function ValueObject([string]$Name = 'value') { [ordered]@{ type = 'object'; additionalProperties = $false; required = @($Name); properties = [ordered]@{ $Name = (StringValue 256) } } }
function StrictObject([System.Collections.IDictionary]$Properties) { [ordered]@{ type = 'object'; additionalProperties = $false; required = @($Properties.Keys); properties = $Properties } }
function Write-Utf8Lf([string]$Path, [string]$Content) { [IO.File]::WriteAllText($Path, (($Content -replace "`r`n", "`n").TrimEnd() + "`n"), [Text.UTF8Encoding]::new($false)) }

$string = StringValue
$stringArray = ArrayOf (StringValue)
$id = ValueObject
$instant = [ordered]@{ type = 'string'; format = 'date-time' }
$schemaVersion = [ordered]@{ const = '1.0' }
$hash = ValueObject
$cycleId = StrictObject ([ordered]@{ value = [ordered]@{ type = 'string'; format = 'uuid' } })
$stepId = StrictObject ([ordered]@{ value = [ordered]@{ type = 'integer'; minimum = 1; maximum = 18 } })
$attemptId = StrictObject ([ordered]@{ value = [ordered]@{ type = 'string'; format = 'uuid' } })
$intentId = StrictObject ([ordered]@{ value = [ordered]@{ type = 'string'; format = 'uuid' } })
$versions = StrictObject ([ordered]@{
    workflowDefinition=(ValueObject); contractSchema=(ValueObject); policy=(ValueObject); innerProfile=(ValueObject)
    featureCalculation=(ValueObject); prompt=(ValueObject); requestedModel=(ValueObject); resolvedModel=(ValueObject)
    reasoningConfiguration=(ValueObject); runtimeConnectorProviderSdk=(ValueObject)
    deterministicDecisionLogic=(ValueObject); persistenceMapping=(ValueObject)
})
$budget = StrictObject ([ordered]@{
    maximumDuration=(StringValue 64); retryLimit=[ordered]@{type='integer';minimum=0}; agentAttemptLimit=[ordered]@{type='integer';minimum=0}
    externalCallLimit=[ordered]@{type='integer';minimum=0}; actionLimit=[ordered]@{type='integer';minimum=0}; modelToolLimit=[ordered]@{const=0}
})
$capabilities = StrictObject ([ordered]@{ toolCount=[ordered]@{const=0}; hasSession=[ordered]@{const=$false}; hasThread=[ordered]@{const=$false}; hasMemory=[ordered]@{const=$false}; hasHandoffs=[ordered]@{const=$false}; hasPreviousResponseChaining=[ordered]@{const=$false} })
$usage = StrictObject ([ordered]@{ inputTokens=[ordered]@{type='integer';minimum=0}; outputTokens=[ordered]@{type='integer';minimum=0}; reasoningTokens=[ordered]@{type=@('integer','null');minimum=0} })
$ruleResult = StrictObject ([ordered]@{ ruleId=(StringValue 128); passed=(Scalar 'boolean'); evidenceReference=(StringValue 256); threshold=(StringValue 256) })

$factor = StrictObject ([ordered]@{ text = (StringValue 2048); evidenceIds = (ArrayOf (ValueObject) 64) })
$evidenceRecord = StrictObject ([ordered]@{
    evidenceId = (ValueObject); sourceIdentity = (StringValue 256); sourceAllowlisted = (Scalar 'boolean'); trustLabel = (StringValue 64)
    integrityState = [ordered]@{ enum = @('Valid','Unknown','Unavailable','Invalid') }; publicationTime = [ordered]@{ type = @('string','null'); format = 'date-time' }
    eventTime = [ordered]@{ type = @('string','null'); format = 'date-time' }; retrievalTime = $instant; contentReference = (StringValue 1024)
    symbolRelevant = (Scalar 'boolean'); timeRelevant = (Scalar 'boolean'); duplicate = (Scalar 'boolean'); fresh = (Scalar 'boolean'); available = (Scalar 'boolean'); neutralClaim = (StringValue 2048)
})
$feature = StrictObject ([ordered]@{
    name = (StringValue 128)
    value = [ordered]@{ type = 'object'; additionalProperties = $false; required = @('state'); properties = [ordered]@{ state = [ordered]@{ enum = @('known','unknown','unavailable','invalid') }; value = [ordered]@{ type = 'number' }; reason = (StringValue 256) } }
    calculationVersion = (ValueObject); provenance = (ValueObject)
})
$policyDefinition = StrictObject ([ordered]@{ policy = [ordered]@{ enum = @('Skip','ConservativeCall','BalancedCall','IncomeCall','InsufficientInformation') }; description = (StringValue 1024); profileReference = (StringValue 256) })

$specs = [ordered]@{}
$specs['evidence-bundle.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; bundleId=(StringValue 128); evaluatedSymbol=(ValueObject); decisionTime=$instant; contentHash=$hash; evidence=(ArrayOf $evidenceRecord 64); validationState=[ordered]@{ enum=@('Valid','Unknown','Unavailable','Invalid') } })
$specs['policy-input-package.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; symbol=(ValueObject); decisionTime=$instant; contentHash=$hash; marketSnapshotId=(ValueObject); accountSnapshotId=(ValueObject); reconciledPortfolioId=(ValueObject); evidenceBundleId=(StringValue 128); features=(ArrayOf $feature 256); readOnlyPortfolioSummary=(ArrayOf (StringValue 512) 64); allowedPolicies=(ArrayOf $policyDefinition 5); versions=$versions })
$specs['policy-assessment.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; inputContentHash=$hash; supportingFactors=(ArrayOf $factor 32); opposingFactors=(ArrayOf $factor 32); conflicts=(ArrayOf $factor 32); missingOrUnreliableInformation=(ArrayOf (StringValue 1024) 32); policy=[ordered]@{ enum=@('Skip','ConservativeCall','BalancedCall','IncomeCall','InsufficientInformation') }; declaredConfidence=[ordered]@{ type='number'; minimum=0; maximum=1 }; uncertaintyCategory=(StringValue 64) })
$specs['agent-attempt.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; attemptId=$attemptId; inputHash=$hash; inputReferences=$stringArray; requestedProvider=(StringValue 128); requestedModel=(StringValue 128); reasoningSetting=(StringValue 128); versions=$versions; startedAt=$instant; timeoutAt=$instant; capabilities=$capabilities })
$specs['agent-attempt-result.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; attemptId=$attemptId; observedAt=$instant; resolvedProvider=(StringValue 128); resolvedModel=(StringValue 128); providerRequestId=(NullableScalar 'string'); finishStatus=(StringValue 128); usage=[ordered]@{ anyOf=@($usage,[ordered]@{type='null'}) }; durationMilliseconds=[ordered]@{ type='integer'; minimum=0 }; acceptedAssessmentReference=(NullableScalar 'string'); rawOutputReference=(NullableScalar 'string'); validationResults=$stringArray; failure=[ordered]@{ type=@('string','null') }; retryClassification=(StringValue 128); authoritativeness=[ordered]@{ enum=@('Authoritative','Diagnostic') } })
$specs['workflow-cycle.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; cycleId=$cycleId; cycleType=(StringValue 64); scheduleKey=(StringValue 256); scheduledAt=$instant; startedAt=$instant; currentStep=$stepId; status=(StringValue 64); budget=$budget; parentCycleId=[ordered]@{ type=@('string','null'); format='uuid' }; replayReference=(NullableScalar 'string'); versions=$versions; durableCheckpointReference=(NullableScalar 'string'); terminalOutcome=(NullableScalar 'string'); terminalReason=(NullableScalar 'string') })
$specs['workflow-step-attempt.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; attemptId=$attemptId; cycleId=$cycleId; stepId=$stepId; attemptNumber=[ordered]@{type='integer';minimum=1}; workerId=(StringValue 128); leaseOwner=(StringValue 128); fencingToken=[ordered]@{type='integer';minimum=1}; scheduledAt=$instant; databaseClaimTime=$instant; startedAt=$instant; leaseExpiresAt=$instant; inputReferences=$stringArray; versions=$versions })
$specs['workflow-step-attempt-result.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; resultId=(StringValue 128); attemptId=$attemptId; observedAt=$instant; authoritativeness=[ordered]@{enum=@('Authoritative','Diagnostic')}; outcome=(StringValue 128); classification=(StringValue 128); diagnosticReference=(NullableScalar 'string'); nextDueAt=[ordered]@{type=@('string','null');format='date-time'}; outputReferences=$stringArray; checkpointReferences=$stringArray })
$specs['order-intent.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; intentId=$intentId; intentKey=(StringValue 256); clientOrderId=(StringValue 128); actionSetReference=(StringValue 256); riskDecisionReference=(StringValue 256); contractId=(ValueObject); side=[ordered]@{enum=@('Sell','Buy')}; quantity=[ordered]@{type='integer';minimum=1}; orderType=[ordered]@{enum=@('Limit')}; limitPrice=[ordered]@{type='object';additionalProperties=$false;required=@('amount','currency');properties=[ordered]@{amount=[ordered]@{type='number';exclusiveMinimum=0};currency=[ordered]@{const='USD'}}}; timeInForce=[ordered]@{enum=@('Day','Gtc')}; submissionDeadline=$instant; cancellationRules=$stringArray; expectedPositionEffect=(StringValue 256); preWriteCheckpointReference=(StringValue 256) })
$specs['portfolio-risk-decision.schema.json'] = StrictObject ([ordered]@{ schemaVersion=$schemaVersion; decisionId=(StringValue 128); actionSetId=(StringValue 128); actionSetHash=$hash; snapshotAndPolicyReferences=$stringArray; ruleResults=(ArrayOf $ruleResult 256); projectedStateBeforeReference=(StringValue 256); projectedStateAfterReference=(StringValue 256); approved=(Scalar 'boolean'); approvalToken=(NullableScalar 'string'); approvalExpiresAt=[ordered]@{type=@('string','null');format='date-time'}; invalidationConditions=$stringArray })

$schemaRoot = Join-Path $RepositoryRoot 'schemas/contracts/v1'
New-Item -ItemType Directory -Force -Path $schemaRoot | Out-Null
foreach ($item in $specs.GetEnumerator()) {
    $schema = [ordered]@{ '$schema'='https://json-schema.org/draft/2020-12/schema'; '$id'="https://alpaca-agent.local/schemas/contracts/v1/$($item.Key)"; title=$item.Key.Replace('.schema.json',''); type=$item.Value.type; additionalProperties=$item.Value.additionalProperties; required=$item.Value.required; properties=$item.Value.properties }
    Write-Utf8Lf (Join-Path $schemaRoot $item.Key) ($schema | ConvertTo-Json -Depth 30)
}
