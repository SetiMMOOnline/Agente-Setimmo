using System.Text;
using System.Text.Json;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Entities;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Knowledge;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class KnowledgeReportCommand
{
    private readonly string _agentRoot;
    private readonly string _format;

    public KnowledgeReportCommand(string agentRoot, string format)
    {
        _agentRoot = agentRoot;
        _format = format;
    }

    public JsonOutput Execute()
    {
        var governance = OperationGovernanceProfiles.EvaluateWithoutValidation("report-knowledge", applyEngineImplemented: true, rollbackEngineImplemented: true);
        var context = new KnowledgeContextService(_agentRoot);
        var service = new KnowledgeService(_agentRoot);
        var conflicts = context.BuildConflictsReport();
        var coverage = context.BuildCoverage();
        var freshness = service.BuildFreshnessReport();
        var markdown = GenerateMarkdown(conflicts, coverage, freshness, service.LoadLearningCandidates());
        var output = JsonOutput.Success("report-knowledge", "Knowledge report generated for stdout.");
        output.Data = new
        {
            format = _format,
            markdown = _format.Equals("md", StringComparison.OrdinalIgnoreCase) ? markdown : null,
            conflicts,
            coverage,
            freshness,
            safeForReadOnlyWork = governance.SafeForReadOnlyWork,
            safeForDryRun = governance.SafeForDryRun,
            safeForApply = governance.SafeForApply,
            safeForProductionApply = governance.SafeForProductionApply,
            governance
        };
        return output;
    }

    private static string GenerateMarkdown(List<EntityConflict> conflicts, object coverage, object freshness, List<LearningCandidate> learningCandidates)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Knowledge Review Report");
        sb.AppendLine();
        sb.AppendLine("## 1. Resumo executivo");
        sb.AppendLine("Hints, conflicts, coverage and pack freshness were generated in read-only mode.");
        sb.AppendLine();
        sb.AppendLine("## 2. Estado das fontes");
        sb.AppendLine("- Local project data has priority.");
        sb.AppendLine("- Divine Pride and RateMyServer are references only.");
        sb.AppendLine("- Controlled live reference remains unavailable by policy in this build.");
        sb.AppendLine();
        sb.AppendLine("## 3. Ranking de confianca");
        sb.AppendLine("LocalProjectConfig > LocalProjectData > rAthenaLocal > PatchClientLocal > GRFLocalIndex > ValidatedInternalKnowledge > InternalKnowledge > DivinePride/RateMyServer > ControlledLiveReference > UnverifiedReference.");
        sb.AppendLine();
        sb.AppendLine("## 4. Cobertura por entidade");
        sb.AppendLine(JsonSerializer.Serialize(coverage, new JsonSerializerOptions { WriteIndented = true }));
        sb.AppendLine();
        sb.AppendLine("## 5. Principais conflitos");
        foreach (var conflict in conflicts.Take(20))
            sb.AppendLine($"- {conflict.Severity}/{conflict.RiskLevel}: {conflict.Explanation}");
        sb.AppendLine();
        sb.AppendLine("## 6. Principais riscos");
        sb.AppendLine("External references alone never create critical risk. Critical risk requires local evidence.");
        sb.AppendLine();
        sb.AppendLine("## 7. Revisao humana");
        sb.AppendLine("Review warnings and medium/high risks manually before any dry-run plan.");
        sb.AppendLine();
        sb.AppendLine("## 8. Freshness dos packs");
        sb.AppendLine(JsonSerializer.Serialize(freshness, new JsonSerializerOptions { WriteIndented = true }));
        sb.AppendLine();
        sb.AppendLine("## 9. Learning candidates");
        foreach (var candidate in learningCandidates.Take(10))
            sb.AppendLine($"- {candidate.Id}: {candidate.SourceId} / {candidate.Topic} / {candidate.Status}");
        sb.AppendLine();
        sb.AppendLine("## 10. Hint, warning, error");
        sb.AppendLine("- hint: context.");
        sb.AppendLine("- warning: review recommended.");
        sb.AppendLine("- error: local evidence requires correction.");
        sb.AppendLine();
        sb.AppendLine("## 11. Por que Divine Pride/RMS nao bloqueiam sozinhos");
        sb.AppendLine("They are external references registered locally; local data has priority.");
        sb.AppendLine();
        sb.AppendLine("## 12. Consulta externa autonoma controlada");
        sb.AppendLine("Only point lookup is allowed. In this build it remains unavailable by policy: no crawler, no follow links, no bulk, no raw HTML, no dump, no real cache by default.");
        sb.AppendLine();
        sb.AppendLine("## 13. Proximas acoes seguras");
        sb.AppendLine("- Run targeted find with --with-knowledge.");
        sb.AppendLine("- Review knowledge refresh plan and snapshots.");
        sb.AppendLine("- Review learning candidates before promoting new knowledge.");
        sb.AppendLine("- Use --knowledge-local-only when internet must be blocked.");
        sb.AppendLine("- Use dry-run planning before implementation.");
        sb.AppendLine();
        sb.AppendLine("safeForApply=validator-governed");
        return sb.ToString();
    }
}

