using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RagnaForge.Agent.Core.Knowledge;

public sealed class KnowledgeSource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("externalReferenceUrl")]
    public string? ExternalReferenceUrl { get; set; }

    [JsonPropertyName("provenance")]
    public string? Provenance { get; set; }

    [JsonPropertyName("localPath")]
    public string? LocalPath { get; set; }

    [JsonPropertyName("license")]
    public string License { get; set; } = string.Empty;

    [JsonPropertyName("permissionNote")]
    public string PermissionNote { get; set; } = string.Empty;

    [JsonPropertyName("lastReviewedUtc")]
    public string LastReviewedUtc { get; set; } = string.Empty;

    [JsonPropertyName("trustLevel")]
    public string TrustLevel { get; set; } = string.Empty;

    [JsonPropertyName("readOnly")]
    public bool ReadOnly { get; set; }

    [JsonPropertyName("allowedUse")]
    public string AllowedUse { get; set; } = string.Empty;

    [JsonPropertyName("supportedEntityTypes")]
    public List<string> SupportedEntityTypes { get; set; } = [];

    [JsonPropertyName("trustPolicy")]
    public string TrustPolicy { get; set; } = string.Empty;

    [JsonPropertyName("conflictPolicy")]
    public string ConflictPolicy { get; set; } = string.Empty;

    [JsonPropertyName("updatePolicy")]
    public string UpdatePolicy { get; set; } = string.Empty;

    [JsonPropertyName("refreshPolicy")]
    public string RefreshPolicy { get; set; } = string.Empty;

    [JsonPropertyName("learningPolicy")]
    public string LearningPolicy { get; set; } = string.Empty;

    [JsonPropertyName("licensePolicy")]
    public string LicensePolicy { get; set; } = string.Empty;

    [JsonPropertyName("authorizedUsePolicy")]
    public string AuthorizedUsePolicy { get; set; } = string.Empty;

    [JsonPropertyName("supportedTopics")]
    public List<string> SupportedTopics { get; set; } = [];

    [JsonPropertyName("sourcePriority")]
    public int SourcePriority { get; set; }

    [JsonPropertyName("canBlock")]
    public bool CanBlock { get; set; }

    [JsonPropertyName("reviewedAt")]
    public string? ReviewedAt { get; set; }

    [JsonPropertyName("reviewedBy")]
    public string? ReviewedBy { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "validated";

    [JsonPropertyName("changelog")]
    public List<string> Changelog { get; set; } = [];

    [JsonPropertyName("refreshCadenceDays")]
    public int RefreshCadenceDays { get; set; }

    [JsonPropertyName("lastCheckedAt")]
    public string? LastCheckedAt { get; set; }

    [JsonPropertyName("nextDueAt")]
    public string? NextDueAt { get; set; }

    [JsonPropertyName("staleAfterDays")]
    public int StaleAfterDays { get; set; }

    [JsonPropertyName("deprecatedAfterDays")]
    public int DeprecatedAfterDays { get; set; }

    [JsonPropertyName("updateMode")]
    public string UpdateMode { get; set; } = "manual";

    [JsonPropertyName("autoRefreshAllowed")]
    public bool AutoRefreshAllowed { get; set; }

    [JsonPropertyName("maxRequestsPerRun")]
    public int MaxRequestsPerRun { get; set; } = 1;

    [JsonPropertyName("timeoutMs")]
    public int TimeoutMs { get; set; } = 3000;

    [JsonPropertyName("rateLimit")]
    public string RateLimit { get; set; } = "1 request per second";

    [JsonPropertyName("requiresHumanReview")]
    public bool RequiresHumanReview { get; set; } = true;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";
}

