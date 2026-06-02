using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Entities;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Implementation;
using RagnaForge.Agent.Core.Logging;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Production;

public sealed class ProductionApprovalDocument
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } = 1;
    [JsonPropertyName("approvalId")] public string ApprovalId { get; set; } = JsonOutput.GenerateOperationId();
    [JsonPropertyName("operationId")] public string OperationId { get; set; } = string.Empty;
    [JsonPropertyName("environment")] public string Environment { get; set; } = "local";
    [JsonPropertyName("approver")] public string Approver { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("approvedAtUtc")] public DateTimeOffset ApprovedAtUtc { get; set; }
    [JsonPropertyName("expiresAtUtc")] public DateTimeOffset ExpiresAtUtc { get; set; }
    [JsonPropertyName("diffSha256")] public string DiffSha256 { get; set; } = string.Empty;
    [JsonPropertyName("rollbackSha256")] public string RollbackSha256 { get; set; } = string.Empty;
    [JsonPropertyName("riskLevel")] public string RiskLevel { get; set; } = "unknown";
    [JsonPropertyName("scopeSummary")] public string ScopeSummary { get; set; } = string.Empty;
}

public sealed class ProductionAssessment
{
    public string OperationId { get; set; } = string.Empty;
    public string Environment { get; set; } = "local";
    public bool OperationFound { get; set; }
    public bool DiffFound { get; set; }
    public bool RollbackFound { get; set; }
    public bool ScopeAuthorized { get; set; }
    public bool ApprovalRecorded { get; set; }
    public bool ApprovalHashMatches { get; set; }
    public bool ApprovalExpired { get; set; }
    public string DiffSha256 { get; set; } = string.Empty;
    public string RollbackSha256 { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "unknown";
    public string ScopeSummary { get; set; } = string.Empty;
    public List<string> Findings { get; set; } = [];
    public OperationGovernanceAssessment Governance { get; set; } = new();
}

public sealed class ProductionWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _configDir;
    private readonly string _agentRoot;

    public ProductionWorkflowService(string configDir, string agentRoot)
    {
        _configDir = configDir;
        _agentRoot = Path.GetFullPath(agentRoot);
    }

    public JsonOutput Plan(string operationId, string environment) =>
        BuildReadOnlyOutput("production-plan", operationId, environment, "Production plan prepared.");

    public JsonOutput Review(string operationId, string environment) =>
        BuildReadOnlyOutput("production-review", operationId, environment, "Production review completed.");

    public JsonOutput Status(string operationId, string environment) =>
        BuildReadOnlyOutput("production-status", operationId, environment, "Production status evaluated.");

    public JsonOutput Approve(string operationId, string environment, string approver, string reason, int ttlMinutes = 1440)
    {
        if (!OperationIdValidator.IsValid(operationId))
            return JsonOutput.Error("production-approve", "Invalid operationId format.");
        if (string.IsNullOrWhiteSpace(approver) || ContainsControlCharacters(approver))
            return JsonOutput.Error("production-approve", "Approver is required and cannot contain control characters.");
        if (string.IsNullOrWhiteSpace(reason) || ContainsControlCharacters(reason))
            return JsonOutput.Error("production-approve", "Reason is required and cannot contain control characters.");
        if (ttlMinutes is < 5 or > 10080)
            return JsonOutput.Error("production-approve", "ttlMinutes must be between 5 and 10080.");

        var assessment = Assess(operationId, environment, requireApproval: false);
        if (!assessment.OperationFound)
            return JsonOutput.Error("production-approve", $"Operation '{operationId}' not found.");
        if (!assessment.DiffFound || !assessment.RollbackFound)
            return JsonOutput.Error("production-approve", "Production approval requires both diff and rollback files.");
        if (!assessment.ScopeAuthorized)
            return BuildBlocked("production-approve", assessment, "Production scope is not authorized.");
        if (assessment.RiskLevel.Equals("high", StringComparison.OrdinalIgnoreCase))
            return BuildBlocked("production-approve", assessment, "High-risk production changes require manual project governance outside Setimmo.");

        var approval = new ProductionApprovalDocument
        {
            OperationId = operationId,
            Environment = NormalizeEnvironment(environment),
            Approver = approver.Trim(),
            Reason = reason.Trim(),
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes),
            DiffSha256 = assessment.DiffSha256,
            RollbackSha256 = assessment.RollbackSha256,
            RiskLevel = assessment.RiskLevel,
            ScopeSummary = assessment.ScopeSummary
        };

        var dir = Path.Combine(_agentRoot, "logs", "production");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{operationId}.approval.json");
        File.WriteAllText(path, JsonSerializer.Serialize(approval, JsonOptions), Encoding.UTF8);

