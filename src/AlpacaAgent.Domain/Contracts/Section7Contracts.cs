using System.Text.Json.Serialization;

namespace AlpacaAgent.Domain.Contracts;

public readonly record struct ContractIdentifier(string Value);
public readonly record struct Symbol(string Value)
{
    public static Symbol Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 16 || normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-'))
            throw new ArgumentException("Symbol contains unsupported characters.", nameof(value));
        return new(normalized);
    }
}
public readonly record struct VersionId(string Value);
public readonly record struct ContentHash(string Value);
public readonly record struct ProvenanceReference(string Value);
public readonly record struct Money(decimal Amount, string Currency);
public readonly record struct Ratio(decimal Value);
public readonly record struct WholeQuantity(long Value);

public sealed record IndependentVersionsV1(
    VersionId WorkflowDefinition,
    VersionId ContractSchema,
    VersionId Policy,
    VersionId InnerProfile,
    VersionId FeatureCalculation,
    VersionId Prompt,
    VersionId RequestedModel,
    VersionId ResolvedModel,
    VersionId ReasoningConfiguration,
    VersionId RuntimeConnectorProviderSdk,
    VersionId DeterministicDecisionLogic,
    VersionId PersistenceMapping);

public enum ValidationState { Valid, Unknown, Unavailable, Invalid }
public enum AssessmentPolicy { Skip, ConservativeCall, BalancedCall, IncomeCall, InsufficientInformation }
public enum AgentFailureKind { Timeout, ServiceUnavailable, RateLimited, Refused, IncompleteOrTruncated, OutputSchemaInvalid, PolicyValueInvalid, ExecutableFieldsPresent, GroundingRejected, SourceScopeViolation, LowConfidenceRejected, Cancelled }
public enum Authoritativeness { Authoritative, Diagnostic }
public enum AttemptOutcome { Completed, RetryableFailure, TerminalFailure, SafelyDeferred, Cancelled, LostLease }

public sealed record WorkflowCycleV1(
    string SchemaVersion, CycleId CycleId, CycleType CycleType, string ScheduleKey,
    DateTimeOffset ScheduledAt, DateTimeOffset StartedAt, StepId CurrentStep, StepStatus Status,
    WorkflowBudget Budget, Guid? ParentCycleId, string? ReplayReference, IndependentVersionsV1 Versions,
    string? DurableCheckpointReference, string? TerminalOutcome, string? TerminalReason);

public sealed record MarketFieldGroupV1(
    string Name, ValidationState State, DateTimeOffset? SourceTime, DateTimeOffset RetrievalTime,
    ProvenanceReference Provenance, string? ValueReference, string? FailureReason);
public sealed record OptionContractSnapshotV1(
    ContractIdentifier ContractId, Symbol Underlying, DateOnly Expiration, decimal Strike, string Right,
    Money? Bid, Money? Ask, long? Volume, long? OpenInterest, Ratio? Delta,
    int ContractMultiplier, string Deliverable, bool Adjusted, bool Active, bool Tradable,
    ContractIdentifier? SuccessorContractId);
public sealed record MarketSnapshotV1(
    string SchemaVersion, SnapshotId ObservationId, Symbol Symbol, DateTimeOffset SourceTime,
    DateTimeOffset RetrievalTime, string MarketSessionState, IReadOnlyList<MarketFieldGroupV1> FieldGroups,
    IReadOnlyList<OptionContractSnapshotV1> OptionContracts, DateOnly? ExDividendDate,
    ProvenanceReference? CorporateActionProvenance, ValidationState ValidationState);

public sealed record AccountPositionV1(string AssetId, Symbol Symbol, decimal Quantity, string AssetClass);
public sealed record AccountOrderEffectV1(string OrderReference, Symbol Symbol, decimal ReservedQuantity, string State);
public sealed record AccountActivityV1(string ActivityId, string Kind, DateTimeOffset OccurredAt, string Reference);
public sealed record AccountSnapshotV1(
    string SchemaVersion, SnapshotId ObservationId, string AccountId, DateTimeOffset ObservedAt,
    Money Cash, Money BuyingPower, Money OptionsBuyingPower, Money Equity, string AccountStatus,
    bool TradingBlocked, int OptionPermissionLevel, VersionId ConfigurationVersion,
    IReadOnlyList<AccountPositionV1> Positions, IReadOnlyList<AccountOrderEffectV1> OpenOrders,
    IReadOnlyList<string> ExistingOptionObligations, IReadOnlyList<AccountActivityV1> RecentActivities);

