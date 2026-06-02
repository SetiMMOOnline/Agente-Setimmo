using System.Text;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Entities;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Knowledge;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Scanning;

namespace RagnaForge.Agent.Core.Commands;

internal sealed class ExternalIssueView
{
    public string Id { get; set; } = string.Empty;
    public string OriginalSeverity { get; set; } = string.Empty;
    public string NormalizedSeverity { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string RiskLevel { get; set; } = "low";
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public bool HumanReviewRecommended { get; set; } = true;
    public bool BlocksReadOnly { get; set; }
    public bool BlocksDryRun { get; set; }
    public bool BlocksApply { get; set; }
    public string ReasonNotBlocking { get; set; } = string.Empty;
    public string NextSafeAction { get; set; } = string.Empty;
}

public sealed class TriageCommand
{
    private readonly string _configDir;
    private readonly string _agentRoot;
    private readonly bool _externalOnly;
    private readonly string _format;

    public TriageCommand(string configDir, string agentRoot, bool externalOnly = true, string format = "json")
    {
        _configDir = configDir;
        _agentRoot = agentRoot;
        _externalOnly = externalOnly;
        _format = format;
    }

    public JsonOutput Execute()
    {
        var output = JsonOutput.Success("triage");
        try
        {
            var loader = new ConfigLoader(_configDir);
            var pathsConfig = loader.LoadPathsConfig();
            var safetyConfig = loader.LoadSafetyConfig();
            var fingerprint = ConfigFingerprint.Generate(pathsConfig, safetyConfig);

            output.ActiveProfile = pathsConfig.ActiveProfile;
            output.ConfigFingerprint = fingerprint;

            var cacheInspection = AgentCacheInspector.InspectEntityIndex(
                _agentRoot,
                pathsConfig.ActiveProfile,
                fingerprint);

            if (cacheInspection.Document is null)
            {
                output = JsonOutput.Error("triage", "Entity index not found or obsolete. Run 'ragnaforge index --entities --json' first.");
                output.NextRequiredAction = "run_index";
                return output;
            }

            var index = cacheInspection.Document;
            var issues = new List<ValidationIssue>();
            issues.AddRange(ValidateCommand.ValidateItems(index));
            issues.AddRange(ValidateCommand.ValidateMonsters(index));
            issues.AddRange(ValidateCommand.ValidateNpcs(index));
            issues.AddRange(ValidateCommand.ValidateMaps(index));
            ValidationOperationalClassifier.ApplyClassification(issues);

            if (_externalOnly)
                issues = issues.Where(i => string.Equals(i.Scope, "external-data", StringComparison.OrdinalIgnoreCase)).ToList();

            var decisionSummary = ValidationOperationalClassifier.BuildSummary(issues);
            var canon = new GlobalCanonValidator(_agentRoot).Check();
            var governance = OperationGovernanceProfiles.EvaluateValidated(
                "triage",
                canon,
                decisionSummary,
                applyEngineImplemented: true,
                rollbackEngineImplemented: true);
            var normalized = issues.Select(ToExternalIssueView).ToList();
            var knowledgeContext = new KnowledgeContextService(_agentRoot);
            var knowledgeService = new KnowledgeService(_agentRoot);
            var liveLookupDecision = new
            {
                liveLookup = false,
                decisionReason = "Triage is broad analysis; live reference lookup is skipped by anti-bulk policy.",
                requestCount = 0,
                timeoutMs = 3000,
                rateLimitApplied = true,
                linksFollowed = false,
                bulkLookup = false,
                rangeLookup = false,
                rawHtmlStored = false,
                dumpStored = false,
                cacheMode = "none",
                warning = "live lookup unavailable by policy during broad triage.",
                safeForApply = governance.SafeForApply
            };

            var data = new
            {
                totalIssues = normalized.Count,
                totalErrors = normalized.Count(i => i.NormalizedSeverity is "error" or "critical"),
                totalWarnings = normalized.Count(i => i.NormalizedSeverity == "warning"),
                byCategory = normalized.GroupBy(i => i.Category).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
                byRisk = normalized.GroupBy(i => i.RiskLevel).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
                byEntityType = normalized.GroupBy(i => i.EntityType).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase),
                topCritical = normalized.Where(i => i.RiskLevel == "critical").Take(25).ToList(),
                topHigh = normalized.Where(i => i.RiskLevel == "high").Take(25).ToList(),
                duplicateItems = normalized.Where(i => i.Category == "DuplicateItem").ToList(),
                missingAssets = normalized.Where(i => i.Category == "MissingClientAsset").Take(100).ToList(),
                episodeDependencies = normalized.Where(i => i.Category == "FutureEpisodeDependency").ToList(),
                customOverrideCandidates = normalized.Where(i => i.Category == "CustomOverrideCandidate").ToList(),
                issues = normalized,
                knowledge = new
                {
                    conflicts = knowledgeContext.BuildConflictsReport().Take(25).ToList(),
                    coverage = knowledgeContext.BuildCoverage(),
                    sourceFreshness = knowledgeService.BuildSourceAssessments(),
                    learningCandidates = knowledgeService.LoadLearningCandidates()
                },
                liveLookupDecisions = new[] { liveLookupDecision },
                nextSafeActions = BuildNextSafeActions(normalized),
                safeForReadOnlyWork = governance.SafeForReadOnlyWork,
                safeForDryRun = governance.SafeForDryRun,
                safeForApply = governance.SafeForApply,
                safeForProductionApply = governance.SafeForProductionApply,
                governance,
                markdown = _format.Equals("md", StringComparison.OrdinalIgnoreCase) ? GenerateMarkdown(normalized, decisionSummary) : null
            };

            output.Summary = $"Triage completed - {normalized.Count} issue(s) analyzed.";
            output.SafeForAutomation = governance.SafeForReadOnlyWork;
            output.Data = data;
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("triage", ex.Message);
        }

