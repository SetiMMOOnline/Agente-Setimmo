using System.Text.Json.Serialization;

namespace RagnaForge.Agent.Core.Api;

public sealed class AgentApiContract
{
    [JsonPropertyName("capabilities")] public AgentCapabilityContract Capabilities { get; set; } = new();
    [JsonPropertyName("supportedCommands")] public List<string> SupportedCommands { get; set; } = [];
    [JsonPropertyName("supportedEntityTypes")] public List<string> SupportedEntityTypes { get; set; } = [];
    [JsonPropertyName("supportedKnowledgeFeatures")] public List<string> SupportedKnowledgeFeatures { get; set; } = [];
    [JsonPropertyName("supportedPlanFeatures")] public List<string> SupportedPlanFeatures { get; set; } = [];
    [JsonPropertyName("supportedReportFeatures")] public List<string> SupportedReportFeatures { get; set; } = [];
    [JsonPropertyName("supportedLearningFeatures")] public List<string> SupportedLearningFeatures { get; set; } = [];
    [JsonPropertyName("supportedRefreshFeatures")] public List<string> SupportedRefreshFeatures { get; set; } = [];
    [JsonPropertyName("supportedOnlineSources")] public List<string> SupportedOnlineSources { get; set; } = [];
    [JsonPropertyName("supportedInternalLibraries")] public List<string> SupportedInternalLibraries { get; set; } = [];
    [JsonPropertyName("supportedAuthorizedCodeSources")] public List<string> SupportedAuthorizedCodeSources { get; set; } = [];
    [JsonPropertyName("supportedMcpTools")] public List<string> SupportedMcpTools { get; set; } = [];
    [JsonPropertyName("jsonSchemas")] public Dictionary<string, object> JsonSchemas { get; set; } = [];
    [JsonPropertyName("safetyFlags")] public AgentValidationSummaryContract SafetyFlags { get; set; } = new();
    [JsonPropertyName("recommendedApiEndpoints")] public List<string> RecommendedApiEndpoints { get; set; } = [];
    [JsonPropertyName("recommendedUiTabs")] public List<string> RecommendedUiTabs { get; set; } = [];
    [JsonPropertyName("recommendedUiPanels")] public List<string> RecommendedUiPanels { get; set; } = [];
}

public sealed class AgentCapabilityContract
{
    [JsonPropertyName("supportsApply")] public bool SupportsApply { get; set; }
    [JsonPropertyName("supportsRollback")] public bool SupportsRollback { get; set; }
    [JsonPropertyName("supportsDryRun")] public bool SupportsDryRun { get; set; } = true;
    [JsonPropertyName("supportsProductionApply")] public bool SupportsProductionApply { get; set; }
    [JsonPropertyName("supportsCodexSupervised")] public bool SupportsCodexSupervised { get; set; } = true;
    [JsonPropertyName("supportsSemanticPatch")] public bool SupportsSemanticPatch { get; set; } = true;
    [JsonPropertyName("supportsContextPacks")] public bool SupportsContextPacks { get; set; } = true;
    [JsonPropertyName("supportsOperationHistory")] public bool SupportsOperationHistory { get; set; } = true;
    [JsonPropertyName("supportsGrfOperations")] public bool SupportsGrfOperations { get; set; } = true;
}

public sealed class AgentEntityLookupContract
{
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = string.Empty;
    [JsonPropertyName("query")] public object Query { get; set; } = new();
    [JsonPropertyName("localEntity")] public object? LocalEntity { get; set; }
    [JsonPropertyName("hints")] public List<AgentKnowledgeHintContract> Hints { get; set; } = [];
    [JsonPropertyName("conflicts")] public List<AgentConflictContract> Conflicts { get; set; } = [];
    [JsonPropertyName("coverage")] public AgentCoverageContract? Coverage { get; set; }
    [JsonPropertyName("risk")] public AgentRiskContract Risk { get; set; } = new();
    [JsonPropertyName("provenance")] public List<AgentProvenanceContract> Provenance { get; set; } = [];
    [JsonPropertyName("externalReference")] public AgentExternalReferenceContract ExternalReference { get; set; } = new();
    [JsonPropertyName("episodeGate")] public AgentEpisodeGateContract EpisodeGate { get; set; } = new();
    [JsonPropertyName("nextSafeActions")] public List<string> NextSafeActions { get; set; } = [];
    [JsonPropertyName("safeForReadOnlyWork")] public bool SafeForReadOnlyWork { get; set; } = true;
    [JsonPropertyName("safeForDryRun")] public bool SafeForDryRun { get; set; } = true;
    [JsonPropertyName("safeForApply")] public bool SafeForApply { get; set; }
    [JsonPropertyName("safeForProductionApply")] public bool SafeForProductionApply { get; set; }
}