public sealed class ExternalDataReportCommand
{
    private readonly string _agentRoot;
    private readonly string _format;

    public ExternalDataReportCommand(string agentRoot, string format)
    {
        _agentRoot = agentRoot;
        _format = format;
    }

    public JsonOutput Execute()
    {
        var governance = OperationGovernanceProfiles.EvaluateWithoutValidation("report-external-data", applyEngineImplemented: true, rollbackEngineImplemented: true);
        var configDir = Path.Combine(_agentRoot, "config");
        var triage = new TriageCommand(configDir, _agentRoot, true, "md").Execute();
        if (!triage.Ok)
            return triage;

        var json = JsonSerializer.Serialize(triage.Data);
        using var document = JsonDocument.Parse(json);
        var markdown = document.RootElement.TryGetProperty("markdown", out var mdElement) && mdElement.ValueKind == JsonValueKind.String
            ? mdElement.GetString()
            : string.Empty;

        var output = JsonOutput.Success("report-external-data", "External data report generated for stdout.");
        output.Data = new
        {
            format = _format,
            markdown = _format.Equals("md", StringComparison.OrdinalIgnoreCase) ? markdown : null,
            triage = triage.Data,
            safeForReadOnlyWork = governance.SafeForReadOnlyWork,
            safeForDryRun = governance.SafeForDryRun,
            safeForApply = governance.SafeForApply,
            safeForProductionApply = governance.SafeForProductionApply,
            governance
        };
        return output;
    }
}

public sealed class ReadinessSummaryReportCommand
{
    private readonly string _agentRoot;
    private readonly string _format;

    public ReadinessSummaryReportCommand(string agentRoot, string format)
    {
        _agentRoot = agentRoot;
        _format = format;
    }

    public JsonOutput Execute()
    {
        var governance = OperationGovernanceProfiles.EvaluateWithoutValidation("report-readiness-summary", applyEngineImplemented: true, rollbackEngineImplemented: true);
        var export = new ApiReadinessExportCommand(_agentRoot).Execute();
        var service = new KnowledgeService(_agentRoot);
        var context = new KnowledgeContextService(_agentRoot);
        var markdown = GenerateMarkdown(export.Data, service.BuildPackAssessments(), service.BuildSourceAssessments(), service.LoadLearningCandidates(), context.BuildConflictsReport());

        var output = JsonOutput.Success("report-readiness-summary", "Readiness summary generated for stdout.");
        output.Data = new
        {
            format = _format,
            markdown = _format.Equals("md", StringComparison.OrdinalIgnoreCase) ? markdown : null,
            apiReadiness = export.Data,
            safeForReadOnlyWork = governance.SafeForReadOnlyWork,
            safeForDryRun = governance.SafeForDryRun,
            safeForApply = governance.SafeForApply,
            safeForProductionApply = governance.SafeForProductionApply,
            governance
        };
        return output;
    }

    private static string GenerateMarkdown(object? exportData, List<KnowledgePackAssessment> packs, List<KnowledgeSourceAssessment> sources, List<LearningCandidate> candidates, List<EntityConflict> conflicts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Agent Final API Readiness");
        sb.AppendLine();
        sb.AppendLine("## Resumo");
        sb.AppendLine("The agent is ready to serve a validator-governed API/UI integration layer with controlled implementation flows.");
        sb.AppendLine();
        sb.AppendLine("## API readiness export");
        sb.AppendLine(JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true }));
        sb.AppendLine();
        sb.AppendLine("## Knowledge pack freshness");
        foreach (var pack in packs)
            sb.AppendLine($"- {pack.PackId}: {pack.FreshnessState} ({pack.Status})");
        sb.AppendLine();
        sb.AppendLine("## Source freshness");
        foreach (var source in sources)
            sb.AppendLine($"- {source.SourceId}: {source.FreshnessState} ({source.UpdateMode})");
        sb.AppendLine();
        sb.AppendLine("## Learning candidates");
        foreach (var candidate in candidates)
            sb.AppendLine($"- {candidate.Id}: {candidate.Status}");
        sb.AppendLine();
        sb.AppendLine("## Conflicts");
        foreach (var conflict in conflicts.Take(10))
            sb.AppendLine($"- {conflict.EntityType}: {conflict.Explanation}");
        sb.AppendLine();
        sb.AppendLine("safeForApply=validator-governed");
        return sb.ToString();
    }
}