        return output;
    }

    private static ExternalIssueView ToExternalIssueView(ValidationIssue issue)
    {
        var category = ClassifyCategory(issue);
        var risk = ClassifyRisk(issue, category);
        return new ExternalIssueView
        {
            Id = $"{issue.Code}:{issue.EntityType}:{issue.EntityId ?? issue.EntityName ?? "unknown"}",
            OriginalSeverity = issue.Severity,
            NormalizedSeverity = issue.Severity,
            Category = category,
            RiskLevel = risk,
            EntityType = string.IsNullOrWhiteSpace(issue.EntityType) ? "unknown" : issue.EntityType,
            EntityId = issue.EntityId,
            EntityName = issue.EntityName,
            Source = string.IsNullOrWhiteSpace(issue.Scope) ? "unknown" : issue.Scope,
            Evidence = BuildEvidence(issue),
            SuggestedAction = issue.Recommendation,
            HumanReviewRecommended = true,
            BlocksReadOnly = issue.BlockingFor.Contains("read-only-audit", StringComparer.OrdinalIgnoreCase),
            BlocksDryRun = issue.BlockingFor.Contains("dry-run", StringComparer.OrdinalIgnoreCase),
            BlocksApply = issue.BlockingFor.Contains("apply", StringComparer.OrdinalIgnoreCase) || issue.Severity is "error" or "critical",
            ReasonNotBlocking = BuildReasonNotBlocking(issue, category),
            NextSafeAction = BuildNextSafeAction(issue, category)
        };
    }

    private static string BuildEvidence(ValidationIssue issue)
    {
        var source = string.IsNullOrWhiteSpace(issue.SourceFile) ? "local index" : issue.SourceFile;
        return issue.Line is > 0
            ? $"{source}:{issue.Line} - {issue.Message}"
            : $"{source} - {issue.Message}";
    }

    private static string BuildReasonNotBlocking(ValidationIssue issue, string category)
    {
        if (issue.BlockingFor.Contains("read-only-audit", StringComparer.OrdinalIgnoreCase))
            return "Local evidence requires explicit review before broad automation.";

        return category switch
        {
            "MissingClientAsset" => "Client asset issues block apply safety but remain read-only and dry-run friendly.",
            "ExternalReferenceOnly" => "Reference context alone never blocks local workflow.",
            _ => "Read-only review stays available; safeForApply remains false by governance."
        };
    }

