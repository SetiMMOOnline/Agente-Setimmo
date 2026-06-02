using System.Text.Json.Serialization;
using RagnaForge.Agent.Core.Entities;

namespace RagnaForge.Agent.Core.Knowledge;

public enum KnowledgeHintSeverity { Info, Hint, Warning, Error }
public enum KnowledgeRiskLevel { Low, Medium, High, Critical }

public sealed class KnowledgeProvenance
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("sourceName")] public string SourceName { get; set; } = string.Empty;
    [JsonPropertyName("sourceKind")] public string SourceKind { get; set; } = string.Empty;
    [JsonPropertyName("origin")] public string Origin { get; set; } = "internal";
    [JsonPropertyName("externalReferenceUrl")] public string? ExternalReferenceUrl { get; set; }
    [JsonPropertyName("packVersion")] public string? PackVersion { get; set; }
    [JsonPropertyName("reviewedAt")] public string? ReviewedAt { get; set; }
    [JsonPropertyName("retrievedAt")] public string? RetrievedAt { get; set; }
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("priority")] public int Priority { get; set; }
    [JsonPropertyName("trustPolicy")] public string TrustPolicy { get; set; } = string.Empty;
    [JsonPropertyName("conflictPolicy")] public string ConflictPolicy { get; set; } = string.Empty;
    [JsonPropertyName("canBlock")] public bool CanBlock { get; set; }
    [JsonPropertyName("reasonNotBlocking")] public string ReasonNotBlocking { get; set; } = "Reference source only; local data has priority.";
}