public sealed class AgentEntityPlanContract
{
    [JsonPropertyName("requestedEntity")] public object RequestedEntity { get; set; } = new();
    [JsonPropertyName("normalizedEntity")] public object NormalizedEntity { get; set; } = new();
    [JsonPropertyName("requiredFields")] public List<string> RequiredFields { get; set; } = [];
    [JsonPropertyName("missingRequiredFields")] public List<string> MissingRequiredFields { get; set; } = [];
    [JsonPropertyName("idConflictCheck")] public object IdConflictCheck { get; set; } = new();
    [JsonPropertyName("idSuggestions")] public List<string> IdSuggestions { get; set; } = [];
    [JsonPropertyName("safeIdRanges")] public List<string> SafeIdRanges { get; set; } = [];
    [JsonPropertyName("localMatches")] public List<object> LocalMatches { get; set; } = [];
    [JsonPropertyName("knowledgeMatches")] public List<object> KnowledgeMatches { get; set; } = [];
    [JsonPropertyName("referenceContext")] public List<object> ReferenceContext { get; set; } = [];
    [JsonPropertyName("controlledLiveReference")] public AgentExternalReferenceContract ControlledLiveReference { get; set; } = new();
    [JsonPropertyName("assetHints")] public List<string> AssetHints { get; set; } = [];
    [JsonPropertyName("dependencyHints")] public List<AgentDependencyContract> DependencyHints { get; set; } = [];
    [JsonPropertyName("episodeGate")] public AgentEpisodeGateContract EpisodeGate { get; set; } = new();
    [JsonPropertyName("riskLevel")] public string RiskLevel { get; set; } = "low";
    [JsonPropertyName("conflicts")] public List<AgentConflictContract> Conflicts { get; set; } = [];
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
    [JsonPropertyName("nextSafeActions")] public List<string> NextSafeActions { get; set; } = [];
    [JsonPropertyName("dryRunPlan")] public object DryRunPlan { get; set; } = new();
    [JsonPropertyName("diffPreviewPlaceholder")] public string DiffPreviewPlaceholder { get; set; } = "generated-by-api-later";
    [JsonPropertyName("humanReviewRequired")] public bool HumanReviewRequired { get; set; } = true;
    [JsonPropertyName("canApply")] public bool CanApply { get; set; }
    [JsonPropertyName("safeForReadOnlyWork")] public bool SafeForReadOnlyWork { get; set; } = true;
    [JsonPropertyName("safeForDryRun")] public bool SafeForDryRun { get; set; } = true;
    [JsonPropertyName("safeForApply")] public bool SafeForApply { get; set; }
    [JsonPropertyName("safeForProductionApply")] public bool SafeForProductionApply { get; set; }
}

public sealed class AgentKnowledgeHintContract
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("severity")] public string Severity { get; set; } = "hint";
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = string.Empty;
    [JsonPropertyName("provenance")] public List<AgentProvenanceContract> Provenance { get; set; } = [];
    [JsonPropertyName("humanReviewRecommended")] public bool HumanReviewRecommended { get; set; }
    [JsonPropertyName("blocksReadOnly")] public bool BlocksReadOnly { get; set; }
    [JsonPropertyName("blocksDryRun")] public bool BlocksDryRun { get; set; }
    [JsonPropertyName("blocksApply")] public bool BlocksApply { get; set; }
    [JsonPropertyName("reasonNotBlocking")] public string ReasonNotBlocking { get; set; } = string.Empty;
}

public sealed class AgentConflictContract
{
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = string.Empty;
    [JsonPropertyName("entityId")] public string? EntityId { get; set; }
    [JsonPropertyName("severity")] public string Severity { get; set; } = "warning";
    [JsonPropertyName("riskLevel")] public string RiskLevel { get; set; } = "low";
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = string.Empty;
    [JsonPropertyName("humanReviewRecommended")] public bool HumanReviewRecommended { get; set; } = true;
    [JsonPropertyName("blocksReadOnly")] public bool BlocksReadOnly { get; set; }
    [JsonPropertyName("blocksDryRun")] public bool BlocksDryRun { get; set; }
    [JsonPropertyName("blocksApply")] public bool BlocksApply { get; set; }
    [JsonPropertyName("reasonNotBlocking")] public string ReasonNotBlocking { get; set; } = string.Empty;
    [JsonPropertyName("nextSafeAction")] public string NextSafeAction { get; set; } = string.Empty;
}

