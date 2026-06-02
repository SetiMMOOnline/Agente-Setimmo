using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RagnaForge.Agent.Core.Knowledge;

public sealed class KnowledgeService
{
    private readonly string _agentRoot;
    public string? LastReadOnlyIndexWarning { get; private set; }
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public KnowledgeService(string agentRoot)
    {
        if (string.IsNullOrWhiteSpace(agentRoot))
            throw new ArgumentNullException(nameof(agentRoot));

        _agentRoot = Path.GetFullPath(agentRoot);
    }

    /// <summary>
    /// Loads all registered knowledge sources from knowledge/sources/*.json.
    /// </summary>
    public List<KnowledgeSource> LoadSources()
    {
        var list = new List<KnowledgeSource>();
        var sourcesDir = Path.Combine(_agentRoot, "knowledge", "sources");
        if (!Directory.Exists(sourcesDir))
            return list;

        foreach (var file in Directory.GetFiles(sourcesDir, "*.json"))
        {
            KnowledgePathGuard.EnforceBoundary(_agentRoot, file);
            try
            {
                var content = File.ReadAllText(file);
                var source = JsonSerializer.Deserialize<KnowledgeSource>(content, JsonOptions);
                if (source != null)
                {
                    NormalizeSource(source);
                    list.Add(source);
                }
            }
            catch
            {
                // Gracefully ignore corrupt files to prevent crashes
            }
        }

        return list;
    }