public sealed class EntityPlanReportCommand
{
    private readonly string _agentRoot;
    private readonly string _entityType;
    private readonly int? _id;
    private readonly string? _name;
    private readonly string? _map;
    private readonly string _format;
    private readonly KnowledgeLookupOptions _options;

    public EntityPlanReportCommand(string agentRoot, string entityType, int? id, string? name, string? map, string format, KnowledgeLookupOptions options)
    {
        _agentRoot = agentRoot;
        _entityType = entityType;
        _id = id;
        _name = name;
        _map = map;
        _format = format;
        _options = options;
    }

    public JsonOutput Execute()
    {
        var governance = OperationGovernanceProfiles.EvaluateWithoutValidation("report-entity-plan", applyEngineImplemented: true, rollbackEngineImplemented: true);
        var configDir = Path.Combine(_agentRoot, "config");
        var plan = new PlanCommand(configDir, _agentRoot, _entityType, _id, _name, _map, _options).Execute();
        if (!plan.Ok)
            return plan;

        var markdown = GenerateMarkdown(plan.Data);
        var output = JsonOutput.Success("report-entity-plan", "Entity plan report generated for stdout.");
        output.Data = new
        {
            format = _format,
            markdown = _format.Equals("md", StringComparison.OrdinalIgnoreCase) ? markdown : null,
            plan = plan.Data,
            safeForReadOnlyWork = governance.SafeForReadOnlyWork,
            safeForDryRun = governance.SafeForDryRun,
            safeForApply = governance.SafeForApply,
            safeForProductionApply = governance.SafeForProductionApply,
            governance
        };
        return output;
    }

    private static string GenerateMarkdown(object? planData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Entity Plan Report");
        sb.AppendLine();
        sb.AppendLine(JsonSerializer.Serialize(planData, new JsonSerializerOptions { WriteIndented = true }));
        sb.AppendLine();
        sb.AppendLine("safeForApply=validator-governed");
        return sb.ToString();
    }
}

public sealed class PlanCommand
{
    private readonly string _configDir;
    private readonly string _agentRoot;
    private readonly string _entityType;
    private readonly int? _id;
    private readonly string? _name;
    private readonly string? _map;
    private readonly KnowledgeLookupOptions _options;

    public PlanCommand(string configDir, string agentRoot, string entityType, int? id, string? name, string? map, KnowledgeLookupOptions options)
    {
        _configDir = configDir;
        _agentRoot = agentRoot;
        _entityType = entityType;
        _id = id;
        _name = name;
        _map = map;
        _options = options;
    }