public sealed record ReconciledHoldingV1(
    string HoldingId, Symbol Symbol, decimal VerifiedQuantity, decimal FreeQuantity,
    decimal ReservedQuantity, IReadOnlyList<string> PendingEffects, IReadOnlyList<string> ObligationReferences);
public sealed record ReconciledPortfolioV1(
    string SchemaVersion, SnapshotId SnapshotId, DateTimeOffset ReconciledAt,
    IReadOnlyList<ReconciledHoldingV1> Holdings, IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> Inconsistencies, bool SafeHalt);

public sealed record EvidenceRecordV1(
    EvidenceId EvidenceId, string SourceIdentity, bool SourceAllowlisted, string TrustLabel,
    ValidationState IntegrityState, DateTimeOffset? PublicationTime, DateTimeOffset? EventTime,
    DateTimeOffset RetrievalTime, string ContentReference, bool SymbolRelevant, bool TimeRelevant,
    bool Duplicate, bool Fresh, bool Available, string NeutralClaim);
public sealed record EvidenceBundleV1(
    string SchemaVersion, string BundleId, Symbol EvaluatedSymbol, DateTimeOffset DecisionTime,
    ContentHash ContentHash, IReadOnlyList<EvidenceRecordV1> Evidence, ValidationState ValidationState);

public sealed record StructuredFeatureV1(string Name, ObservedValue<decimal> Value, VersionId CalculationVersion, ProvenanceReference Provenance);
public sealed record PolicyDefinitionV1(AssessmentPolicy Policy, string Description, string ProfileReference);
public sealed record PolicyInputPackageV1(
    string SchemaVersion, Symbol Symbol, DateTimeOffset DecisionTime, ContentHash ContentHash,
    SnapshotId MarketSnapshotId, SnapshotId AccountSnapshotId, SnapshotId ReconciledPortfolioId,
    string EvidenceBundleId, IReadOnlyList<StructuredFeatureV1> Features,
    IReadOnlyList<string> ReadOnlyPortfolioSummary, IReadOnlyList<PolicyDefinitionV1> AllowedPolicies,
    IndependentVersionsV1 Versions);

public sealed record AssessmentFactorV1(string Text, IReadOnlyList<EvidenceId> EvidenceIds);
public sealed record PolicyAssessmentV1Contract(
    string SchemaVersion, ContentHash InputContentHash,
    IReadOnlyList<AssessmentFactorV1> SupportingFactors, IReadOnlyList<AssessmentFactorV1> OpposingFactors,
    IReadOnlyList<AssessmentFactorV1> Conflicts, IReadOnlyList<string> MissingOrUnreliableInformation,
    AssessmentPolicy Policy, decimal DeclaredConfidence, string UncertaintyCategory);
public sealed record AcceptedAssessmentEnvelopeV1(
    string SchemaVersion, string AssessmentId, AttemptId SourceAttemptId,
    ContentHash InputContentHash, IReadOnlyList<string> InputReferences,
    PolicyAssessmentV1Contract Assessment, bool Accepted, IReadOnlyList<string> ValidationResults);

public sealed record AgentCapabilityConfigurationV1(int ToolCount, bool HasSession, bool HasThread, bool HasMemory, bool HasHandoffs, bool HasPreviousResponseChaining);
public sealed record AgentAttemptStartV1(
    string SchemaVersion, AttemptId AttemptId, ContentHash InputHash, IReadOnlyList<string> InputReferences,
    string RequestedProvider, string RequestedModel, string ReasoningSetting, IndependentVersionsV1 Versions,
    DateTimeOffset StartedAt, DateTimeOffset TimeoutAt, AgentCapabilityConfigurationV1 Capabilities);
public sealed record AgentUsageV1(long InputTokens, long OutputTokens, long? ReasoningTokens);
public sealed record AgentAttemptResultV1(
    string SchemaVersion, AttemptId AttemptId, DateTimeOffset ObservedAt, string ResolvedProvider,
    string ResolvedModel, string? ProviderRequestId, string FinishStatus, AgentUsageV1? Usage,
    long DurationMilliseconds, string? AcceptedAssessmentReference, string? RawOutputReference,
    IReadOnlyList<string> ValidationResults, AgentFailureKind? Failure, string RetryClassification,
    Authoritativeness Authoritativeness);