    public KnowledgeSource? GetSource(string id) =>
        LoadSources().FirstOrDefault(source => source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Loads all curated knowledge packs from knowledge/packs/*.json.
    /// </summary>
    public List<KnowledgePack> LoadPacks()
    {
        var list = new List<KnowledgePack>();
        var packsDir = Path.Combine(_agentRoot, "knowledge", "packs");
        if (!Directory.Exists(packsDir))
            return list;

        foreach (var file in Directory.GetFiles(packsDir, "*.json"))
        {
            KnowledgePathGuard.EnforceBoundary(_agentRoot, file);
            try
            {
                var content = File.ReadAllText(file);
                var pack = JsonSerializer.Deserialize<KnowledgePack>(content, JsonOptions);
                if (pack != null)
                {
                    NormalizePack(pack);
                    list.Add(pack);
                }
            }
            catch
            {
                // Gracefully ignore corrupt packs to prevent crashes
            }
        }

        return list;
    }

    public KnowledgePack? GetPack(string id) =>
        LoadPacks().FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public List<KnowledgeSourceSnapshot> LoadSnapshots()
    {
        var list = new List<KnowledgeSourceSnapshot>();
        var snapshotsDir = Path.Combine(_agentRoot, "knowledge", "snapshots");
        if (!Directory.Exists(snapshotsDir))
            return list;

        foreach (var file in Directory.GetFiles(snapshotsDir, "*.json"))
        {
            KnowledgePathGuard.EnforceBoundary(_agentRoot, file);
            try
            {
                var content = File.ReadAllText(file);
                var snapshot = JsonSerializer.Deserialize<KnowledgeSourceSnapshot>(content, JsonOptions);
                if (snapshot != null)
                {
                    NormalizeSnapshot(snapshot);
                    list.Add(snapshot);
                }
            }
            catch
            {
                // Ignore corrupt snapshots safely.
            }
        }

        return list
            .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(s => s.RetrievedAt, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public KnowledgeSourceSnapshot? GetSnapshot(string id) =>
        LoadSnapshots().FirstOrDefault(snapshot => snapshot.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public KnowledgeSourceSnapshot? GetLatestSnapshotForSource(string sourceId) =>
        LoadSnapshots()
            .Where(snapshot => snapshot.SourceId.Equals(sourceId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.RetrievedAt, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

    public List<LearningCandidate> LoadLearningCandidates()
    {
        var list = new List<LearningCandidate>();
        var candidatesDir = Path.Combine(_agentRoot, "data", "learning", "candidates");
        if (!Directory.Exists(candidatesDir))
            return list;

        foreach (var file in Directory.GetFiles(candidatesDir, "*.json"))
        {
            KnowledgePathGuard.EnforceBoundary(_agentRoot, file);
            try
            {
                var content = File.ReadAllText(file);
                var candidate = JsonSerializer.Deserialize<LearningCandidate>(content, JsonOptions);
                if (candidate != null)
                {
                    NormalizeLearningCandidate(candidate);
                    list.Add(candidate);
                }
            }
            catch
            {
                // Ignore corrupt candidates safely.
            }
        }

        return list
            .OrderBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public LearningCandidate? GetLearningCandidate(string id) =>
        LoadLearningCandidates().FirstOrDefault(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public List<KnowledgeSourceAssessment> BuildSourceAssessments()
    {
        var snapshots = LoadSnapshots();
        return LoadSources()
            .Select(source => AssessSource(source, snapshots))
            .OrderBy(assessment => assessment.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<KnowledgePackAssessment> BuildPackAssessments() =>
        LoadPacks()
            .Select(AssessPack)
            .OrderBy(p => p.PackId, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public object BuildFreshnessReport()
    {
        var assessments = BuildPackAssessments();
        var sourceAssessments = BuildSourceAssessments();
        var snapshots = LoadSnapshots();
        return new
        {
            totalPacks = assessments.Count,
            totalSources = sourceAssessments.Count,
            totalSnapshots = snapshots.Count,
            warnings = assessments.Sum(a => a.Warnings.Count),
            errors = assessments.Sum(a => a.Errors.Count) + sourceAssessments.Sum(a => a.Errors.Count),
            byStatus = assessments
                .GroupBy(a => a.Status, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
            byFreshness = assessments
                .GroupBy(a => a.FreshnessState, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
            sourcesByFreshness = sourceAssessments
                .GroupBy(a => a.FreshnessState, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
            packs = assessments,
            sources = sourceAssessments,
            snapshots = new
            {
                totalSnapshots = snapshots.Count,
                sanitizedSnapshots = snapshots.Count(snapshot => snapshot.Sanitized),
                rawStoredSnapshots = snapshots.Count(snapshot => snapshot.RawStored),
                canPromoteToKnowledgeCandidates = snapshots.Count(snapshot => snapshot.CanPromoteToKnowledgeCandidates)
            },
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = false
        };
    }

    /// <summary>
    /// Loads the consolidated index from knowledge/index/knowledge.index.json without writing.
    /// If index is missing or corrupt, builds a transient in-memory index and emits a safe warning.
    /// </summary>
    public KnowledgeIndex LoadIndexReadOnly()
    {
        LastReadOnlyIndexWarning = null;
        var indexPath = Path.Combine(_agentRoot, "knowledge", "index", "knowledge.index.json");
        KnowledgePathGuard.EnforceBoundary(_agentRoot, indexPath);
        if (File.Exists(indexPath))
        {
            KnowledgePathGuard.EnforceBoundary(_agentRoot, indexPath);
            try
            {
                var content = File.ReadAllText(indexPath);
                var index = JsonSerializer.Deserialize<KnowledgeIndex>(content, JsonOptions);
                if (index != null)
                    return index;
            }
            catch
            {
                LastReadOnlyIndexWarning = "Knowledge index could not be read; using a transient in-memory index. Run knowledge build to refresh the local controlled index.";
                return BuildIndexInMemory();
            }
        }

        LastReadOnlyIndexWarning = "Knowledge index is missing; using a transient in-memory index. Run knowledge build to persist the local controlled index.";
        return BuildIndexInMemory();
    }

    /// <summary>
    /// Builds a consolidated search index from all active packs and saves it.
    /// </summary>
    public KnowledgeIndex BuildIndex()
    {
        return BuildIndexCore(persist: true);
    }

    /// <summary>
    /// Builds a consolidated search index from all active packs without writing it.
    /// </summary>
    public KnowledgeIndex BuildIndexInMemory()
    {
        return BuildIndexCore(persist: false);
    }

    private KnowledgeIndex BuildIndexCore(bool persist)
    {
        var index = new KnowledgeIndex();
        var packs = LoadPacks();

        var topicsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tagsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entityTypesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filePatternsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceRefsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pack in packs)
        {
            foreach (var entry in pack.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                    continue;

                index.Entries[entry.Id] = entry;

                if (!string.IsNullOrWhiteSpace(entry.Topic))
                    topicsSet.Add(entry.Topic);

                foreach (var tag in entry.Tags)
                    tagsSet.Add(tag);

                foreach (var et in entry.EntityTypes)
                    entityTypesSet.Add(et);

                foreach (var fp in entry.FilePatterns)
                    filePatternsSet.Add(fp);

                foreach (var sr in entry.SourceRefs)
                    sourceRefsSet.Add(sr);
            }
        }

        index.Topics = [.. topicsSet.OrderBy(x => x)];
        index.Tags = [.. tagsSet.OrderBy(x => x)];
        index.EntityTypes = [.. entityTypesSet.OrderBy(x => x)];
        index.FilePatterns = [.. filePatternsSet.OrderBy(x => x)];
        index.SourceRefs = [.. sourceRefsSet.OrderBy(x => x)];

        if (persist)
        {
            var indexDir = Path.Combine(_agentRoot, "knowledge", "index");
            var indexPath = Path.Combine(indexDir, "knowledge.index.json");
            KnowledgePathGuard.EnforceBoundary(_agentRoot, indexDir);
            KnowledgePathGuard.EnforceBoundary(_agentRoot, indexPath);

            Directory.CreateDirectory(indexDir);
            var indexJson = JsonSerializer.Serialize(index, JsonOptions);
            File.WriteAllText(indexPath, indexJson);
        }

        return index;
    }

    /// <summary>
    /// Searches knowledge entries matching standard queries and filters.
    /// </summary>
    public List<KnowledgeResult> Search(KnowledgeQuery query)
    {
        var index = LoadIndexReadOnly();
        var results = new List<KnowledgeResult>();

        var terms = (query.Query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToList();

        foreach (var entry in index.Entries.Values)
        {
            // Category filter
            if (!string.IsNullOrWhiteSpace(query.Category) &&
                !entry.Category.Equals(query.Category, StringComparison.OrdinalIgnoreCase))
                continue;

            // EntityType filter
            if (!string.IsNullOrWhiteSpace(query.EntityType) &&
                !entry.EntityTypes.Any(et => et.Equals(query.EntityType, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Tags filter
            if (query.Tags != null && query.Tags.Count > 0 &&
                !query.Tags.Any(t => entry.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                continue;

            // Score matching
            double score = 0;
            var matchedTags = new List<string>();

            if (terms.Count > 0)
            {
                foreach (var term in terms)
                {
                    if (entry.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                        score += 15;

                    if (entry.Topic.Contains(term, StringComparison.OrdinalIgnoreCase))
                        score += 10;

                    if (entry.Id.Contains(term, StringComparison.OrdinalIgnoreCase))
                        score += 5;

                    foreach (var tag in entry.Tags)
                    {
                        if (tag.Contains(term, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 8;
                            if (!matchedTags.Contains(tag)) matchedTags.Add(tag);
                        }
                    }

                    if (entry.Summary.Contains(term, StringComparison.OrdinalIgnoreCase))
                        score += 5;

                    if (entry.Details.Contains(term, StringComparison.OrdinalIgnoreCase))
                        score += 3;
                }

                // If query is specified and we got a score of 0, it means it doesn't match the query
                if (score == 0)
                    continue;
            }
            else
            {
                // No search query terms: base score on category/entityType filter presence
                score = 1.0;
            }

            results.Add(new KnowledgeResult
            {
                EntryId = entry.Id,
                Title = entry.Title,
                Summary = entry.Summary,
                Details = query.IncludeDetails ? entry.Details : null,
                Confidence = entry.Confidence,
                SourceRefs = query.IncludeSources ? entry.SourceRefs : [],
                SourceIds = query.IncludeSources ? entry.SourceIds : [],
                Warnings = entry.Warnings,
                MatchedTags = matchedTags,
                Score = score
            });
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Title)
            .Take(query.Limit)
            .ToList();
    }

    /// <summary>
    /// Explains a topic or entityType using standard entries.
    /// </summary>
    public List<KnowledgeResult> Explain(string topicOrEntityType)
    {
        if (string.IsNullOrWhiteSpace(topicOrEntityType))
            return [];

        // Exact match by topic or entityType
        var q1 = new KnowledgeQuery { Query = "", Limit = 5, IncludeDetails = true, IncludeSources = true };
        var index = LoadIndexReadOnly();

        var matches = index.Entries.Values
            .Where(e => e.Topic.Equals(topicOrEntityType, StringComparison.OrdinalIgnoreCase) ||
                        e.EntityTypes.Any(et => et.Equals(topicOrEntityType, StringComparison.OrdinalIgnoreCase)) ||
                        e.Category.Equals(topicOrEntityType, StringComparison.OrdinalIgnoreCase))
            .Select(e => new KnowledgeResult
            {
                EntryId = e.Id,
                Title = e.Title,
                Summary = e.Summary,
                Details = e.Details,
                Confidence = e.Confidence,
                SourceRefs = e.SourceRefs,
                SourceIds = e.SourceIds,
                Warnings = e.Warnings,
                MatchedTags = [],
                Score = 100.0
            })
            .ToList();

        if (matches.Count > 0)
            return matches;

        // Fallback to fuzzy search query
        return Search(new KnowledgeQuery
        {
            Query = topicOrEntityType,
            Limit = 3,
            IncludeDetails = true,
            IncludeSources = true
        });
    }

    /// <summary>
    /// Retrieves a single knowledge entry by ID.
    /// </summary>
    public KnowledgeEntry? GetEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var index = LoadIndexReadOnly();
        return index.Entries.TryGetValue(id, out var entry) ? entry : null;
    }

    /// <summary>
    /// Performs static validation check on knowledge packs.
    /// Checks for duplicate IDs, missing source refs, invalid JSON files, and confidence.
    /// </summary>
    public List<string> ValidatePacks()
    {
        var issues = new List<string>();
        var sources = LoadSources().Select(s => s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var packs = LoadPacks();
        var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pack in packs)
        {
            if (string.IsNullOrWhiteSpace(pack.Id))
                issues.Add($"Pack name '{pack.Name}' has an empty or null pack ID.");

            if (string.IsNullOrWhiteSpace(pack.SchemaVersion))
                issues.Add($"Pack '{pack.Id}' has an empty schemaVersion.");

            if (string.IsNullOrWhiteSpace(pack.ReviewedAt))
                issues.Add($"Pack '{pack.Id}' has no reviewedAt metadata.");

            if (pack.Status.Equals("deprecated", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(pack.DeprecationReason))
                issues.Add($"Pack '{pack.Id}' is deprecated but has no deprecationReason.");

            foreach (var entry in pack.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    issues.Add($"Entry '{entry.Title}' in pack '{pack.Name}' has an empty ID.");
                    continue;
                }

                if (!uniqueIds.Add(entry.Id))
                {
                    issues.Add($"Duplicate entry ID detected: '{entry.Id}' is defined multiple times.");
                }

                if (string.IsNullOrWhiteSpace(entry.Title))
                    issues.Add($"Entry '{entry.Id}' has an empty title.");

                if (string.IsNullOrWhiteSpace(entry.Summary))
                    issues.Add($"Entry '{entry.Id}' has an empty summary.");

                if (entry.SourceIds.Count == 0)
                {
                    issues.Add($"Entry '{entry.Id}' does not reference any knowledge sources.");
                }
                else
                {
                    foreach (var sId in entry.SourceIds)
                    {
                        if (!sources.Contains(sId))
                        {
                            issues.Add($"Entry '{entry.Id}' references an undefined source ID: '{sId}'.");
                        }
                    }
                }

                var allowedConfidence = new[] { "authoritative", "informative", "unverified" };
                if (!allowedConfidence.Contains(entry.Confidence.ToLowerInvariant()))
                {
                    issues.Add($"Entry '{entry.Id}' has invalid confidence: '{entry.Confidence}'. Allowed: authoritative, informative, unverified.");
                }
            }
        }

        return issues;
    }

    public List<string> ValidateSources()
    {
        var issues = new List<string>();
        foreach (var source in LoadSources())
        {
            if (string.IsNullOrWhiteSpace(source.Id))
                issues.Add("Knowledge source has an empty id.");
            if (string.IsNullOrWhiteSpace(source.Name))
                issues.Add($"Knowledge source '{source.Id}' has an empty name.");
            if (string.IsNullOrWhiteSpace(source.SourceType))
                issues.Add($"Knowledge source '{source.Id}' has an empty sourceType.");
            if (string.IsNullOrWhiteSpace(source.ExternalReferenceUrl) && string.IsNullOrWhiteSpace(source.Url))
                issues.Add($"Knowledge source '{source.Id}' has no url or externalReferenceUrl.");
            if (!source.ReadOnly)
                issues.Add($"Knowledge source '{source.Id}' must remain readOnly=true.");
            if (string.IsNullOrWhiteSpace(source.TrustPolicy))
                issues.Add($"Knowledge source '{source.Id}' has no trustPolicy.");
            if (string.IsNullOrWhiteSpace(source.ConflictPolicy))
                issues.Add($"Knowledge source '{source.Id}' has no conflictPolicy.");
            if (string.IsNullOrWhiteSpace(source.UpdatePolicy))
                issues.Add($"Knowledge source '{source.Id}' has no updatePolicy.");
            if (string.IsNullOrWhiteSpace(source.RefreshPolicy))
                issues.Add($"Knowledge source '{source.Id}' has no refreshPolicy.");
            if (string.IsNullOrWhiteSpace(source.LearningPolicy))
                issues.Add($"Knowledge source '{source.Id}' has no learningPolicy.");
            if (string.IsNullOrWhiteSpace(source.LicensePolicy))
                issues.Add($"Knowledge source '{source.Id}' has no licensePolicy.");
            if (string.IsNullOrWhiteSpace(source.AuthorizedUsePolicy))
                issues.Add($"Knowledge source '{source.Id}' has no authorizedUsePolicy.");
        }

        foreach (var snapshot in LoadSnapshots())
        {
            if (!snapshot.Sanitized)
                issues.Add($"Snapshot '{snapshot.Id}' is not sanitized.");
            if (snapshot.RawStored)
                issues.Add($"Snapshot '{snapshot.Id}' stores raw content.");
        }

        foreach (var candidate in LoadLearningCandidates())
        {
            if (candidate.RawHtmlStored)
                issues.Add($"Learning candidate '{candidate.Id}' stores raw HTML.");
            if (candidate.SecretStored)
                issues.Add($"Learning candidate '{candidate.Id}' stores secrets.");
        }

        return issues;
    }

    private KnowledgePackAssessment AssessPack(KnowledgePack pack)
    {
        NormalizePack(pack);

        var assessment = new KnowledgePackAssessment
        {
            PackId = pack.Id,
            Name = pack.Name,
            Version = pack.Version,
            SchemaVersion = pack.SchemaVersion,
            Status = pack.Status,
            Theme = pack.Theme,
            ReviewedAt = pack.ReviewedAt,
            ReviewedBy = pack.ReviewedBy,
            SupportedEntityTypes = pack.SupportedEntityTypes,
            SourcePriority = pack.SourcePriority
        };

        if (string.IsNullOrWhiteSpace(pack.SchemaVersion))
            assessment.Errors.Add("schemaVersion is required.");

        if (string.IsNullOrWhiteSpace(pack.ReviewedAt))
        {
            assessment.Warnings.Add("Pack has no reviewedAt metadata.");
            assessment.FreshnessState = "review-missing";
        }
        else if (TryParseTimestamp(pack.ReviewedAt, out var reviewedAt))
        {
            assessment.AgeDays = (int)Math.Max(0, Math.Floor((DateTimeOffset.UtcNow - reviewedAt).TotalDays));
            assessment.FreshnessState = assessment.AgeDays switch
            {
                > 365 => "stale",
                > 180 => "aging",
                _ => "fresh"
            };

            if (assessment.AgeDays > 180)
                assessment.Warnings.Add($"Pack review is {assessment.AgeDays} day(s) old.");
        }
        else
        {
            assessment.Errors.Add("reviewedAt is not a valid timestamp.");
            assessment.FreshnessState = "invalid";
        }

        if (pack.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            assessment.Warnings.Add("Draft pack should be reviewed before it informs important decisions.");

        if (pack.Status.Equals("deprecated", StringComparison.OrdinalIgnoreCase))
            assessment.Warnings.Add(string.IsNullOrWhiteSpace(pack.DeprecationReason)
                ? "Deprecated pack has no deprecationReason."
                : $"Deprecated: {pack.DeprecationReason}");

        return assessment;
    }

    private KnowledgeSourceAssessment AssessSource(KnowledgeSource source, IReadOnlyCollection<KnowledgeSourceSnapshot> snapshots)
    {
        NormalizeSource(source);

        var assessment = new KnowledgeSourceAssessment
        {
            SourceId = source.Id,
            Name = source.Name,
            SourceType = source.SourceType,
            Status = source.Status,
            UpdateMode = source.UpdateMode,
            ReviewedAt = source.ReviewedAt ?? source.LastReviewedUtc,
            LastCheckedAt = source.LastCheckedAt,
            SupportedTopics = source.SupportedTopics
        };

        if (string.IsNullOrWhiteSpace(source.RefreshPolicy))
            assessment.Errors.Add("refreshPolicy is required.");
        if (string.IsNullOrWhiteSpace(source.LearningPolicy))
            assessment.Errors.Add("learningPolicy is required.");
        if (string.IsNullOrWhiteSpace(source.LicensePolicy))
            assessment.Errors.Add("licensePolicy is required.");
        if (string.IsNullOrWhiteSpace(source.AuthorizedUsePolicy))
            assessment.Errors.Add("authorizedUsePolicy is required.");
        if (!source.ReadOnly)
            assessment.Errors.Add("Source must remain readOnly=true.");

        var latestSnapshot = snapshots
            .Where(snapshot => snapshot.SourceId.Equals(source.Id, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.RetrievedAt, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        assessment.LatestSnapshotId = latestSnapshot?.Id;
        assessment.NextDueAt = ResolveNextDueAt(source)?.ToString("O");

        if (source.Status.Equals("deprecated", StringComparison.OrdinalIgnoreCase))
        {
            assessment.FreshnessState = "deprecated";
            assessment.Warnings.Add("Source is deprecated.");
        }
        else if (ResolveNextDueAt(source) is { } nextDueAt)
        {
            if (nextDueAt <= DateTimeOffset.UtcNow)
            {
                assessment.FreshnessState = "due";
                assessment.Warnings.Add("Source refresh is due.");
            }
            else
            {
                assessment.FreshnessState = "fresh";
            }
        }
        else
        {
            assessment.FreshnessState = "manual";
            assessment.Warnings.Add("Source has no scheduled due date; manual review only.");
        }

        return assessment;
    }

    private void NormalizePack(KnowledgePack pack)
    {
        if (string.IsNullOrWhiteSpace(pack.Title))
            pack.Title = pack.Name;

        if (string.IsNullOrWhiteSpace(pack.Theme))
            pack.Theme = InferTheme(pack);

        if (string.IsNullOrWhiteSpace(pack.Status))
            pack.Status = "validated";

        if (string.IsNullOrWhiteSpace(pack.SchemaVersion))
            pack.SchemaVersion = "1.0";

        if (string.IsNullOrWhiteSpace(pack.ReviewedAt))
            pack.ReviewedAt = string.IsNullOrWhiteSpace(pack.GeneratedAtUtc) ? null : pack.GeneratedAtUtc;

        if (string.IsNullOrWhiteSpace(pack.ReviewedBy))
            pack.ReviewedBy = string.IsNullOrWhiteSpace(pack.GeneratedBy) ? "Agente Setimmo" : pack.GeneratedBy;

        if (pack.SupportedEntityTypes.Count == 0)
        {
            pack.SupportedEntityTypes = pack.Entries
                .SelectMany(e => e.EntityTypes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (pack.SourcePriority == 0)
        {
            var firstSourceId = pack.SourceIds.FirstOrDefault() ?? "internal-knowledge";
            pack.SourcePriority = KnowledgeTrustPolicy.PriorityForSource(firstSourceId, "internal-knowledge");
        }

        if (string.IsNullOrWhiteSpace(pack.Provenance))
            pack.Provenance = "internal-curated-pack";

        if (string.IsNullOrWhiteSpace(pack.TrustPolicy))
            pack.TrustPolicy = "Curated internal knowledge pack. Local project data still wins when local evidence conflicts.";

        if (string.IsNullOrWhiteSpace(pack.ConflictPolicy))
            pack.ConflictPolicy = "Use as read-only context; do not block solely on pack metadata without local evidence.";
    }

    private void NormalizeSource(KnowledgeSource source)
    {
        if (string.IsNullOrWhiteSpace(source.Description))
            source.Description = source.Name;

        if (string.IsNullOrWhiteSpace(source.ExternalReferenceUrl))
            source.ExternalReferenceUrl = source.Url;

        if (string.IsNullOrWhiteSpace(source.Provenance))
            source.Provenance = source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase)
                ? "authorized-github-reference"
                : source.Id.Equals("rathena-board", StringComparison.OrdinalIgnoreCase)
                    ? "community-reference-metadata"
                    : "internal-curated-source";

        if (string.IsNullOrWhiteSpace(source.License))
            source.License = "metadata-only";

        if (string.IsNullOrWhiteSpace(source.PermissionNote))
            source.PermissionNote = "Read-only metadata use only.";

        if (string.IsNullOrWhiteSpace(source.TrustLevel))
            source.TrustLevel = "informative";

        source.ReadOnly = true;

        if (string.IsNullOrWhiteSpace(source.AllowedUse))
            source.AllowedUse = "read-only hints, provenance, metadata refresh planning.";

        if (string.IsNullOrWhiteSpace(source.TrustPolicy))
            source.TrustPolicy = "External/community sources are contextual only. Local project data and curated internal knowledge have priority.";

        if (string.IsNullOrWhiteSpace(source.ConflictPolicy))
            source.ConflictPolicy = "Source cannot block alone. Use only as hint/context unless local evidence confirms the issue.";

        if (string.IsNullOrWhiteSpace(source.UpdatePolicy))
            source.UpdatePolicy = "Manual review and curated metadata updates only.";

        if (string.IsNullOrWhiteSpace(source.RefreshPolicy))
            source.RefreshPolicy = "Metadata-only refresh. No crawler, no pagination, no follow links, no raw HTML, no dump.";

        if (string.IsNullOrWhiteSpace(source.LearningPolicy))
            source.LearningPolicy = "Observation creates review-first learning candidates only. No self-modification.";

        if (string.IsNullOrWhiteSpace(source.LicensePolicy))
            source.LicensePolicy = string.IsNullOrWhiteSpace(source.License)
                ? "Preserve source license notes and provenance."
                : $"Preserve declared license context: {source.License}.";

        if (string.IsNullOrWhiteSpace(source.AuthorizedUsePolicy))
            source.AuthorizedUsePolicy = source.PermissionNote;

        if (source.SourcePriority == 0)
            source.SourcePriority = KnowledgeTrustPolicy.PriorityForSource(source.Id, source.SourceType);

        source.CanBlock = false;

        if (string.IsNullOrWhiteSpace(source.ReviewedAt))
            source.ReviewedAt = source.LastReviewedUtc;

        if (string.IsNullOrWhiteSpace(source.ReviewedBy))
            source.ReviewedBy = "Agente Setimmo";

        if (string.IsNullOrWhiteSpace(source.Status))
            source.Status = "validated";

        if (source.RefreshCadenceDays <= 0)
            source.RefreshCadenceDays = source.Id.Equals("rathena-board", StringComparison.OrdinalIgnoreCase) ? 30 : 14;

        if (source.StaleAfterDays <= 0)
            source.StaleAfterDays = source.Id.Equals("rathena-board", StringComparison.OrdinalIgnoreCase) ? 90 : 60;

        if (source.DeprecatedAfterDays <= 0)
            source.DeprecatedAfterDays = 180;

        if (string.IsNullOrWhiteSpace(source.UpdateMode))
            source.UpdateMode = source.Id.Equals("rathena-board", StringComparison.OrdinalIgnoreCase)
                ? "metadata-only"
                : source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase)
                    ? "github-authorized-code-reference"
                    : "manual";

        if (source.MaxRequestsPerRun <= 0)
            source.MaxRequestsPerRun = source.Id.Equals("rathena-board", StringComparison.OrdinalIgnoreCase) ? 1 : 4;

        if (source.TimeoutMs <= 0)
            source.TimeoutMs = 3000;

        if (string.IsNullOrWhiteSpace(source.RateLimit))
            source.RateLimit = "1 request per second";

        source.RequiresHumanReview = true;

        if (string.IsNullOrWhiteSpace(source.LastCheckedAt))
            source.LastCheckedAt = source.ReviewedAt;

        if (string.IsNullOrWhiteSpace(source.NextDueAt) &&
            DateTimeOffset.TryParse(source.LastCheckedAt, out var lastChecked))
        {
            source.NextDueAt = lastChecked.AddDays(source.RefreshCadenceDays).ToString("O");
        }
    }

    private static void NormalizeSnapshot(KnowledgeSourceSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.MetadataHash))
            snapshot.MetadataHash = snapshot.SourceVersion;

        if (string.IsNullOrWhiteSpace(snapshot.ContentHash))
            snapshot.ContentHash = snapshot.MetadataHash;

        snapshot.Sanitized = true;
        snapshot.RawStored = false;
    }

    private static void NormalizeLearningCandidate(LearningCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.Status))
            candidate.Status = "needs_review";

        candidate.HumanReviewRequired = true;
        candidate.RawHtmlStored = false;
        candidate.SecretStored = false;
        candidate.SafeForApply = false;
    }

    private static DateTimeOffset? ResolveNextDueAt(KnowledgeSource source)
    {
        if (DateTimeOffset.TryParse(source.NextDueAt, out var nextDue))
            return nextDue;

        if (DateTimeOffset.TryParse(source.LastCheckedAt, out var lastChecked) && source.RefreshCadenceDays > 0)
            return lastChecked.AddDays(source.RefreshCadenceDays);

        return null;
    }

    private static string InferTheme(KnowledgePack pack)
    {
        var id = pack.Id.ToLowerInvariant();
        if (id.Contains("consumable", StringComparison.OrdinalIgnoreCase)) return "consumables";
        if (id.Contains("equipment", StringComparison.OrdinalIgnoreCase) || id.Contains("item", StringComparison.OrdinalIgnoreCase)) return "equipment";
        if (id.Contains("mob", StringComparison.OrdinalIgnoreCase) || id.Contains("monster", StringComparison.OrdinalIgnoreCase)) return "monsters";
        if (id.Contains("map", StringComparison.OrdinalIgnoreCase)) return "maps";
        if (id.Contains("quest", StringComparison.OrdinalIgnoreCase)) return "quests";
        if (id.Contains("skill", StringComparison.OrdinalIgnoreCase)) return "skills";
        if (id.Contains("asset", StringComparison.OrdinalIgnoreCase) || id.Contains("grf", StringComparison.OrdinalIgnoreCase)) return "grf-client-assets";
        if (id.Contains("canon", StringComparison.OrdinalIgnoreCase) || id.Contains("governance", StringComparison.OrdinalIgnoreCase)) return "governance";
        if (id.Contains("divine-pride", StringComparison.OrdinalIgnoreCase) || id.Contains("ratemyserver", StringComparison.OrdinalIgnoreCase)) return "external-reference-policies";
        return "custom-overrides";
    }

    private static bool TryParseTimestamp(string? value, out DateTimeOffset parsed) =>
        DateTimeOffset.TryParse(value, out parsed);
}
