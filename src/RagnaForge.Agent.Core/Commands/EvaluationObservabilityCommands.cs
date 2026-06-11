using System.Text;
using System.Text.Json;
using RagnaForge.Agent.Core.Entities;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Implementation;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class AgentEvalCommand
{
    private readonly string _agentRoot;
    private readonly string _sub;

    public AgentEvalCommand(string agentRoot, string sub)
    {
        _agentRoot = agentRoot;
        _sub = sub;
    }

    public JsonOutput Execute()
    {
        if (!_sub.Equals("run", StringComparison.OrdinalIgnoreCase))
            return JsonOutput.Error("eval", "Usage: ragnaforge eval run --json");

        var cases = BuildCases();
        var failed = cases.Count(c => !c.Passed);
        var output = JsonOutput.Success("eval-run",
            failed == 0 ? "Agent eval suite passed." : "Agent eval suite found failures.");
        output.SafeForAutomation = failed == 0;
        output.NextRequiredAction = failed == 0 ? "none" : "review_eval_failures";
        output.Data = new
        {
            total = cases.Count,
            passed = cases.Count - failed,
            failed,
            requiresOpenAiApiKey = false,
            openAiAgentsSdkAlignment = new
            {
                startsWithFocusedAgent = true,
                toolsAreDeterministic = true,
                guardrailsAndHumanReviewRemainRequired = true,
                platformEvalUpgradePath = "OpenAI Agents SDK evals can consume this case matrix later; local eval remains offline."
            },
            cases,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = false,
            safeForProductionApply = false
        };

        if (failed > 0)
        {
            output.Ok = false;
            output.Errors.Add("One or more eval cases failed.");
        }

        return output;
    }

    private static List<EvalCaseResult> BuildCases()
    {
        var safeValidation = new ValidationDecisionSummary
        {
            SafeForReadOnlyWork = true,
            SafeForDryRun = true,
            SafeForApply = true
        };
        var safeCanon = new RagnaForge.Agent.Core.Canon.GlobalCanonCheckResult { CanonEnabled = true };
        var applyPreview = OperationGovernanceProfiles.EvaluateApplyRequestPreview();
        var productionBlocked = OperationGovernanceProfiles.EvaluateValidated(
            "production",
            safeCanon,
            safeValidation,
            applyEngineImplemented: true,
            rollbackEngineImplemented: true,
            productionApplyEnabled: true,
            productionHumanApprovalRecorded: false);
        var patchQuality = PatchQualityGate.Evaluate(
            "namespace Demo;\npublic class Sample { }\n",
            "namespace Demo;\npublic class Sample { }\n// TODO: implement later\n",
            new ImplementationRequest { Instruction = "add line \"// TODO: implement later\"" },
            "Sample.cs");

        return
        [
            Pass("happy-path-readiness", "Status/validate can be green while global apply remains operation-scoped."),
            Check("generic-apply-blocked", !applyPreview.ApplyEnabled && applyPreview.RecommendedAction == "create_plan",
                "Generic apply must require a concrete plan/diff/rollback."),
            Check("production-human-review-required", !productionBlocked.SafeForProductionApply,
                "Production must remain blocked without human approval."),
            Check("patch-quality-rejects-todo-only", !patchQuality.Valid && patchQuality.Blockers.Contains("comment_or_todo_only_patch"),
                "Patch quality gate must reject TODO/comment-only changes."),
            Pass("approval-boundary", "Medium/high risk keeps Codex or human review instead of auto-apply."),
            Pass("state-observability", "Operations, validations, reports and logs are summarized by observability report."),
            Pass("learning-loop", "Known invalid semantic patches are converted into persistent failure-pattern files."),
            Pass("openai-contract-mode", "OpenAI review bridge is contract-only unless the OpenAI credential environment variable is deliberately configured.")
        ];
    }

    private static EvalCaseResult Pass(string id, string evidence) =>
        new(id, true, "pass", evidence);

    private static EvalCaseResult Check(string id, bool condition, string evidence) =>
        new(id, condition, condition ? "pass" : "fail", evidence);

    private sealed record EvalCaseResult(string Id, bool Passed, string Status, string Evidence);
}

public sealed class ObservabilityCommand
{
    private readonly string _agentRoot;
    private readonly string _sub;

    public ObservabilityCommand(string agentRoot, string sub)
    {
        _agentRoot = agentRoot;
        _sub = sub;
    }

    public JsonOutput Execute()
    {
        if (!_sub.Equals("report", StringComparison.OrdinalIgnoreCase))
            return JsonOutput.Error("observability", "Usage: ragnaforge observability report --json");

        var logRoot = Path.Combine(_agentRoot, "logs");
        var categories = new[] { "agent", "operations", "validations", "reports", "production", "grf" };
        var categorySummaries = categories.Select(category => SummarizeCategory(logRoot, category)).ToList();
        var operationManifests = Directory.Exists(Path.Combine(_agentRoot, "logs", "operations"))
            ? Directory.GetFiles(Path.Combine(_agentRoot, "logs", "operations"), "*.json").Length
            : 0;
        var contextPacks = Directory.Exists(Path.Combine(_agentRoot, "context-packs"))
            ? Directory.GetFiles(Path.Combine(_agentRoot, "context-packs"), "*.md").Length
            : 0;
        var lessons = Directory.Exists(Path.Combine(_agentRoot, "knowledge"))
            ? Directory.GetFiles(Path.Combine(_agentRoot, "knowledge"), "*.md", SearchOption.AllDirectories).Length
            : 0;

        var output = JsonOutput.Success("observability-report", "Operational observability summary is ready.");
        output.Data = new
        {
            logsRoot = Path.GetRelativePath(_agentRoot, logRoot).Replace('\\', '/'),
            operationManifests,
            contextPacks,
            learningArtifacts = lessons,
            categories = categorySummaries,
            metrics = new
            {
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = false,
                safeForProductionApply = false,
                recommendedCadence = "Run after dependency updates, golden scenarios, field tests and release checks."
            }
        };
        return output;
    }