public sealed record StrategyPolicyV1(
    string SchemaVersion, VersionId PolicyVersion, IReadOnlyList<string> AllowedStructures,
    IReadOnlyList<Symbol> AllowedSymbols, int ContractMultiplier, bool StandardDeliverableOnly,
    int MinimumDte, int MaximumDte, decimal MinimumDelta, decimal MaximumDelta,
    decimal MaximumSpreadRatio, int MaximumContractsPerShareBlock, decimal MaximumPortfolioExposure,
    int RetryLimit, int MaximumActions, IReadOnlyList<string> EntryRules,
    IReadOnlyList<string> CloseRules, IReadOnlyList<string> ExpirationRules, IReadOnlyList<string> AssignmentRules);
public sealed record StrategyProfileV1(
    string SchemaVersion, VersionId ProfileVersion, string Name, int MinimumDte, int MaximumDte,
    decimal MinimumDelta, decimal MaximumDelta, decimal MaximumSpreadRatio,
    int MaximumContractsPerShareBlock, decimal MaximumPortfolioExposure, int RetryLimit,
    IReadOnlyList<string> RankingPreferences);

public sealed record CandidateV1(
    string SchemaVersion, string CandidateId, Symbol Underlying, ContractIdentifier ContractId,
    string StrategyType, decimal RequiredShareCoverage, string AllocationReference,
    Money Premium, Ratio SpreadRatio, Money BreakEven, string CappedUpsideContext,
    string AssignmentContext, string DownsideContext, IReadOnlyDictionary<string, ObservedValue<decimal>> Features,
    VersionId PolicyProfileVersion, IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> Warnings, IReadOnlyList<string> Rationale, IReadOnlyList<string> InvalidationConditions);
public sealed record RankedCandidateV1(
    string SchemaVersion, string CandidateId, decimal Score, VersionId ScoringPolicyVersion,
    IReadOnlyDictionary<string, decimal> FeatureContributions, IReadOnlyList<string> TieBreakEvidence, int Rank);
public sealed record ProposedActionSetV1(
    string SchemaVersion, string ActionSetId, ContentHash ContentHash, IReadOnlyList<string> RankedCandidateIds,
    IReadOnlyList<string> ReservationReferences, IReadOnlyDictionary<string, decimal> ProjectedExposure,
    IReadOnlyList<string> RejectedAlternatives, string? NoActionReason);
public sealed record RiskRuleResultV1(string RuleId, bool Passed, string EvidenceReference, string Threshold);
public sealed record PortfolioRiskDecisionV1(
    string SchemaVersion, string DecisionId, string ActionSetId, ContentHash ActionSetHash,
    IReadOnlyList<string> SnapshotAndPolicyReferences, IReadOnlyList<RiskRuleResultV1> RuleResults,
    string ProjectedStateBeforeReference, string ProjectedStateAfterReference, bool Approved,
    string? ApprovalToken, DateTimeOffset? ApprovalExpiresAt, IReadOnlyList<string> InvalidationConditions);

public sealed record OrderIntentV1(
    string SchemaVersion, IntentId IntentId, string IntentKey, string ClientOrderId,
    string ActionSetReference, string RiskDecisionReference, ContractIdentifier ContractId,
    string Side, int Quantity, string OrderType, Money LimitPrice, string TimeInForce,
    DateTimeOffset SubmissionDeadline, IReadOnlyList<string> CancellationRules,
    string ExpectedPositionEffect, string PreWriteCheckpointReference);
public sealed record DecisionRecordV1(
    string SchemaVersion, string DecisionId, CycleId CycleId, string CorrelationId,
    IReadOnlyList<string> InputReferences, IReadOnlyList<AttemptId> AgentAttempts,
    string? AcceptedAssessmentReference, IReadOnlyList<string> CandidateReferences,
    string? RiskDecisionReference, IReadOnlyList<IntentId> IntentIds,
    IReadOnlyList<string> BrokerAndLifecycleEventReferences, IReadOnlyList<string> HumanInterventions,
    string Outcome, string? UnresolvedReason, IndependentVersionsV1 Versions);

public sealed record WorkflowStepAttemptStartV1(
    string SchemaVersion, AttemptId AttemptId, CycleId CycleId, StepId StepId, int AttemptNumber,
    string WorkerId, string LeaseOwner, long FencingToken, DateTimeOffset ScheduledAt,
    DateTimeOffset DatabaseClaimTime, DateTimeOffset StartedAt, DateTimeOffset LeaseExpiresAt,
    IReadOnlyList<string> InputReferences, IndependentVersionsV1 Versions);