public sealed class AgentCoverageContract
{
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = string.Empty;
    [JsonPropertyName("localCount")] public int LocalCount { get; set; }
    [JsonPropertyName("internalReferenceCount")] public int InternalReferenceCount { get; set; }
    [JsonPropertyName("withHints")] public int WithHints { get; set; }
    [JsonPropertyName("conflicts")] public int Conflicts { get; set; }
    [JsonPropertyName("coverageRatio")] public double CoverageRatio { get; set; }
}

public sealed class AgentRiskContract
{
    [JsonPropertyName("level")] public string Level { get; set; } = "low";
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
}

public sealed class AgentProvenanceContract
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("sourceName")] public string SourceName { get; set; } = string.Empty;
    [JsonPropertyName("sourceKind")] public string SourceKind { get; set; } = string.Empty;
    [JsonPropertyName("origin")] public string Origin { get; set; } = string.Empty;
    [JsonPropertyName("externalReferenceUrl")] public string? ExternalReferenceUrl { get; set; }
    [JsonPropertyName("reviewedAt")] public string? ReviewedAt { get; set; }
    [JsonPropertyName("retrievedAt")] public string? RetrievedAt { get; set; }
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("priority")] public int Priority { get; set; }
    [JsonPropertyName("trustPolicy")] public string TrustPolicy { get; set; } = string.Empty;
    [JsonPropertyName("conflictPolicy")] public string ConflictPolicy { get; set; } = string.Empty;
    [JsonPropertyName("canBlock")] public bool CanBlock { get; set; }
    [JsonPropertyName("reasonNotBlocking")] public string ReasonNotBlocking { get; set; } = string.Empty;
}

public sealed class AgentExternalReferenceContract
{
    [JsonPropertyName("liveLookupDecision")] public string LiveLookupDecision { get; set; } = string.Empty;
    [JsonPropertyName("decisionReason")] public string DecisionReason { get; set; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; set; } = "none";
    [JsonPropertyName("requestCount")] public int RequestCount { get; set; }
    [JsonPropertyName("timeoutMs")] public int TimeoutMs { get; set; } = 3000;
    [JsonPropertyName("rateLimitApplied")] public bool RateLimitApplied { get; set; } = true;
    [JsonPropertyName("linksFollowed")] public bool LinksFollowed { get; set; }
    [JsonPropertyName("bulkLookup")] public bool BulkLookup { get; set; }
    [JsonPropertyName("rangeLookup")] public bool RangeLookup { get; set; }
    [JsonPropertyName("rawHtmlStored")] public bool RawHtmlStored { get; set; }
    [JsonPropertyName("dumpStored")] public bool DumpStored { get; set; }
    [JsonPropertyName("cacheMode")] public string CacheMode { get; set; } = "none";
    [JsonPropertyName("warning")] public string? Warning { get; set; }
    [JsonPropertyName("reasonNotBlocking")] public string ReasonNotBlocking { get; set; } = "External reference alone cannot block local workflow.";
}

public sealed class AgentEpisodeGateContract
{
    [JsonPropertyName("status")] public string Status { get; set; } = "unknown";
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
}

public sealed class AgentDependencyContract
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
}

public sealed class AgentReportContract
{
    [JsonPropertyName("reportType")] public string ReportType { get; set; } = string.Empty;
    [JsonPropertyName("format")] public string Format { get; set; } = "md";
    [JsonPropertyName("markdown")] public string? Markdown { get; set; }
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
    [JsonPropertyName("safeForReadOnlyWork")] public bool SafeForReadOnlyWork { get; set; } = true;
    [JsonPropertyName("safeForDryRun")] public bool SafeForDryRun { get; set; } = true;
    [JsonPropertyName("safeForApply")] public bool SafeForApply { get; set; }
    [JsonPropertyName("safeForProductionApply")] public bool SafeForProductionApply { get; set; }
}