public sealed class KnowledgeEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("appliesTo")]
    public string AppliesTo { get; set; } = string.Empty;

    [JsonPropertyName("entityTypes")]
    public List<string> EntityTypes { get; set; } = [];

    [JsonPropertyName("filePatterns")]
    public List<string> FilePatterns { get; set; } = [];

    [JsonPropertyName("sourceIds")]
    public List<string> SourceIds { get; set; } = [];

    [JsonPropertyName("sourceRefs")]
    public List<string> SourceRefs { get; set; } = [];

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];

    [JsonPropertyName("relatedEntries")]
    public List<string> RelatedEntries { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("lastReviewedUtc")]
    public string LastReviewedUtc { get; set; } = string.Empty;
}

public sealed class KnowledgePack
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "validated";

    [JsonPropertyName("reviewedAt")]
    public string? ReviewedAt { get; set; }

    [JsonPropertyName("reviewedBy")]
    public string? ReviewedBy { get; set; }

    [JsonPropertyName("sourcePriority")]
    public int SourcePriority { get; set; }

    [JsonPropertyName("supportedEntityTypes")]
    public List<string> SupportedEntityTypes { get; set; } = [];

    [JsonPropertyName("changelog")]
    public List<string> Changelog { get; set; } = [];

    [JsonPropertyName("freshnessPolicy")]
    public string FreshnessPolicy { get; set; } = "warnAfterDays:180;staleAfterDays:365";

    [JsonPropertyName("deprecationReason")]
    public string? DeprecationReason { get; set; }

    [JsonPropertyName("provenance")]
    public string? Provenance { get; set; }

    [JsonPropertyName("trustPolicy")]
    public string? TrustPolicy { get; set; }

    [JsonPropertyName("conflictPolicy")]
    public string? ConflictPolicy { get; set; }

    [JsonPropertyName("entries")]
    public List<KnowledgeEntry> Entries { get; set; } = [];

    [JsonPropertyName("sourceIds")]
    public List<string> SourceIds { get; set; } = [];

    [JsonPropertyName("generatedBy")]
    public string GeneratedBy { get; set; } = "Agente Setimmo";

    [JsonPropertyName("generatedAtUtc")]
    public string GeneratedAtUtc { get; set; } = string.Empty;
}

public sealed class KnowledgePackAssessment
{
    [JsonPropertyName("packId")]
    public string PackId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = string.Empty;

    [JsonPropertyName("reviewedAt")]
    public string? ReviewedAt { get; set; }

    [JsonPropertyName("reviewedBy")]
    public string? ReviewedBy { get; set; }

    [JsonPropertyName("ageDays")]
    public int? AgeDays { get; set; }

    [JsonPropertyName("freshnessState")]
    public string FreshnessState { get; set; } = "unknown";

    [JsonPropertyName("supportedEntityTypes")]
    public List<string> SupportedEntityTypes { get; set; } = [];

    [JsonPropertyName("sourcePriority")]
    public int SourcePriority { get; set; }

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];
}

public sealed class KnowledgeIndex
{
    [JsonPropertyName("entries")]
    public Dictionary<string, KnowledgeEntry> Entries { get; set; } = [];

    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("entityTypes")]
    public List<string> EntityTypes { get; set; } = [];

    [JsonPropertyName("filePatterns")]
    public List<string> FilePatterns { get; set; } = [];

    [JsonPropertyName("sourceRefs")]
    public List<string> SourceRefs { get; set; } = [];
}

public sealed class KnowledgeQuery
{
    public string Query { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public List<string>? Tags { get; set; }
    public int Limit { get; set; } = 10;
    public bool IncludeDetails { get; set; } = true;
    public bool IncludeSources { get; set; } = true;
}

public sealed class KnowledgeResult
{
    [JsonPropertyName("entryId")]
    public string EntryId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Details { get; set; }

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;

    [JsonPropertyName("sourceRefs")]
    public List<string> SourceRefs { get; set; } = [];

    [JsonPropertyName("sourceIds")]
    public List<string> SourceIds { get; set; } = [];

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];

    [JsonPropertyName("matchedTags")]
    public List<string> MatchedTags { get; set; } = [];

    [JsonPropertyName("score")]
    public double Score { get; set; }
}