    public JsonOutput Execute()
    {
        var output = JsonOutput.Success("plan");
        try
        {
            var loader = new ConfigLoader(_configDir);
            var pathsConfig = loader.LoadPathsConfig();
            var safetyConfig = loader.LoadSafetyConfig();
            output.ActiveProfile = pathsConfig.ActiveProfile;
            output.ConfigFingerprint = ConfigFingerprint.Generate(pathsConfig, safetyConfig);

            _options.WithKnowledge = true;
            var index = IndexCommand.LoadCachedIndex(_agentRoot, pathsConfig.ActiveProfile, output.ConfigFingerprint!, "entities");
            var localMatches = FindLocalMatches(index);
            var knowledge = new KnowledgeContextService(_agentRoot).BuildContext(_entityType, _id, _name, localMatches.FirstOrDefault(), _options);
            var requiredFields = GetRequiredFields();
            var missingRequiredFields = GetMissingRequiredFields(requiredFields);
            var safeIdRanges = BuildSafeRanges(index);
            var idSuggestions = BuildIdSuggestions(index, localMatches.Count > 0);
            var dependencyHints = BuildDependencyHints();
            var sourceFreshness = new KnowledgeService(_agentRoot).BuildSourceAssessments();
            var warnings = knowledge.Hints.Where(h => h.Severity is "warning" or "error").Select(h => h.Message).ToList();
            var canon = new GlobalCanonValidator(_agentRoot).Check();
            var governance = OperationGovernanceProfiles.EvaluateWithoutValidation(
                "plan",
                canon,
                applyEngineImplemented: true,
                rollbackEngineImplemented: true,
                hasPlan: true,
                hasDiff: true);
            if (!string.IsNullOrWhiteSpace(knowledge.ControlledLiveReference.Warning))
                warnings.Add(knowledge.ControlledLiveReference.Warning);

            output.Summary = "Create plan generated in dry-run mode. Nothing was applied.";
            output.Data = new
            {
                requestedEntity = new { entityType = _entityType, id = _id, name = _name, map = _map },
                normalizedEntity = new { entityType = _entityType.ToLowerInvariant(), id = _id, name = _name?.Trim(), map = _map?.Trim() },
                requiredFields,
                missingRequiredFields,
                idConflictCheck = new
                {
                    requestedId = _id,
                    hasConflict = _id is not null && localMatches.Count > 0,
                    conflictingLocalEntities = localMatches.Select(ToLocalSummary).ToList(),
                    explanation = _id is null
                        ? "No ID was requested."
                        : localMatches.Count > 0
                            ? "The requested ID is already present in the local index."
                            : "No local ID conflict was detected in the current index."
                },
                idSuggestions,
                safeIdRanges,
                localMatches = localMatches.Select(ToLocalSummary).ToList(),
                knowledgeMatches = knowledge.InternalKnowledgeMatches,
                referenceContext = knowledge.ReferenceMatches,
                knowledgeHints = knowledge.Hints,
                sourceFreshness,
                controlledLiveReference = knowledge.ControlledLiveReference,
                assetHints = BuildAssetHints(),
                dependencyHints,
                episodeGate = knowledge.EpisodeGate,
                riskLevel = knowledge.Risk,
                conflicts = knowledge.Conflicts,
                warnings,
                nextSafeActions = knowledge.NextSafeActions,
                dryRunPlan = new
                {
                    canApply = governance.ApplyEnabled,
                    steps = new[]
                    {
                        "Normalize requested input.",
                        "Review local conflicts and knowledge hints.",
                        "Review dependency and asset hints.",
                        "Generate diff preview in API/UI before any human-approved pipeline step."
                    }
                },
                diffPreviewPlaceholder = "API can generate diff-preview later from this dry-run plan.",
                humanReviewRequired = true,
                canApply = governance.ApplyEnabled,
                safeForReadOnlyWork = governance.SafeForReadOnlyWork,
                safeForDryRun = governance.SafeForDryRun && knowledge.SafeForDryRun,
                safeForApply = governance.SafeForApply,
                safeForProductionApply = governance.SafeForProductionApply,
                governance
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("plan", ex.Message);
        }

        return output;
    }

    private List<string> GetRequiredFields() => _entityType.ToLowerInvariant() switch
    {
        "npc" => new List<string> { "name", "map" },
        "map" => new List<string> { "name" },
        "item" or "equipment" or "monster" or "skill" or "quest" => new List<string> { "id", "name" },
        _ => new List<string> { "name" }
    };

    private List<string> GetMissingRequiredFields(IEnumerable<string> requiredFields)
    {
        var missing = new List<string>();
        foreach (var field in requiredFields)
        {
            switch (field)
            {
                case "id" when _id is null:
                    missing.Add("id");
                    break;
                case "name" when string.IsNullOrWhiteSpace(_name):
                    missing.Add("name");
                    break;
                case "map" when string.IsNullOrWhiteSpace(_map):
                    missing.Add("map");
                    break;
            }
        }

        return missing;
    }

    private List<string> BuildIdSuggestions(EntityIndex? index, bool hasConflict)
    {
        if (_entityType is "map" or "npc")
            return new List<string> { "No numeric reservation is persisted. Review naming and map dependencies instead." };

        if (index is null)
            return new List<string> { "Run index --entities for stronger local ID suggestions." };

        var maxId = _entityType.ToLowerInvariant() switch
        {
            "item" or "equipment" => index.Items.Where(i => i.Side == "server").Select(i => i.Id).DefaultIfEmpty(0).Max(),
            "monster" => index.Monsters.Select(m => m.Id).DefaultIfEmpty(0).Max(),
            _ => 0
        };

        if (_id is not null && !hasConflict)
            return [$"ID {_id.Value} is currently unique in the local index. Reservation is not persisted."];

        return new List<string>
        {
            $"Suggested next candidate: {maxId + 1}",
            $"Suggested review window: {maxId + 1}-{maxId + 25}",
            "Suggestions are advisory only and do not reserve IDs."
        };
    }

    private List<string> BuildSafeRanges(EntityIndex? index)
    {
        if (_entityType is "map" or "npc")
            return new List<string> { "Not applicable; review names and dependencies instead." };

        if (index is null)
            return new List<string> { "Unavailable until local index exists." };

        var maxId = _entityType.ToLowerInvariant() switch
        {
            "item" or "equipment" => index.Items.Where(i => i.Side == "server").Select(i => i.Id).DefaultIfEmpty(0).Max(),
            "monster" => index.Monsters.Select(m => m.Id).DefaultIfEmpty(0).Max(),
            _ => 0
        };

        return new List<string> { $"{maxId + 1}-{maxId + 50} (derived from current local maximum; advisory only)" };
    }

    private List<object> FindLocalMatches(EntityIndex? index)
    {
        if (index is null)
            return [];

        return _entityType.ToLowerInvariant() switch
        {
            "item" or "equipment" => index.Items
                .Where(i => (_id is not null && i.Id == _id.Value) || MatchName(i.Name, i.AegisName))
                .Cast<object>()
                .Take(20)
                .ToList(),
            "monster" => index.Monsters
                .Where(m => (_id is not null && m.Id == _id.Value) || MatchName(m.Name, m.AegisName))
                .Cast<object>()
                .Take(20)
                .ToList(),
            "npc" => index.Npcs
                .Where(n => MatchName(n.Name) && (string.IsNullOrWhiteSpace(_map) || n.Map.Contains(_map, StringComparison.OrdinalIgnoreCase)))
                .Cast<object>()
                .Take(20)
                .ToList(),
            "map" => index.Maps
                .Where(m => MatchName(m.Name))
                .Cast<object>()
                .Take(20)
                .ToList(),
            _ => []
        };
    }

    private bool MatchName(params string[] candidates) =>
        !string.IsNullOrWhiteSpace(_name) &&
        candidates.Any(candidate => candidate.Contains(_name, StringComparison.OrdinalIgnoreCase));

    private List<object> BuildDependencyHints()
    {
        return _entityType.ToLowerInvariant() switch
        {
            "item" or "equipment" => new[] { "Validate item_db, itemInfo tables and sprite/resource references." }.Select(text => new { name = "item-client-server-sync", status = "review", reason = text }).Cast<object>().ToList(),
            "monster" => new[] { "Validate mob_db, drops, spawn references and monster sprite assets." }.Select(text => new { name = "monster-sync", status = "review", reason = text }).Cast<object>().ToList(),
            "npc" => new[] { "Validate map existence, coordinates and script naming collisions." }.Select(text => new { name = "npc-map-script", status = "review", reason = text }).Cast<object>().ToList(),
            "map" => new[] { "Validate server registration and client trio (.rsw/.gnd/.gat)." }.Select(text => new { name = "map-client-trio", status = "review", reason = text }).Cast<object>().ToList(),
            _ => new[] { "Validate local dependencies before implementation." }.Select(text => new { name = "generic-review", status = "review", reason = text }).Cast<object>().ToList()
        };
    }

    private List<string> BuildAssetHints() => _entityType.ToLowerInvariant() switch
    {
        "item" or "equipment" => new List<string> { "Do not edit .lub.", "Review client display/resource tables and loose/GRF asset presence only." },
        "monster" => new List<string> { "Review .spr/.act presence only.", "Do not persist extracted assets automatically." },
        "npc" => new List<string> { "Review sprite identifier and map placement only." },
        "map" => new List<string> { "Review .rsw/.gnd/.gat presence only.", "Do not edit GRF/Patch files from this command." },
        _ => new List<string> { "Read-only asset review only." }
    };

    private static object ToLocalSummary(object entity) => entity switch
    {
        ItemEntry item => new { type = "item", item.Id, item.AegisName, item.Name, item.RelativePath, item.Line, item.DbMode },
        MonsterEntry monster => new { type = "monster", monster.Id, monster.AegisName, monster.Name, monster.RelativePath, monster.Line, monster.DbMode },
        NpcEntry npc => new { type = "npc", npc.Name, npc.Map, npc.X, npc.Y, npc.RelativePath, npc.Line },
        MapEntry map => new { type = "map", map.Name, map.RelativePath, map.HasRsw, map.HasGnd, map.HasGat },
        _ => new { type = "unknown" }
    };
}