public sealed class AgentValidationSummaryContract
{
    [JsonPropertyName("canApply")] public bool CanApply { get; set; }
    [JsonPropertyName("safeForReadOnlyWork")] public bool SafeForReadOnlyWork { get; set; } = true;
    [JsonPropertyName("safeForDryRun")] public bool SafeForDryRun { get; set; } = true;
    [JsonPropertyName("safeForApply")] public bool SafeForApply { get; set; }
    [JsonPropertyName("safeForProductionApply")] public bool SafeForProductionApply { get; set; }
    [JsonPropertyName("readOnlyMode")] public bool ReadOnlyMode { get; set; } = true;
    [JsonPropertyName("applyEnabled")] public bool ApplyEnabled { get; set; }
    [JsonPropertyName("rollbackEnabled")] public bool RollbackEnabled { get; set; }
}

public sealed class AgentKnowledgeSourceContract
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("sourceType")] public string SourceType { get; set; } = string.Empty;
    [JsonPropertyName("externalReferenceUrl")] public string? ExternalReferenceUrl { get; set; }
    [JsonPropertyName("supportedTopics")] public List<string> SupportedTopics { get; set; } = [];
    [JsonPropertyName("supportedEntityTypes")] public List<string> SupportedEntityTypes { get; set; } = [];
    [JsonPropertyName("updateMode")] public string UpdateMode { get; set; } = string.Empty;
    [JsonPropertyName("refreshPolicy")] public string RefreshPolicy { get; set; } = string.Empty;
    [JsonPropertyName("licenseNotes")] public List<AgentLicenseNoteContract> LicenseNotes { get; set; } = [];
    [JsonPropertyName("authorizedUse")] public AgentAuthorizedUseContract AuthorizedUse { get; set; } = new();
    [JsonPropertyName("canBlock")] public bool CanBlock { get; set; }
}

public sealed class AgentKnowledgeRefreshContract
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("mode")] public string Mode { get; set; } = "metadata";
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("requestCount")] public int RequestCount { get; set; }
    [JsonPropertyName("timeoutMs")] public int TimeoutMs { get; set; } = 3000;
    [JsonPropertyName("rateLimitApplied")] public bool RateLimitApplied { get; set; } = true;
    [JsonPropertyName("linksFollowed")] public bool LinksFollowed { get; set; }
    [JsonPropertyName("paginationUsed")] public bool PaginationUsed { get; set; }
    [JsonPropertyName("bulkLookup")] public bool BulkLookup { get; set; }
    [JsonPropertyName("rangeLookup")] public bool RangeLookup { get; set; }
    [JsonPropertyName("rawHtmlStored")] public bool RawHtmlStored { get; set; }
    [JsonPropertyName("dumpStored")] public bool DumpStored { get; set; }
    [JsonPropertyName("cacheMode")] public string CacheMode { get; set; } = "none";
    [JsonPropertyName("updateDetected")] public bool UpdateDetected { get; set; }
    [JsonPropertyName("warning")] public string? Warning { get; set; }
}

public sealed class AgentLearningCandidateContract
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("topic")] public string Topic { get; set; } = string.Empty;
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = "needs_review";
    [JsonPropertyName("humanReviewRequired")] public bool HumanReviewRequired { get; set; } = true;
    [JsonPropertyName("licenseNotes")] public List<AgentLicenseNoteContract> LicenseNotes { get; set; } = [];
    [JsonPropertyName("authorizedUse")] public AgentAuthorizedUseContract AuthorizedUse { get; set; } = new();
    [JsonPropertyName("safeForApply")] public bool SafeForApply { get; set; }
}

public sealed class AgentSourceSnapshotContract
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("sourceVersion")] public string SourceVersion { get; set; } = string.Empty;
    [JsonPropertyName("retrievedAt")] public string RetrievedAt { get; set; } = string.Empty;
    [JsonPropertyName("sanitized")] public bool Sanitized { get; set; } = true;
    [JsonPropertyName("rawStored")] public bool RawStored { get; set; }
    [JsonPropertyName("updateDetected")] public bool UpdateDetected { get; set; }
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
}

public sealed class AgentLicenseNoteContract
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("license")] public string License { get; set; } = string.Empty;
    [JsonPropertyName("note")] public string Note { get; set; } = string.Empty;
}

public sealed class AgentAuthorizedUseContract
{
    [JsonPropertyName("authorized")] public bool Authorized { get; set; } = true;
    [JsonPropertyName("authorizationScope")] public string AuthorizationScope { get; set; } = string.Empty;
    [JsonPropertyName("codeAnalysisAllowed")] public bool CodeAnalysisAllowed { get; set; }
    [JsonPropertyName("selectiveIncorporationAllowed")] public bool SelectiveIncorporationAllowed { get; set; }
    [JsonPropertyName("requiresHumanReview")] public bool RequiresHumanReview { get; set; } = true;
}