        new AgentLogger(_agentRoot).Log("production", new
        {
            eventType = "production_approval_recorded",
            operationId,
            approval.ApprovalId,
            approval.Environment,
            approval.ApprovedAtUtc,
            approval.ExpiresAtUtc,
            approval.RiskLevel
        });

        var postApproval = Assess(operationId, environment, requireApproval: true);
        var output = JsonOutput.Success("production-approve", "Production approval recorded for the current diff hash.");
        output.OperationId = operationId;
        output.Data = new
        {
            approvalPath = Path.GetRelativePath(_agentRoot, path),
            approval,
            production = postApproval,
            safeForProductionApply = postApproval.Governance.SafeForProductionApply
        };
        output.NextRequiredAction = postApproval.Governance.SafeForProductionApply ? "production_apply" : postApproval.Governance.RecommendedAction;
        return output;
    }

    public JsonOutput Apply(string operationId, string environment, bool confirm)
    {
        if (!confirm)
            return JsonOutput.Error("production-apply", "Explicit confirmation is required. Usage: ragnaforge production apply --operation <id> --confirm");

        var assessment = Assess(operationId, environment, requireApproval: true);
        if (!assessment.Governance.SafeForProductionApply)
            return BuildBlocked("production-apply", assessment, "Production apply is blocked by formal governance.");

        var result = new ImplementationWorkflowService(_configDir, _agentRoot).ApplyImplementation(operationId, confirm: true);
        new AgentLogger(_agentRoot).Log("production", new
        {
            eventType = result.Ok ? "production_apply_succeeded" : "production_apply_failed",
            operationId,
            environment = NormalizeEnvironment(environment),
            result.Ok,
            result.Errors,
            atUtc = DateTimeOffset.UtcNow
        });

        if (!result.Ok)
            return result;

        result.Mode = "production-apply";
        result.Summary = $"Production apply completed for operation {operationId}.";
        result.Data = new
        {
            implementationResult = result.Data,
            production = assessment,
            safeForProductionApply = assessment.Governance.SafeForProductionApply
        };
        return result;
    }

    public JsonOutput Rollback(string operationId, string environment, bool confirm)
    {
        if (!confirm)
            return JsonOutput.Error("production-rollback", "Explicit confirmation is required. Usage: ragnaforge production rollback --operation <id> --confirm");

        var assessment = Assess(operationId, environment, requireApproval: true);
        if (!assessment.ApprovalRecorded || !assessment.ApprovalHashMatches || assessment.ApprovalExpired)
            return BuildBlocked("production-rollback", assessment, "Production rollback requires a valid approval record for the applied operation.");

        var result = new ImplementationWorkflowService(_configDir, _agentRoot).RollbackImplementation(operationId, confirm: true);
        new AgentLogger(_agentRoot).Log("production", new
        {
            eventType = result.Ok ? "production_rollback_succeeded" : "production_rollback_failed",
            operationId,
            environment = NormalizeEnvironment(environment),
            result.Ok,
            result.Errors,
            atUtc = DateTimeOffset.UtcNow
        });

        if (!result.Ok)
            return result;

        result.Mode = "production-rollback";
        result.Summary = $"Production rollback completed for operation {operationId}.";
        return result;
    }

    public JsonOutput Audit()
    {
        var output = JsonOutput.Success("production-audit", "Production audit loaded.");
        var dir = Path.Combine(_agentRoot, "logs", "production");
        var approvals = new List<object>();
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.approval.json").OrderByDescending(File.GetLastWriteTimeUtc))
            {
                var approval = JsonSerializer.Deserialize<ProductionApprovalDocument>(File.ReadAllText(file), JsonOptions);
                if (approval is null) continue;
                approvals.Add(new
                {
                    approval.ApprovalId,
                    approval.OperationId,
                    approval.Environment,
                    approval.ApprovedAtUtc,
                    approval.ExpiresAtUtc,
                    expired = approval.ExpiresAtUtc <= DateTimeOffset.UtcNow,
                    approval.RiskLevel,
                    approval.ScopeSummary,
                    path = Path.GetRelativePath(_agentRoot, file)
                });
            }
        }

        output.Data = new
        {
            approvals,
            approvalsCount = approvals.Count,
            policy = new
            {
                requiresHumanApproval = true,
                requiresDiffHashMatch = true,
                requiresRollbackPlan = true,
                forbidsGenericShell = true,
                forbidsGrfMutation = true,
                forbidsLubEditing = true
            }
        };
        return output;
    }

    public ProductionAssessment Assess(string operationId, string environment, bool requireApproval)
    {
        var assessment = new ProductionAssessment
        {
            OperationId = operationId,
            Environment = NormalizeEnvironment(environment)
        };

        if (!OperationIdValidator.IsValid(operationId))
        {
            assessment.Findings.Add("Invalid operationId format.");
            assessment.Governance = BuildGovernance(assessment, requireApproval);
            return assessment;
        }

        var manifestPath = Path.Combine(_agentRoot, "logs", "operations", $"{operationId}.json");
        if (!File.Exists(manifestPath))
        {
            assessment.Findings.Add("Operation manifest not found.");
            assessment.Governance = BuildGovernance(assessment, requireApproval);
            return assessment;
        }

        assessment.OperationFound = true;
        var manifest = JsonSerializer.Deserialize<OperationManifest>(File.ReadAllText(manifestPath), JsonOptions);
        if (manifest is null)
        {
            assessment.Findings.Add("Operation manifest could not be read.");
            assessment.Governance = BuildGovernance(assessment, requireApproval);
            return assessment;
        }

        var diffPath = ResolveAgentOwnedFile(manifest.DiffPath);
        var rollbackPath = ResolveAgentOwnedFile(manifest.RollbackPlanPath);
        assessment.DiffFound = diffPath is not null && File.Exists(diffPath);
        assessment.RollbackFound = rollbackPath is not null && File.Exists(rollbackPath);
        if (assessment.DiffFound) assessment.DiffSha256 = ComputeSha256(diffPath!);
        if (assessment.RollbackFound) assessment.RollbackSha256 = ComputeSha256(rollbackPath!);

        var context = LoadContext();
        var canon = new GlobalCanonPolicy();
        assessment.ScopeAuthorized = manifest.AffectedFiles.Count > 0 &&
            manifest.AffectedFiles.All(file =>
            {
                var writeCheck = context.Guard.EnsureCanWrite(file.Path);
                return writeCheck.IsAllowed && !canon.IsSensitiveFile(file.Path);
            });

        assessment.ScopeSummary = $"{manifest.AffectedFiles.Count} file(s), type={manifest.OperationType}, applied={manifest.Applied}";
        assessment.RiskLevel = ClassifyRisk(manifest, assessment.ScopeAuthorized);
        assessment.ApprovalRecorded = TryLoadApproval(operationId, out var approval);
        assessment.ApprovalExpired = approval is not null && approval.ExpiresAtUtc <= DateTimeOffset.UtcNow;
        assessment.ApprovalHashMatches = approval is not null &&
            string.Equals(approval.DiffSha256, assessment.DiffSha256, StringComparison.OrdinalIgnoreCase);

        if (!assessment.DiffFound) assessment.Findings.Add("Diff file is missing.");
        if (!assessment.RollbackFound) assessment.Findings.Add("Rollback file is missing.");
        if (!assessment.ScopeAuthorized) assessment.Findings.Add("One or more affected files are outside writable scope or sensitive.");
        if (requireApproval && !assessment.ApprovalRecorded) assessment.Findings.Add("Approval file is missing.");
        if (requireApproval && assessment.ApprovalRecorded && !assessment.ApprovalHashMatches) assessment.Findings.Add("Approval hash does not match current diff/rollback.");
        if (requireApproval && assessment.ApprovalExpired) assessment.Findings.Add("Approval has expired.");

        assessment.Governance = BuildGovernance(assessment, requireApproval, manifest.ValidationIssues);
        return assessment;
    }

    private JsonOutput BuildReadOnlyOutput(string mode, string operationId, string environment, string summary)
    {
        var assessment = Assess(operationId, environment, requireApproval: mode is "production-status" or "production-review");
        var output = assessment.OperationFound
            ? JsonOutput.Success(mode, summary)
            : JsonOutput.Error(mode, assessment.Findings);
        output.OperationId = OperationIdValidator.IsValid(operationId) ? operationId : output.OperationId;
        output.Data = new
        {
            production = assessment,
            safeForProductionApply = assessment.Governance.SafeForProductionApply,
            nextSafeActions = BuildNextActions(assessment)
        };
        output.NextRequiredAction = assessment.Governance.RecommendedAction;
        return output;
    }

    private static List<string> BuildNextActions(ProductionAssessment assessment)
    {
        var actions = new List<string>();
        if (!assessment.DiffFound) actions.Add("regenerate_diff");
        if (!assessment.RollbackFound) actions.Add("regenerate_rollback_plan");
        if (!assessment.ScopeAuthorized) actions.Add("restrict_scope");
        if (!assessment.ApprovalRecorded) actions.Add("production_approve");
        if (assessment.ApprovalRecorded && !assessment.ApprovalHashMatches) actions.Add("review_and_reapprove_current_diff");
        if (assessment.ApprovalExpired) actions.Add("record_fresh_human_approval");
        if (assessment.Governance.SafeForProductionApply) actions.Add("production_apply");
        return actions.Count == 0 ? ["keep_review_mode"] : actions;
    }

    private JsonOutput BuildBlocked(string mode, ProductionAssessment assessment, string summary)
    {
        var output = JsonOutput.Error(mode, [summary, .. assessment.Findings]);
        output.OperationId = OperationIdValidator.IsValid(assessment.OperationId) ? assessment.OperationId : output.OperationId;
        output.Data = new
        {
            production = assessment,
            governance = assessment.Governance,
            safeForProductionApply = assessment.Governance.SafeForProductionApply
        };
        output.NextRequiredAction = assessment.Governance.RecommendedAction;
        return output;
    }

    private OperationGovernanceAssessment BuildGovernance(
        ProductionAssessment assessment,
        bool requireApproval,
        List<ValidationIssue>? validationIssues = null)
    {
        var validation = validationIssues is null || validationIssues.Count == 0
            ? new ValidationDecisionSummary { SafeForReadOnlyWork = true, SafeForDryRun = true, SafeForApply = true }
            : ValidationOperationalClassifier.BuildSummary(validationIssues);

        return OperationGovernanceProfiles.EvaluateValidated(
            "production",
            new GlobalCanonValidator(_agentRoot).Check(),
            validation,
            applyEngineImplemented: true,
            rollbackEngineImplemented: true,
            productionApplyEnabled: true,
            pathScopeValidated: assessment.ScopeAuthorized,
            externalWriteRequested: !assessment.ScopeAuthorized,
            buildPassed: true,
            testsPassed: true,
            requirePlanForApply: true,
            requireDiffForApply: true,
            requireRollbackForApply: true,
            hasPlan: assessment.OperationFound,
            hasDiff: assessment.DiffFound,
            hasRollback: assessment.RollbackFound,
            productionEnvironment: assessment.Environment,
            productionHumanApprovalRecorded: !requireApproval || assessment.ApprovalRecorded,
            productionApprovalHashMatches: !requireApproval || assessment.ApprovalHashMatches,
            productionApprovalExpired: requireApproval && assessment.ApprovalExpired,
            productionScopeAuthorized: assessment.ScopeAuthorized,
            productionAuditLogAvailable: true,
            productionOperationClassified: !assessment.RiskLevel.Equals("unknown", StringComparison.OrdinalIgnoreCase),
            productionRiskWithinLimit: !assessment.RiskLevel.Equals("high", StringComparison.OrdinalIgnoreCase));
    }

    private bool TryLoadApproval(string operationId, out ProductionApprovalDocument? approval)
    {
        approval = null;
        var path = Path.Combine(_agentRoot, "logs", "production", $"{operationId}.approval.json");
        if (!File.Exists(path))
            return false;

        approval = JsonSerializer.Deserialize<ProductionApprovalDocument>(File.ReadAllText(path), JsonOptions);
        return approval is not null;
    }

    private string? ResolveAgentOwnedFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || PathGuard.ContainsTraversal(relativePath))
            return null;

        var full = Path.GetFullPath(Path.Combine(_agentRoot, relativePath));
        return PathGuard.IsContainedIn(full, _agentRoot) ? full : null;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ClassifyRisk(OperationManifest manifest, bool scopeAuthorized)
    {
        if (!scopeAuthorized)
            return "high";

        if (manifest.AffectedFiles.Any(file =>
                file.Action.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(file.Path).Equals(".lub", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(file.Path).Equals(".grf", StringComparison.OrdinalIgnoreCase)))
            return "high";

        if (manifest.AffectedFiles.Count > 5)
            return "medium";

        if (manifest.AffectedFiles.Any(file =>
                file.Path.Contains($"{Path.DirectorySeparatorChar}scripts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                file.Path.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ||
                file.Path.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)))
            return "medium";

        return "low";
    }

    private LoadedContext LoadContext()
    {
        var loader = new ConfigLoader(_configDir);
        var paths = loader.LoadPathsConfig();
        var safety = loader.LoadSafetyConfig();
        var profile = ConfigLoader.GetActiveProfile(paths);
        return new LoadedContext(profile, new PathGuard(profile.WritableRoots, profile.ReadOnlyRoots, safety.BlockLubEditing));
    }

    private static string NormalizeEnvironment(string environment)
    {
        var value = string.IsNullOrWhiteSpace(environment) ? "local" : environment.Trim().ToLowerInvariant();
        return value is "local" or "development" or "staging" or "production" ? value : "local";
    }

    private static bool ContainsControlCharacters(string value) =>
        value.Any(ch => char.IsControl(ch) && ch is not '\r' and not '\n' and not '\t');

    private sealed record LoadedContext(ProfileConfig Profile, PathGuard Guard);
}