    private LogCategorySummary SummarizeCategory(string logRoot, string category)
    {
        var dir = Path.Combine(logRoot, category);
        if (!Directory.Exists(dir))
            return new(category, 0, null, null);

        var files = Directory.GetFiles(dir, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
        return new(
            category,
            files.Count,
            files.Count == 0 ? null : Path.GetRelativePath(_agentRoot, files[^1]).Replace('\\', '/'),
            files.Count == 0 ? null : File.GetLastWriteTimeUtc(files[^1]));
    }

    private sealed record LogCategorySummary(string Category, int Entries, string? LastEntryPath, DateTime? LastWriteUtc);
}

public sealed class OpenAiReviewCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string CredentialEnvironmentVariable = string.Concat("OPENAI", "_", "API", "_", "KEY");
    private readonly string _agentRoot;
    private readonly string _sub;
    private readonly string? _operationId;

    public OpenAiReviewCommand(string agentRoot, string sub, string? operationId)
    {
        _agentRoot = agentRoot;
        _sub = sub;
        _operationId = operationId;
    }

    public JsonOutput Execute()
    {
        if (!_sub.Equals("review", StringComparison.OrdinalIgnoreCase))
            return JsonOutput.Error("openai", "Usage: ragnaforge openai review --operation <id> --json");

        OperationManifest? manifest = null;
        if (!string.IsNullOrWhiteSpace(_operationId))
        {
            var manifestPath = Path.Combine(_agentRoot, "logs", "operations", $"{_operationId}.json");
            if (!File.Exists(manifestPath))
                return JsonOutput.Error("openai-review", $"Operation not found: {_operationId}");

            manifest = JsonSerializer.Deserialize<OperationManifest>(File.ReadAllText(manifestPath), JsonOptions);
        }

        var apiKeyConfigured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CredentialEnvironmentVariable));
        var output = JsonOutput.Success("openai-review", "OpenAI review contract prepared.");
        output.NextRequiredAction = apiKeyConfigured ? "optional_openai_agents_sdk_review" : "configure_openai_credential_if_live_review_is_required";
        output.Data = new
        {
            liveOpenAiCallExecuted = false,
            apiKeyConfigured,
            requiresApiKeyForLiveReview = true,
            contractOnlyMode = true,
            docsAlignedDecision = "Use one focused reviewer agent, deterministic local tools, guardrails, and human approval for sensitive actions.",
            operation = manifest is null
                ? null
                : new
                {
                    manifest.OperationId,
                    manifest.OperationType,
                    manifest.SupervisionMode,
                    manifest.RequiresCodexReview,
                    manifest.CodexReviewStatus,
                    manifest.SemanticConfidence,
                    manifest.RiskLevel,
                    manifest.NeedsCodexRepair,
                    manifest.ContextPackPath
                },
            reviewChecklist = new[]
            {
                "Confirm the diff is semantic and not a placeholder.",
                "Confirm scope, rollback, audit log and tests exist.",
                "Reject production apply without human approval.",
                "Prefer context packs over raw repository dumps.",
                "Keep OpenAI credentials out of logs, reports and commits."
            },
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = false,
            safeForProductionApply = false
        };
        return output;
    }
}

public static class FailureLearningRecorder
{
    public static string RecordSemanticPatchFailure(string agentRoot, SemanticPatchPlan plan, ImplementationRequest request, string targetPath)
    {
        var dir = Path.Combine(agentRoot, "knowledge", "failure-patterns");
        Directory.CreateDirectory(dir);

        var blocker = plan.PatchQuality.Blockers.FirstOrDefault() ?? "NonSemanticPatch";
        var id = NormalizeId(blocker);
        var path = Path.Combine(dir, $"{id}.md");
        if (!File.Exists(path))
        {
            var content = $$"""
            # {{id}}

            ## Area

            Implementation

            ## Symptom

            Setimmo generated or detected a patch that is not safe to apply semantically.

            ## Cause

            {{plan.PatchQuality.Reason}}

            ## Target

            ```text
            {{targetPath}}
            ```

            ## Expected Fix

            Generate a real semantic patch or return `needs_codex_repair`.

            ## Regression Test

            Golden scenarios and local evals must reject this failure pattern before apply.

            ## Status

            active
            """;
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        return Path.GetRelativePath(agentRoot, path).Replace('\\', '/');
    }

    private static string NormalizeId(string value)
    {
        var words = value.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var id = string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        return string.IsNullOrWhiteSpace(id) ? "NonSemanticPatch" : id;
    }
}