public sealed record WorkflowStepAttemptResultV1(
    string SchemaVersion, string ResultId, AttemptId AttemptId, DateTimeOffset ObservedAt,
    Authoritativeness Authoritativeness, AttemptOutcome Outcome, string Classification,
    string? DiagnosticReference, DateTimeOffset? NextDueAt,
    IReadOnlyList<string> OutputReferences, IReadOnlyList<string> CheckpointReferences);
public sealed record WorkflowCheckpointV1(
    string SchemaVersion, string CheckpointId, CycleId CycleId, StepId StepId,
    string Boundary, ContentHash ContentHash, VersionId ContractVersion,
    string PayloadReference, DateTimeOffset CreatedAt);
public sealed record OutboxMessageV1(
    string SchemaVersion, string OutboxId, string DeduplicationKey, string MessageType,
    VersionId MessageVersion, CycleId CycleId, IntentId? IntentId, string PayloadReference,
    string Status, DateTimeOffset DueAt, int AttemptCount, int MaximumAttempts,
    string? LeaseOwner, long FencingToken, DateTimeOffset? LeaseExpiresAt,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
public sealed record ShareReservationV1(
    string SchemaVersion, string ReservationId, CycleId CycleId, IntentId IntentId,
    string HoldingId, int ShareBlock, decimal Quantity, string LifecycleState,
    DateTimeOffset CreatedAt, string ReconciliationReference);
public sealed record BrokerRequestAttemptV1(
    string SchemaVersion, string OperationId, string AttemptId, IntentId IntentId,
    string ClientOrderId, string OperationType, ContentHash RequestHash,
    string RequestReference, DateTimeOffset StartedAt, DateTimeOffset TimeoutAt,
    string? OutboxId, long FencingToken);
public sealed record BrokerObservationV1(
    string SchemaVersion, string ObservationId, string OperationId, IntentId IntentId,
    DateTimeOffset ObservedAt, string SourceChannel, string ResponseReference,
    string? BrokerOrderId, string? ActivityId);
public sealed record BrokerReconciliationResultV1(
    string SchemaVersion, IntentId IntentId, OrderState State, string EvidenceReference,
    string NextPermittedAction, bool BlindRetryPrevented, DateTimeOffset ReconciledAt);
public sealed record DomainEventV1(
    string SchemaVersion, string EventId, string CorrelationId, CycleId CycleId,
    string EventType, VersionId ContractVersion, string PayloadReference, DateTimeOffset OccurredAt);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(WorkflowCycleV1))]
[JsonSerializable(typeof(MarketSnapshotV1))]
[JsonSerializable(typeof(AccountSnapshotV1))]
[JsonSerializable(typeof(ReconciledPortfolioV1))]
[JsonSerializable(typeof(EvidenceBundleV1))]
[JsonSerializable(typeof(PolicyInputPackageV1))]
[JsonSerializable(typeof(PolicyAssessmentV1Contract))]
[JsonSerializable(typeof(AcceptedAssessmentEnvelopeV1))]
[JsonSerializable(typeof(AgentAttemptStartV1))]
[JsonSerializable(typeof(AgentAttemptResultV1))]
[JsonSerializable(typeof(StrategyPolicyV1))]
[JsonSerializable(typeof(StrategyProfileV1))]
[JsonSerializable(typeof(CandidateV1))]
[JsonSerializable(typeof(RankedCandidateV1))]
[JsonSerializable(typeof(ProposedActionSetV1))]
[JsonSerializable(typeof(PortfolioRiskDecisionV1))]
[JsonSerializable(typeof(OrderIntentV1))]
[JsonSerializable(typeof(DecisionRecordV1))]
[JsonSerializable(typeof(WorkflowStepAttemptStartV1))]
[JsonSerializable(typeof(WorkflowStepAttemptResultV1))]
[JsonSerializable(typeof(WorkflowCheckpointV1))]
[JsonSerializable(typeof(OutboxMessageV1))]
[JsonSerializable(typeof(ShareReservationV1))]
[JsonSerializable(typeof(BrokerRequestAttemptV1))]
[JsonSerializable(typeof(BrokerObservationV1))]
[JsonSerializable(typeof(BrokerReconciliationResultV1))]
[JsonSerializable(typeof(DomainEventV1))]
public partial class Section7JsonContext : JsonSerializerContext;