public sealed class KnowledgeHint
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = string.Empty;
    [JsonPropertyName("entityId")] public string? EntityId { get; set; }
    [JsonPropertyName("entityName")] public string? EntityName { get; set; }
    [JsonPropertyName("severity")] public string Severity { get; set; } = "hint";
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = string.Empty;
    [JsonPropertyName("localEvidence")] public string? LocalEvidence { get; set; }
    [JsonPropertyName("referenceEvidence")] public string? ReferenceEvidence { get; set; }
    [JsonPropertyName("winningSource")] public string WinningSource { get; set; } = "local";
    [JsonPropertyName("losingSources")] public List<string> LosingSources { get; set; } = [];
    [JsonPropertyName("provenance")] public List<KnowledgeProvenance> Provenance { get; set; } = [];
    [JsonPropertyName("confidence")] public double Confidence { get; set; } = 0.7;
    [JsonPropertyName("priority")] public int Priority { get; set; } = 70;
    [JsonPropertyName("humanReviewRecommended")] public bool HumanReviewRecommended { get; set; }
    [JsonPropertyName("blocksReadOnly")] public bool BlocksReadOnly { get; set; }
    [JsonPropertyName("blocksDryRun")] public bool BlocksDryRun { get; set; }
    [JsonPropertyName("blocksApply")] public bool BlocksApply { get; set; } = true;
    [JsonPropertyName("suggestedAction")] public string SuggestedAction { get; set; } = "Review context before making changes.";
    [JsonPropertyName("reasonNotBlocking")] public string ReasonNotBlocking { get; set; } = "External reference alone cannot block local workflow.";
    [JsonPropertyName("generatedAt")] public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EntityConflict
{
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = string.Empty;
    [JsonPropertyName("entityId")] public string? EntityId { get; set; }
    [JsonPropertyName("localValue")] public string? LocalValue { get; set; }
    [JsonPropertyName("referenceValue")] public string? ReferenceValue { get; set; }
    [JsonPropertyName("localSources")] public List<string> LocalSources { get; set; } = [];
    [JsonPropertyName("referenceSources")] public List<string> ReferenceSources { get; set; } = [];
    [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;
    [JsonPropertyName("severity")] public string Severity { get; set; } = "warning";
    [JsonPropertyName("riskLevel")] public string RiskLevel { get; set; } = "low";
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = string.Empty;
    [JsonPropertyName("humanReviewRecommended")] public bool HumanReviewRecommended { get; set; } = true;
    [JsonPropertyName("blocksReadOnly")] public bool BlocksReadOnly { get; set; }
    [JsonPropertyName("blocksDryRun")] public bool BlocksDryRun { get; set; }
    [JsonPropertyName("blocksApply")] public bool BlocksApply { get; set; } = true;
    [JsonPropertyName("reasonNotBlocking")] public string ReasonNotBlocking { get; set; } = "Reference source alone cannot block local workflow.";
    [JsonPropertyName("nextSafeAction")] public string NextSafeAction { get; set; } = "Review manually; do not apply automatically.";
    [JsonPropertyName("customOverrideCandidate")] public bool CustomOverrideCandidate { get; set; }
}

public sealed class KnowledgeLookupOptions
{
    public bool WithKnowledge { get; set; }
    public bool KnowledgeLocalOnly { get; set; }
    public bool NoLiveReference { get; set; }
    public string LiveSource { get; set; } = "auto";
    public int LiveTimeoutMs { get; set; } = 3000;
    public int MaxLiveRequestsPerSource { get; set; } = 1;
    public int MaxTotalLiveRequests { get; set; } = 2;
    public bool AllowSanitizedMetadataCache { get; set; }
}

public sealed class ControlledReferenceLookupDecision
{
    [JsonPropertyName("liveLookup")] public bool LiveLookup { get; set; }
    [JsonPropertyName("lookupMode")] public string LookupMode { get; set; } = "autonomous-controlled";
    [JsonPropertyName("decisionReason")] public string DecisionReason { get; set; } = string.Empty;
    [JsonPropertyName("requestCount")] public int RequestCount { get; set; }
    [JsonPropertyName("timeoutMs")] public int TimeoutMs { get; set; } = 3000;
    [JsonPropertyName("cacheMode")] public string CacheMode { get; set; } = "none";
    [JsonPropertyName("rateLimitApplied")] public bool RateLimitApplied { get; set; } = true;
    [JsonPropertyName("rawHtmlStored")] public bool RawHtmlStored { get; set; }
    [JsonPropertyName("dumpStored")] public bool DumpStored { get; set; }
    [JsonPropertyName("linksFollowed")] public bool LinksFollowed { get; set; }
    [JsonPropertyName("bulkLookup")] public bool BulkLookup { get; set; }
    [JsonPropertyName("rangeLookup")] public bool RangeLookup { get; set; }
    [JsonPropertyName("sourceSelectedBy")] public string SourceSelectedBy { get; set; } = "policy";
    [JsonPropertyName("selectedSource")] public string SelectedSource { get; set; } = "none";
    [JsonPropertyName("warning")] public string? Warning { get; set; }
}

public sealed class EntityKnowledgeContext
{
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = string.Empty;
    [JsonPropertyName("entityId")] public string? EntityId { get; set; }
    [JsonPropertyName("entityName")] public string? EntityName { get; set; }
    [JsonPropertyName("internalKnowledgeMatches")] public List<KnowledgeResult> InternalKnowledgeMatches { get; set; } = [];
    [JsonPropertyName("referenceMatches")] public List<KnowledgeResult> ReferenceMatches { get; set; } = [];
    [JsonPropertyName("hints")] public List<KnowledgeHint> Hints { get; set; } = [];
    [JsonPropertyName("conflicts")] public List<EntityConflict> Conflicts { get; set; } = [];
    [JsonPropertyName("provenance")] public List<KnowledgeProvenance> Provenance { get; set; } = [];
    [JsonPropertyName("controlledLiveReference")] public ControlledReferenceLookupDecision ControlledLiveReference { get; set; } = new();
    [JsonPropertyName("episodeGate")] public object EpisodeGate { get; set; } = new { status = "unknown", reason = "No episode evidence evaluated." };
    [JsonPropertyName("decisionExplanation")] public string DecisionExplanation { get; set; } = string.Empty;
    [JsonPropertyName("risk")] public string Risk { get; set; } = "low";
    [JsonPropertyName("sourceFreshness")] public List<KnowledgeSourceAssessment> SourceFreshness { get; set; } = [];
    [JsonPropertyName("learningCandidatesAvailable")] public int LearningCandidatesAvailable { get; set; }
    [JsonPropertyName("nextSafeActions")] public List<string> NextSafeActions { get; set; } = [];
    [JsonPropertyName("safeForReadOnlyWork")] public bool SafeForReadOnlyWork { get; set; } = true;
    [JsonPropertyName("safeForDryRun")] public bool SafeForDryRun { get; set; } = true;
    [JsonPropertyName("safeForApply")] public bool SafeForApply { get; set; }
}

public static class KnowledgeTrustPolicy
{
    public static int PriorityForSource(string sourceId, string sourceKind)
    {
        if (sourceId.Equals("local-project-config", StringComparison.OrdinalIgnoreCase)) return 100;
        if (sourceId.Equals("local-project-data", StringComparison.OrdinalIgnoreCase)) return 95;
        if (sourceId.Equals("rathena-local", StringComparison.OrdinalIgnoreCase)) return 90;
        if (sourceId.Equals("patch-client-local", StringComparison.OrdinalIgnoreCase)) return 85;
        if (sourceId.Equals("grf-local-index", StringComparison.OrdinalIgnoreCase)) return 80;
        if (sourceKind.Equals("internal_governance", StringComparison.OrdinalIgnoreCase)) return 75;
        if (sourceId.Equals("divine-pride", StringComparison.OrdinalIgnoreCase)) return 55;
        if (sourceId.Equals("ratemyserver", StringComparison.OrdinalIgnoreCase)) return 55;
        if (sourceKind.Equals("external-live", StringComparison.OrdinalIgnoreCase)) return 45;
        return 70;
    }

    public static bool CanBlockAlone(string sourceId, string sourceKind) =>
        !sourceId.Equals("divine-pride", StringComparison.OrdinalIgnoreCase) &&
        !sourceId.Equals("ratemyserver", StringComparison.OrdinalIgnoreCase) &&
        !sourceKind.Equals("external-live", StringComparison.OrdinalIgnoreCase);
}

public static class CustomProjectPolicy
{
    public static bool IsCustomOverride(object? localEntity) => localEntity switch
    {
        ItemEntry item => IsImportLike(item.DbMode, item.RelativePath),
        MonsterEntry monster => IsImportLike(monster.DbMode, monster.RelativePath),
        _ => false
    };

    public static string Describe(object? localEntity) =>
        IsCustomOverride(localEntity)
            ? "Divergencia esperada para projeto custom/import. Requer revisao humana e nao bloqueia dry-run por si so."
            : "Sem evidencia local clara de override custom.";

    private static bool IsImportLike(string? dbMode, string? relativePath) =>
        string.Equals(dbMode, "import", StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrWhiteSpace(relativePath) &&
         relativePath.Contains("db/import", StringComparison.OrdinalIgnoreCase));
}

public static class ProgressiveEpisodePolicy
{
    public static object Evaluate(string entityType, object? localEntity, bool hasReferenceContext)
    {
        if (CustomProjectPolicy.IsCustomOverride(localEntity))
        {
            return new
            {
                status = "customAllowed",
                reason = "Local import/custom override evidence exists. Review manually before implementation."
            };
        }

        if (hasReferenceContext)
        {
            return new
            {
                status = "allowed",
                reason = $"Reference context exists for {entityType}; still requires local validation."
            };
        }

        return new
        {
            status = "unknown",
            reason = $"No explicit episode evidence was found for {entityType}. Human review is required."
        };
    }
}

public static class CustomOverrideDetector
{
    public static bool Detect(object? localEntity) => CustomProjectPolicy.IsCustomOverride(localEntity);
}

public static class CustomOverrideHintService
{
    public static KnowledgeHint Create(string entityType, string? entityId, string? entityName) => new()
    {
        Id = "knowledge.custom.override",
        EntityType = entityType,
        EntityId = entityId,
        EntityName = entityName,
        Severity = "info",
        Category = "CustomOverride",
        Message = "Local custom override detected.",
        Explanation = "Divergencia com referencias externas pode ser esperada para projeto custom/progressivo.",
        WinningSource = "local-project-data",
        Priority = 95,
        Confidence = 0.9,
        HumanReviewRecommended = true,
        Provenance = [KnowledgeContextService.LocalProvenance()]
    };
}

public sealed class KnowledgeContextService
{
    private static readonly HashSet<string> ExternalReferenceSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "divine-pride",
        "ratemyserver"
    };

    private static readonly HashSet<string> AllowedLiveEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "item",
        "monster",
        "equipment",
        "map",
        "npc",
        "skill",
        "quest"
    };

    private readonly string _agentRoot;
    private readonly KnowledgeService _knowledgeService;

    public KnowledgeContextService(string agentRoot)
    {
        _agentRoot = agentRoot;
        _knowledgeService = new KnowledgeService(agentRoot);
    }

    public EntityKnowledgeContext BuildContext(string entityType, int? id, string? name, object? localEntity, KnowledgeLookupOptions options)
    {
        var queryText = BuildQuery(entityType, id, name);
        var matches = string.IsNullOrWhiteSpace(queryText)
            ? []
            : _knowledgeService.Search(new KnowledgeQuery { Query = queryText, Limit = 10, IncludeDetails = true, IncludeSources = true });
        var sources = _knowledgeService.LoadSources();
        var provenance = BuildProvenance(matches, sources);
        var referenceMatches = matches.Where(m => m.SourceIds.Any(s => ExternalReferenceSources.Contains(s))).ToList();
        var internalMatches = matches.Except(referenceMatches).ToList();
        var liveDecision = DecideLiveLookup(entityType, id, name, internalMatches, referenceMatches, options);
        var hints = BuildHints(entityType, id, name, localEntity, internalMatches, referenceMatches, provenance, liveDecision);
        var conflicts = BuildConflicts(entityType, id, name, localEntity, referenceMatches);
        var episodeGate = ProgressiveEpisodePolicy.Evaluate(entityType, localEntity, referenceMatches.Count > 0 || internalMatches.Count > 0);
        var sourceFreshness = _knowledgeService.BuildSourceAssessments();
        var learningCandidates = _knowledgeService.LoadLearningCandidates();
        var risk = conflicts.Any(c => c.RiskLevel == "critical") ? "critical"
            : conflicts.Any(c => c.RiskLevel == "high") ? "high"
            : conflicts.Any(c => c.RiskLevel == "medium") ? "medium"
            : hints.Any(h => h.Severity is "warning" or "error") ? "medium"
            : "low";

        return new EntityKnowledgeContext
        {
            EntityType = entityType,
            EntityId = id?.ToString(),
            EntityName = name,
            InternalKnowledgeMatches = internalMatches,
            ReferenceMatches = referenceMatches,
            Hints = hints,
            Conflicts = conflicts,
            Provenance = provenance,
            ControlledLiveReference = liveDecision,
            EpisodeGate = episodeGate,
            DecisionExplanation = liveDecision.DecisionReason,
            Risk = risk,
            SourceFreshness = sourceFreshness,
            LearningCandidatesAvailable = learningCandidates.Count,
            NextSafeActions = [
                "Review hints and conflicts manually.",
                "Use dry-run for any planned change.",
                CustomProjectPolicy.IsCustomOverride(localEntity) ? "Treat this entity as potential custom override and preserve local data priority." : "Prefer local project and rAthena evidence over external references.",
                learningCandidates.Count > 0 ? "Review learning candidates before promoting new knowledge." : "No pending learning candidates were detected.",
                "Do not apply automatically."
            ],
            SafeForReadOnlyWork = true,
            SafeForDryRun = !conflicts.Any(c => c.BlocksDryRun),
            SafeForApply = false
        };
    }

    public object BuildCoverage()
    {
        var index = TryLoadAnyEntityIndex();
        var entries = _knowledgeService.LoadIndexReadOnly().Entries.Values.ToList();
        var conflicts = BuildConflictsReport();
        var freshness = _knowledgeService.BuildPackAssessments();
        var sourceFreshness = _knowledgeService.BuildSourceAssessments();
        return new
        {
            items = CoverageCount(index?.Items.Count ?? 0, entries, "item"),
            monsters = CoverageCount(index?.Monsters.Count ?? 0, entries, "monster", "mob"),
            maps = CoverageCount(index?.Maps.Count ?? 0, entries, "map"),
            npcs = CoverageCount(index?.Npcs.Count ?? 0, entries, "npc"),
            skills = CoverageCount(0, entries, "skill"),
            quests = CoverageCount(0, entries, "quest"),
            equipment = CoverageCount(0, entries, "equipment"),
            lowCoverageEntityTypes = new[] { "skill", "quest", "equipment" },
            hints = entries.Count,
            conflicts = conflicts.Count,
            staleOrDeprecatedPacks = freshness.Where(p => p.FreshnessState is "stale" or "aging" || p.Status.Equals("deprecated", StringComparison.OrdinalIgnoreCase)).ToList(),
            sourceFreshness,
            internalLibraries = _knowledgeService.LoadSources().Select(source => new
            {
                source.Id,
                source.Name,
                source.SourceType,
                source.UpdateMode,
                source.SupportedTopics,
                source.RequiresHumanReview
            }).ToList(),
            liveLookupPolicy = "Coverage is broad analysis; live lookup is skipped by anti-bulk policy.",
            safeForApply = false
        };
    }

    public List<EntityConflict> BuildConflictsReport(string? entityType = null)
    {
        var conflicts = new List<EntityConflict>();
        var index = TryLoadAnyEntityIndex();
        if (index is null)
        {
            conflicts.Add(new EntityConflict
            {
                EntityType = "index",
                Source = "local-cache",
                Severity = "warning",
                RiskLevel = "medium",
                Explanation = "Entity index is missing or stale; run index before broad conflict analysis.",
                NextSafeAction = "Run ragnaforge index --entities --json."
            });
            return conflicts;
        }

        foreach (var duplicate in index.Items.Where(i => i.Side == "server").GroupBy(i => i.Id).Where(g => g.Count() > 1).Take(25))
        {
            var values = duplicate.ToList();
            conflicts.Add(new EntityConflict
            {
                EntityType = "item",
                EntityId = duplicate.Key.ToString(),
                LocalValue = string.Join(", ", values.Select(i => i.AegisName).Distinct()),
                Source = "local-project-data",
                Severity = "error",
                RiskLevel = "critical",
                Explanation = "Duplicate server-side item ID has local evidence.",
                LocalSources = values.Select(i => $"{i.RelativePath}:{i.Line}").Distinct().ToList(),
                BlocksReadOnly = false,
                BlocksDryRun = true,
                BlocksApply = true,
                ReasonNotBlocking = "Read-only analysis remains allowed; dry-run should avoid conflicting ID."
            });
        }

        foreach (var assessment in _knowledgeService.BuildPackAssessments().Where(a => a.Warnings.Count > 0 || a.Errors.Count > 0))
        {
            conflicts.Add(new EntityConflict
            {
                EntityType = "knowledge-pack",
                EntityId = assessment.PackId,
                LocalValue = assessment.Name,
                Source = "internal-knowledge",
                Severity = assessment.Errors.Count > 0 ? "error" : "warning",
                RiskLevel = assessment.Errors.Count > 0 ? "high" : "medium",
                Explanation = assessment.Errors.Count > 0
                    ? string.Join(" ", assessment.Errors)
                    : string.Join(" ", assessment.Warnings),
                BlocksReadOnly = false,
                BlocksDryRun = false,
                BlocksApply = true,
                ReasonNotBlocking = "Freshness and deprecation issues inform review but do not block read-only work alone.",
                NextSafeAction = "Review pack metadata and refresh reviewedAt/schema metadata."
            });
        }

        foreach (var assessment in _knowledgeService.BuildSourceAssessments().Where(a => a.Warnings.Count > 0 || a.Errors.Count > 0))
        {
            conflicts.Add(new EntityConflict
            {
                EntityType = "knowledge-source",
                EntityId = assessment.SourceId,
                LocalValue = assessment.Name,
                Source = "internal-knowledge",
                Severity = assessment.Errors.Count > 0 ? "error" : "warning",
                RiskLevel = assessment.Errors.Count > 0 ? "high" : "medium",
                Explanation = assessment.Errors.Count > 0
                    ? string.Join(" ", assessment.Errors)
                    : string.Join(" ", assessment.Warnings),
                BlocksReadOnly = false,
                BlocksDryRun = false,
                BlocksApply = true,
                ReasonNotBlocking = "Source freshness and policy issues inform review but do not block read-only work alone.",
                NextSafeAction = "Review source metadata, freshness, and snapshot policy."
            });
        }

        if (conflicts.Count == 0)
        {
            conflicts.Add(new EntityConflict
            {
                EntityType = "knowledge",
                Source = "internal-reference",
                Severity = "hint",
                RiskLevel = "low",
                Explanation = "No high-signal conflicts were detected from current local index and internal reference packs."
            });
        }

        return string.IsNullOrWhiteSpace(entityType)
            ? conflicts
            : conflicts.Where(c => c.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public object BuildAskAnswer(string question, KnowledgeLookupOptions options)
    {
        var query = new KnowledgeQuery { Query = question, Limit = 5, IncludeDetails = true, IncludeSources = true };
        var results = _knowledgeService.Search(query);
        var genericQuestion = !ContainsEntitySpecificSignal(question);
        var liveDecision = genericQuestion
            ? Skipped(options, "Question is generic; live reference requires a specific entity.")
            : DecideLiveLookup("item", null, question, results, [], options);

        return new
        {
            question,
            answer = results.Count == 0
                ? "No strong local knowledge match was found. Treat this as uncertain and review local data."
                : "Local Knowledge Library returned relevant context. Use it as evidence for manual review, not automatic apply.",
            evidence = results,
            uncertainty = results.Count == 0 ? "high" : "medium",
            liveLookupDecision = liveDecision,
            nextSafeActions = new[] { "Run find/validate with --with-knowledge for a specific entity.", "Use dry-run before any planned change." },
            safeForApply = false
        };
    }

    public ControlledReferenceLookupDecision DecideLiveLookup(string entityType, int? id, string? name, IReadOnlyCollection<KnowledgeResult> internalMatches, IReadOnlyCollection<KnowledgeResult> referenceMatches, KnowledgeLookupOptions options)
    {
        if (!options.WithKnowledge)
            return Skipped(options, "--with-knowledge was not requested.");
        if (options.KnowledgeLocalOnly)
            return Skipped(options, "--knowledge-local-only blocks live reference lookup.");
        if (options.NoLiveReference)
            return Skipped(options, "--no-live-reference blocks live reference lookup.");
        if (!AllowedLiveEntityTypes.Contains(entityType))
            return Skipped(options, $"Entity type '{entityType}' is not allowlisted for live reference.");
        if (id is null && string.IsNullOrWhiteSpace(name))
            return Skipped(options, "Live reference requires a single specific entity ID or name.");
        if (options.MaxLiveRequestsPerSource < 1 || options.MaxTotalLiveRequests < 1)
            return Skipped(options, "Live request budget is zero.");
        if (!options.LiveSource.Equals("auto", StringComparison.OrdinalIgnoreCase) && !ExternalReferenceSources.Contains(options.LiveSource))
            return Skipped(options, $"Live source '{options.LiveSource}' is not allowlisted.");

        var useful = referenceMatches.Count == 0 || internalMatches.Count == 0;
        if (!useful)
            return Skipped(options, "Internal knowledge is sufficient; live reference is not necessary.");

        var selected = options.LiveSource.Equals("auto", StringComparison.OrdinalIgnoreCase)
            ? PreferredSourceFor(entityType)
            : options.LiveSource;

        return new ControlledReferenceLookupDecision
        {
            LiveLookup = false,
            DecisionReason = "Autonomous policy approved only a point lookup, but real HTTP is unavailable by policy in this build; returning controlled warning instead of scraping.",
            RequestCount = 0,
            TimeoutMs = Math.Clamp(options.LiveTimeoutMs, 500, 3000),
            CacheMode = options.AllowSanitizedMetadataCache ? "sanitized-metadata" : "none",
            RateLimitApplied = true,
            RawHtmlStored = false,
            DumpStored = false,
            LinksFollowed = false,
            BulkLookup = false,
            RangeLookup = false,
            SourceSelectedBy = options.LiveSource.Equals("auto", StringComparison.OrdinalIgnoreCase) ? "auto" : "user-option",
            SelectedSource = selected,
            Warning = "live lookup unavailable by policy; no request was sent, no HTML was stored, no links were followed, no bulk lookup was attempted."
        };
    }

    public static KnowledgeHint BuildLocalValidationErrorHint(ValidationIssue issue) =>
        new()
        {
            Id = $"local.validation.{issue.Code}".ToLowerInvariant(),
            EntityType = issue.EntityType,
            EntityId = issue.EntityId,
            EntityName = issue.EntityName,
            Severity = "error",
            Category = "LocalValidationError",
            Message = issue.Message,
            Explanation = "Validation error is backed by local evidence.",
            LocalEvidence = issue.SourceFile,
            WinningSource = "local-project-data",
            Confidence = 0.95,
            Priority = 95,
            HumanReviewRecommended = true,
            BlocksReadOnly = false,
            BlocksDryRun = true,
            ReasonNotBlocking = "Read-only analysis is allowed; dry-run should respect the local validation error."
        };

    private List<KnowledgeHint> BuildHints(string entityType, int? id, string? name, object? localEntity, List<KnowledgeResult> internalMatches, List<KnowledgeResult> referenceMatches, List<KnowledgeProvenance> provenance, ControlledReferenceLookupDecision liveDecision)
    {
        var hints = new List<KnowledgeHint>();
        if (CustomOverrideDetector.Detect(localEntity))
            hints.Add(CustomOverrideHintService.Create(entityType, id?.ToString(), name));

        if (localEntity is not null)
        {
            hints.Add(new KnowledgeHint
            {
                Id = "knowledge.local.entity-found",
                EntityType = entityType,
                EntityId = id?.ToString(),
                EntityName = name,
                Severity = "info",
                Category = "LocalEntityFound",
                Message = "Local entity found.",
                Explanation = "Local project data has the highest priority for this workflow.",
                LocalEvidence = "entity-index",
                WinningSource = "local-project-data",
                Priority = 95,
                Confidence = 0.95,
                Provenance = [LocalProvenance()]
            });
        }
        else
        {
            hints.Add(new KnowledgeHint
            {
                Id = "knowledge.local.entity-missing",
                EntityType = entityType,
                EntityId = id?.ToString(),
                EntityName = name,
                Severity = "hint",
                Category = "LocalOnlyEntity",
                Message = "No local entity match was found in the current index.",
                Explanation = "This may be expected for planning or custom content; run index if cache is stale.",
                WinningSource = "local-project-data",
                Priority = 95,
                Provenance = [LocalProvenance()]
            });
        }

        if (internalMatches.Count > 0)
        {
            hints.Add(new KnowledgeHint
            {
                Id = "knowledge.internal.match",
                EntityType = entityType,
                EntityId = id?.ToString(),
                EntityName = name,
                Severity = "hint",
                Category = "InternalReferenceMatch",
                Message = "Internal Knowledge Library returned contextual matches.",
                Explanation = "Use these entries as implementation guidance and review context.",
                ReferenceEvidence = string.Join(", ", internalMatches.Take(3).Select(r => r.EntryId)),
                WinningSource = "internal-knowledge",
                Priority = 70,
                Provenance = provenance.Where(p => !ExternalReferenceSources.Contains(p.SourceId)).ToList()
            });
        }

        if (referenceMatches.Count > 0)
        {
            hints.Add(new KnowledgeHint
            {
                Id = "knowledge.external.reference-similar",
                EntityType = entityType,
                EntityId = id?.ToString(),
                EntityName = name,
                Severity = "hint",
                Category = "ExternalReferenceSimilar",
                Message = "External reference library returned contextual matches.",
                Explanation = "Divine Pride/RateMyServer are contextual references only and do not override local data.",
                ReferenceEvidence = string.Join(", ", referenceMatches.Take(3).Select(r => r.EntryId)),
                WinningSource = "local-project-data",
                LosingSources = referenceMatches.SelectMany(r => r.SourceIds).Distinct().ToList(),
                Priority = 55,
                HumanReviewRecommended = true,
                Provenance = provenance.Where(p => ExternalReferenceSources.Contains(p.SourceId)).ToList()
            });
        }

        foreach (var sourceHint in BuildSourceContextHints(entityType, id, name))
            hints.Add(sourceHint);

        hints.Add(new KnowledgeHint
        {
            Id = liveDecision.LiveLookup ? "knowledge.live.used" : "knowledge.live.skipped",
            EntityType = entityType,
            EntityId = id?.ToString(),
            EntityName = name,
            Severity = liveDecision.Warning is null ? "info" : "warning",
            Category = liveDecision.LiveLookup ? "ControlledLiveReferenceUsed" : "ControlledLiveReferenceSkipped",
            Message = liveDecision.LiveLookup ? "Controlled live reference was used." : "Controlled live reference was skipped.",
            Explanation = liveDecision.DecisionReason,
            ReferenceEvidence = liveDecision.Warning,
            WinningSource = "local-project-data",
            LosingSources = liveDecision.SelectedSource == "none" ? [] : [liveDecision.SelectedSource],
            Provenance = [LiveProvenance(liveDecision)]
        });

        return hints;
    }

    private List<KnowledgeHint> BuildSourceContextHints(string entityType, int? id, string? name)
    {
        var hints = new List<KnowledgeHint>();
        var sources = _knowledgeService.LoadSources().ToDictionary(source => source.Id, StringComparer.OrdinalIgnoreCase);

        if (sources.TryGetValue("robrowserlegacy-remoteclient-js", out var remoteClientSource) &&
            entityType is "item" or "equipment" or "monster" or "npc" or "map")
        {
            hints.Add(new KnowledgeHint
            {
                Id = "knowledge.source.remoteclient.asset-pipeline",
                EntityType = entityType,
                EntityId = id?.ToString(),
                EntityName = name,
                Severity = "info",
                Category = "AuthorizedCodeReference",
                Message = "RemoteClient-JS can inform browser asset serving, DATA.INI, cache/index, and encoding review.",
                Explanation = "Use this source for read-only architecture hints around GRF asset delivery, WebSocket proxying, and cache behavior.",
                WinningSource = "local-project-data",
                HumanReviewRecommended = true,
                Provenance = [BuildSourceProvenance(remoteClientSource)],
                ReasonNotBlocking = "Authorized code reference is contextual only; local project data has priority."
            });
        }

        if (sources.TryGetValue("robrowserlegacy", out var browserSource) &&
            entityType is "item" or "equipment" or "monster" or "npc" or "map")
        {
            hints.Add(new KnowledgeHint
            {
                Id = "knowledge.source.robrowser.browser-client",
                EntityType = entityType,
                EntityId = id?.ToString(),
                EntityName = name,
                Severity = "info",
                Category = "AuthorizedCodeReference",
                Message = "roBrowserLegacy can inform browser client, WebGL, wsProxy, worker, and pathfinding review.",
                Explanation = "Use this source for read-only client architecture context and future UI/API integration planning.",
                WinningSource = "local-project-data",
                HumanReviewRecommended = true,
                Provenance = [BuildSourceProvenance(browserSource)],
                ReasonNotBlocking = "Authorized code reference is contextual only; local project data has priority."
            });
        }

        if (sources.TryGetValue("rathena-board", out var boardSource))
        {
            hints.Add(new KnowledgeHint
            {
                Id = "knowledge.source.rathena-board.community",
                EntityType = entityType,
                EntityId = id?.ToString(),
                EntityName = name,
                Severity = "hint",
                Category = "CommunityReference",
                Message = "rAthena Board can provide metadata-only community routing hints for support, database, source, and client-side topics.",
                Explanation = "Forum context remains metadata-only and never blocks alone. Local project and curated internal data have priority.",
                WinningSource = "local-project-data",
                HumanReviewRecommended = true,
                Provenance = [BuildSourceProvenance(boardSource)],
                ReasonNotBlocking = "Forum metadata is contextual only; no crawler, no pagination, no raw post copying."
            });
        }

        return hints;
    }

    private static List<EntityConflict> BuildConflicts(string entityType, int? id, string? name, object? localEntity, List<KnowledgeResult> referenceMatches)
    {
        if (referenceMatches.Count == 0)
            return [];

        var conflicts = new List<EntityConflict>();
        var customOverride = CustomProjectPolicy.IsCustomOverride(localEntity);
        if (localEntity is null)
        {
            conflicts.Add(new EntityConflict
            {
                EntityType = entityType,
                EntityId = id?.ToString(),
                LocalValue = null,
                ReferenceValue = string.Join(", ", referenceMatches.Take(3).Select(r => r.Title)),
                Source = "internal-reference-library",
                Severity = "hint",
                RiskLevel = "low",
                Explanation = "Reference-only entity; use as context, not authoritative data.",
                CustomOverrideCandidate = false
            });
        }
        else if (!string.IsNullOrWhiteSpace(name) && referenceMatches.Any(r => !r.Title.Contains(name, StringComparison.OrdinalIgnoreCase)))
        {
            conflicts.Add(new EntityConflict
            {
                EntityType = entityType,
                EntityId = id?.ToString(),
                LocalValue = name,
                ReferenceValue = string.Join(", ", referenceMatches.Take(3).Select(r => r.Title)),
                Source = "internal-reference-library",
                Severity = "warning",
                RiskLevel = customOverride ? "low" : "medium",
                Explanation = customOverride
                    ? "Name divergence is compatible with a local custom override. Local data wins by default."
                    : "Name divergence between local query and external reference context. Local data wins by default.",
                CustomOverrideCandidate = customOverride,
                ReasonNotBlocking = "External reference alone cannot block local workflow."
            });
        }

        return conflicts;
    }

    private List<KnowledgeProvenance> BuildProvenance(IEnumerable<KnowledgeResult> results, List<KnowledgeSource> sources)
    {
        var byId = sources.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        return results.SelectMany(r => r.SourceIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id =>
            {
                byId.TryGetValue(id, out var source);
                var sourceKind = source?.SourceType ?? "unknown";
                return new KnowledgeProvenance
                {
                    SourceId = id,
                    SourceName = source?.Name ?? id,
                    SourceKind = sourceKind,
                    Origin = source?.Provenance ?? (ExternalReferenceSources.Contains(id) ? "external-reference-registered-locally" : "internal"),
                    ExternalReferenceUrl = source?.ExternalReferenceUrl ?? source?.Url,
                    ReviewedAt = source?.LastReviewedUtc,
                    Confidence = rConfidence(source?.TrustLevel),
                    Priority = KnowledgeTrustPolicy.PriorityForSource(id, sourceKind),
                    TrustPolicy = string.IsNullOrWhiteSpace(source?.TrustPolicy) ? source?.AllowedUse ?? string.Empty : source.TrustPolicy,
                    ConflictPolicy = string.IsNullOrWhiteSpace(source?.ConflictPolicy)
                        ? (ExternalReferenceSources.Contains(id)
                            ? "External reference can create hints/warnings only; local data has priority."
                            : "Internal knowledge supports validation and explanation.")
                        : source.ConflictPolicy,
                    CanBlock = KnowledgeTrustPolicy.CanBlockAlone(id, sourceKind)
                };
            })
            .ToList();
    }

    private EntityIndex? TryLoadAnyEntityIndex()
    {
        var cacheRoot = Path.Combine(_agentRoot, "cache", "agent");
        if (!Directory.Exists(cacheRoot))
            return null;

        var file = Directory.EnumerateFiles(cacheRoot, "entities*.json", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(cacheRoot, "project_index.json", SearchOption.AllDirectories))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (file is null)
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<EntityIndex>(File.ReadAllText(file), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static object CoverageCount(int localCount, IReadOnlyCollection<KnowledgeEntry> entries, params string[] entityTypes)
    {
        var refs = entries.Count(e => e.EntityTypes.Any(t => entityTypes.Contains(t, StringComparer.OrdinalIgnoreCase)));
        return new
        {
            localCount,
            internalReferenceCount = refs,
            withHints = refs,
            conflicts = 0,
            coverageRatio = localCount == 0 ? 0 : Math.Round((double)Math.Min(localCount, refs) / localCount, 3)
        };
    }

    private static ControlledReferenceLookupDecision Skipped(KnowledgeLookupOptions options, string reason) =>
        new()
        {
            LiveLookup = false,
            DecisionReason = reason,
            RequestCount = 0,
            TimeoutMs = Math.Clamp(options.LiveTimeoutMs, 500, 3000),
            CacheMode = options.AllowSanitizedMetadataCache ? "sanitized-metadata" : "none",
            RateLimitApplied = true,
            RawHtmlStored = false,
            DumpStored = false,
            LinksFollowed = false,
            BulkLookup = false,
            RangeLookup = false,
            SelectedSource = "none",
            SourceSelectedBy = options.LiveSource.Equals("auto", StringComparison.OrdinalIgnoreCase) ? "auto" : "user-option"
        };

    private static string PreferredSourceFor(string entityType) =>
        entityType.Equals("monster", StringComparison.OrdinalIgnoreCase) ? "ratemyserver" : "divine-pride";

    private static string BuildQuery(string entityType, int? id, string? name) =>
        string.Join(' ', new[] { entityType, id?.ToString(), name }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public static KnowledgeProvenance LocalProvenance() =>
        new()
        {
            SourceId = "local-project-data",
            SourceName = "Local Project Data",
            SourceKind = "local",
            Origin = "local",
            Confidence = 0.95,
            Priority = 95,
            CanBlock = true,
            ReasonNotBlocking = "Local evidence can block unsafe dry-run/apply decisions when validation confirms an error."
        };

    private static KnowledgeProvenance LiveProvenance(ControlledReferenceLookupDecision decision) =>
        new()
        {
            SourceId = decision.SelectedSource,
            SourceName = decision.SelectedSource,
            SourceKind = "external-live",
            Origin = "external-live",
            RetrievedAt = DateTimeOffset.UtcNow.ToString("O"),
            Confidence = 0.45,
            Priority = 45,
            CanBlock = false,
            TrustPolicy = "Controlled live references are contextual only.",
            ConflictPolicy = "Never blocks alone; local data has priority.",
            ReasonNotBlocking = "Live reference source only; local data has priority."
        };

    private static KnowledgeProvenance BuildSourceProvenance(KnowledgeSource source) =>
        new()
        {
            SourceId = source.Id,
            SourceName = source.Name,
            SourceKind = source.SourceType,
            Origin = source.Provenance ?? "internal",
            ExternalReferenceUrl = source.ExternalReferenceUrl ?? source.Url,
            ReviewedAt = source.ReviewedAt ?? source.LastReviewedUtc,
            Confidence = rConfidence(source.TrustLevel),
            Priority = KnowledgeTrustPolicy.PriorityForSource(source.Id, source.SourceType),
            TrustPolicy = source.TrustPolicy,
            ConflictPolicy = source.ConflictPolicy,
            CanBlock = false,
            ReasonNotBlocking = "Context source only; local project data has priority."
        };

    private static double rConfidence(string? trustLevel) =>
        trustLevel?.ToLowerInvariant() switch
        {
            "authoritative" => 0.9,
            "high" => 0.85,
            "informative" => 0.65,
            _ => 0.5
        };

    private static bool ContainsEntitySpecificSignal(string question) =>
        question.Any(char.IsDigit) ||
        question.Contains("item", StringComparison.OrdinalIgnoreCase) ||
        question.Contains("monstro", StringComparison.OrdinalIgnoreCase) ||
        question.Contains("monster", StringComparison.OrdinalIgnoreCase) ||
        question.Contains("npc", StringComparison.OrdinalIgnoreCase) ||
        question.Contains("map", StringComparison.OrdinalIgnoreCase);
}