    private static string BuildNextSafeAction(ValidationIssue issue, string category) => category switch
    {
        "MissingClientAsset" => "Review patch path or add the missing client files before any controlled apply workflow.",
        "DuplicateItem" => "Review the local database entries and confirm whether the duplicate is real or a parser/data issue.",
        "FutureEpisodeDependency" => "Review episode gating and custom/progressive policy before implementation.",
        _ => string.IsNullOrWhiteSpace(issue.Recommendation) ? "Review manually." : issue.Recommendation
    };

    private static string ClassifyCategory(ValidationIssue issue)
    {
        return (issue.Code ?? string.Empty).ToUpperInvariant() switch
        {
            "ITEM_DUPLICATE_ID_SERVER" => "DuplicateItem",
            "ITEM_DUPLICATE_ID_CROSS_DB_MODE" => "CustomOverrideCandidate",
            "MOB_DUPLICATE_ID" => "DuplicateMonster",
            "MAP_NO_CLIENT_FILES" => "MissingClientAsset",
            "MAP_INCOMPLETE_CLIENT" => "MissingClientAsset",
            "NPC_NO_MAP" => "MissingMap",
            "ITEM_MISSING_AEGIS" => "DataQuality",
            _ => "Other"
        };
    }

    private static string ClassifyRisk(ValidationIssue issue, string category)
    {
        if (issue.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase))
            return "critical";

        if (category == "DuplicateItem" || category == "DuplicateMonster")
            return "critical";

        if (category == "MissingClientAsset")
            return "high";

        if (category is "CustomOverrideCandidate" or "FutureEpisodeDependency")
            return "medium";

        return issue.Severity.Equals("error", StringComparison.OrdinalIgnoreCase) ? "high" : "low";
    }

    private static List<string> BuildNextSafeActions(List<ExternalIssueView> issues)
    {
        var actions = new List<string>();
        if (issues.Any(i => i.Category == "DuplicateItem"))
            actions.Add("Review duplicate item IDs first because they can keep apply safety blocked.");
        if (issues.Any(i => i.Category == "MissingClientAsset"))
            actions.Add("Review missing client map assets or accept them as known external-data warnings.");
        actions.Add("Use targeted find --with-knowledge for a specific entity before planning changes.");
        actions.Add("Do not run broad live reference lookup; anti-bulk policy keeps it disabled here.");
        return actions;
    }

    private static string GenerateMarkdown(List<ExternalIssueView> issues, ValidationDecisionSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# External Data Triage Report");
        sb.AppendLine();
        sb.AppendLine("## 1. Resumo executivo");
        sb.AppendLine($"- Total de issues: {issues.Count}");
        sb.AppendLine($"- Errors/critical: {issues.Count(i => i.NormalizedSeverity is "error" or "critical")}");
        sb.AppendLine($"- Warnings: {issues.Count(i => i.NormalizedSeverity == "warning")}");
        sb.AppendLine($"- Safe for read-only work: {summary.SafeForReadOnlyWork}");
        sb.AppendLine($"- Safe for dry-run: {summary.SafeForDryRun}");
        sb.AppendLine($"- Safe for apply: {summary.SafeForApply}");
        sb.AppendLine();
        sb.AppendLine("## 2. Categorias");
        foreach (var category in issues.GroupBy(i => i.Category).OrderByDescending(g => g.Count()))
            sb.AppendLine($"- {category.Key}: {category.Count()}");
        sb.AppendLine();
        sb.AppendLine("## 3. Riscos");
        foreach (var risk in issues.GroupBy(i => i.RiskLevel).OrderByDescending(g => g.Count()))
            sb.AppendLine($"- {risk.Key}: {risk.Count()}");
        sb.AppendLine();
        sb.AppendLine("## 4. Principais achados");
        foreach (var issue in issues.Take(25))
            sb.AppendLine($"- [{issue.Category}] {issue.EntityType} {issue.EntityId ?? issue.EntityName ?? "unknown"}: {issue.Evidence}");
        sb.AppendLine();
        sb.AppendLine("## 5. Politica de referencia externa");
        sb.AppendLine("- No crawler");
        sb.AppendLine("- No follow links");
        sb.AppendLine("- No forum pagination");
        sb.AppendLine("- No bulk lookup");
        sb.AppendLine("- No raw HTML");
        sb.AppendLine("- No dump");
        sb.AppendLine($"- SafeForApply={summary.SafeForApply}");
        return sb.ToString();
    }
}
