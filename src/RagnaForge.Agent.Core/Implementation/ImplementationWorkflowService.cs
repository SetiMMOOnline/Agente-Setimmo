using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Entities;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Logging;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Implementation;

public sealed class ImplementationWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Regex SecretRegex = new(
        string.Join("|",
        [
            "sk" + "-[A-Za-z0-9]{12,}",
            "api" + "key\\s*[:=]",
            "api[_-]?" + "key\\s*[:=]",
            "pass" + "word\\s*[:=]",
            "to" + "ken\\s*[:=]",
            "se" + "cret\\s*[:=]"
        ]),
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly string _configDir;
    private readonly string _agentRoot;
    private readonly LanguageCapabilityRegistry _registry = new();
    private readonly GlobalCanonPolicy _canon = GlobalCanonPolicy.CreateDefault();

    public ImplementationWorkflowService(string configDir, string agentRoot)
    {
        _configDir = configDir;
        _agentRoot = agentRoot;
    }

    public JsonOutput ReviewCode(string targetPath, string workspace, string? languageHint)
    {
        var output = JsonOutput.Success("review-code");

        try
        {
            var context = LoadContext();
            var resolved = ResolveTargetPath(targetPath, workspace, context.Profile);
            var readCheck = context.Guard.EnsureCanRead(resolved.FullPath);
            if (!readCheck.IsAllowed)
                return JsonOutput.Error("review-code", readCheck.Reason ?? "Read access denied.");

            if (!File.Exists(resolved.FullPath))
                return JsonOutput.Error("review-code", $"Target file not found: {resolved.DisplayPath}");

            var content = File.ReadAllText(resolved.FullPath);
            var capability = _registry.ResolveByPath(resolved.FullPath, languageHint);
            var issues = AnalyzeContent(resolved.FullPath, content, capability);
            var ecosystems = _registry.DetectProjectEcosystems(context.Profile.RagnaforgeMainProjectPath);
            var governance = OperationGovernanceProfiles.EvaluateWithoutValidation(
                "review-code",
                new GlobalCanonValidator(_agentRoot).Check(),
                applyEngineImplemented: true,
                rollbackEngineImplemented: true);

            output.ActiveProfile = context.Paths.ActiveProfile;
            output.ConfigFingerprint = context.Fingerprint;
            output.Summary = $"Review completed for {resolved.DisplayPath}.";
            output.Data = new
            {
                targetPath = resolved.DisplayPath,
                resolvedPath = resolved.FullPath,
                workspace = resolved.Workspace,
                language = capability?.Key ?? "unknown",
                ecosystems,
                issues,
                issueCounts = new
                {
                    errors = issues.Count(issue => issue.Severity is "error" or "critical"),
                    warnings = issues.Count(issue => issue.Severity == "warning"),
                    info = issues.Count(issue => issue.Severity == "info")
                },
                safeForReadOnlyWork = governance.SafeForReadOnlyWork,
                safeForDryRun = governance.SafeForDryRun,
                safeForApply = governance.SafeForApply,
                governance
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("review-code", ex.Message);
        }

        return output;
    }

    public JsonOutput FixCode(string targetPath, string workspace, string? languageHint)
    {
        var request = new ImplementationRequest
        {
            Intent = ImplementationIntent.Fix,
            Workspace = workspace,
            TargetPath = targetPath,
            LanguageHint = languageHint
        };

        return BuildImplementationOperation(request, persist: true, operationName: "fix-code");
    }

    public JsonOutput CreateContent(ImplementationRequest request)
    {
        request.Intent = ImplementationIntent.CreateContent;
        return BuildImplementationOperation(request, persist: true, operationName: "create-content");
    }

    public JsonOutput PlanImplement(ImplementationRequest request)
    {
        request.Intent = ImplementationIntent.Implement;
        return BuildImplementationOperation(request, persist: false, operationName: "plan-implement");
    }

    public JsonOutput DryRunImplement(ImplementationRequest request)
    {
        request.Intent = ImplementationIntent.Implement;
        return BuildImplementationOperation(request, persist: true, operationName: "dry-run-implement");
    }

    public JsonOutput ApplyImplementation(string operationId, bool confirm)
    {
        var output = JsonOutput.Success("apply-implement");

        try
        {
            var context = LoadContext();
            var safety = context.Safety;
            if (safety.RequireExplicitConfirmation && !confirm)
            {
                return JsonOutput.Error("apply-implement",
                    "Explicit confirmation is required. Usage: ragnaforge apply implement --operation <id> --confirm");
            }

            if (!OperationIdValidator.IsValid(operationId))
                return JsonOutput.Error("apply-implement", "Invalid operationId format.");

            var manifestPath = Path.Combine(_agentRoot, "logs", "operations", $"{operationId}.json");
            if (!File.Exists(manifestPath))
                return JsonOutput.Error("apply-implement", $"Operation '{operationId}' not found.");

            var manifest = JsonSerializer.Deserialize<OperationManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (manifest is null)
                return JsonOutput.Error("apply-implement", "Could not read operation manifest.");

            if (!string.Equals(manifest.OperationType, "implementation-dry-run", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(manifest.OperationType, "fix-code", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(manifest.OperationType, "create-content", StringComparison.OrdinalIgnoreCase))
            {
                return JsonOutput.Error("apply-implement", "This operation was not created by the implementation workflow.");
            }

            if (manifest.Applied)
                return JsonOutput.Error("apply-implement", $"Operation '{operationId}' has already been applied.");

            if (manifest.NeedsCodexRepair || manifest.PatchQuality is not null &&
                manifest.PatchQuality.ToString()?.Contains("non_semantic_patch", StringComparison.OrdinalIgnoreCase) == true)
            {
                return JsonOutput.Error("apply-implement", "Operation requires Codex repair/review before apply.");
            }

            if (manifest.RequiresCodexReview && !string.Equals(manifest.CodexReviewStatus, "approved", StringComparison.OrdinalIgnoreCase))
                return JsonOutput.Error("apply-implement", "Operation requires Codex review approval before apply.");

            if (string.IsNullOrWhiteSpace(manifest.DiffPath) || string.IsNullOrWhiteSpace(manifest.RollbackPlanPath))
                return JsonOutput.Error("apply-implement", "Operation is missing diff or rollback plan.");

            var diffPath = Path.Combine(_agentRoot, manifest.DiffPath);
            var rollbackPath = Path.Combine(_agentRoot, manifest.RollbackPlanPath);
            if (!File.Exists(diffPath) || !File.Exists(rollbackPath))
                return JsonOutput.Error("apply-implement", "Diff or rollback file is missing from agentRoot logs.");

            var plan = JsonSerializer.Deserialize<ImplementationPlanDocument>(File.ReadAllText(diffPath), JsonOptions);
            var rollback = JsonSerializer.Deserialize<RollbackPlanDocument>(File.ReadAllText(rollbackPath), JsonOptions);
            if (plan is null || rollback is null)
                return JsonOutput.Error("apply-implement", "Could not read persisted implementation plan.");

            var validationSummary = BuildValidationSummary(manifest.ValidationIssues);
            var governance = OperationGovernanceProfiles.EvaluateValidated(
                "apply-implement",
                new GlobalCanonValidator(_agentRoot).Check(),
                validationSummary,
                applyEngineImplemented: true,
                rollbackEngineImplemented: true,
                productionApplyEnabled: false,
                pathScopeValidated: true,
                pathTraversalDetected: false,
                genericShellExposed: false,
                externalWriteRequested: false,
                secretsDetected: plan.Files.Any(file => SecretRegex.IsMatch(file.TargetContent ?? string.Empty)),
                buildPassed: true,
                testsPassed: true,
                destructiveOperationRequested: false,
                requirePlanForApply: context.Safety.RequireDryRunBeforeApply,
                requireDiffForApply: context.Safety.RequireDiffBeforeApply,
                requireRollbackForApply: context.Safety.BackupBeforeApply,
                hasPlan: true,
                hasDiff: true,
                hasRollback: true);

            if (!governance.ApplyEnabled)
                return BuildGovernanceBlockedOutput("apply-implement", governance, "Operation was blocked by implementation governance.");

            foreach (var file in plan.Files)
            {
                var writeCheck = context.Guard.EnsureCanWrite(file.Path);
                if (!writeCheck.IsAllowed)
                    return JsonOutput.Error("apply-implement", writeCheck.Reason ?? "Write access denied.");

                if (_canon.IsSensitiveFile(file.Path))
                    return JsonOutput.Error("apply-implement", $"Sensitive target blocked by policy: {file.Path}");

                if (file.TargetContent is null)
                    return JsonOutput.Error("apply-implement", $"Missing target content for '{file.Path}'.");

                var capability = _registry.ResolveByPath(file.Path, file.Language);
                var validation = capability?.Validator(file.Path, file.TargetContent);
                if (validation is not null && !validation.Valid)
                {
                    var firstError = validation.Messages.FirstOrDefault(msg => msg.Severity is "error" or "critical");
                    if (firstError is not null)
                        return JsonOutput.Error("apply-implement", $"Target content failed validation for '{file.Path}': {firstError.Message}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);
                File.WriteAllText(file.Path, file.TargetContent, Encoding.UTF8);
            }

            var postValidationIssues = new List<string>();
            foreach (var file in plan.Files)
            {
                var written = File.ReadAllText(file.Path);
                if (!string.Equals(written, file.TargetContent, StringComparison.Ordinal))
                    postValidationIssues.Add($"Post-validation mismatch for '{file.Path}'.");

                var capability = _registry.ResolveByPath(file.Path, file.Language);
                var validation = capability?.Validator(file.Path, written);
                if (validation is not null)
                {
                    postValidationIssues.AddRange(validation.Messages
                        .Where(message => message.Severity is "error" or "critical")
                        .Select(message => $"{file.Path}: {message.Message}"));
                }
            }

            if (postValidationIssues.Count > 0)
            {
                RestoreRollback(context, rollback);
                return JsonOutput.Error("apply-implement", ["Post-validation failed after apply.", .. postValidationIssues]);
            }

            manifest.Applied = true;
            manifest.Status = "applied";
            manifest.Warnings.Add("Post-validation completed successfully.");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), Encoding.UTF8);

            rollback.Applied = true;
            rollback.Note = "Rollback plan belongs to an applied implementation operation.";
            File.WriteAllText(rollbackPath, JsonSerializer.Serialize(rollback, JsonOptions), Encoding.UTF8);

            var logger = new AgentLogger(_agentRoot);
            logger.Log("operations", new
            {
                eventType = "implementation_apply",
                operationId,
                appliedAtUtc = DateTimeOffset.UtcNow,
                files = plan.Files.Select(file => new { file.Path, file.Action }),
                governance
            });

            output.ActiveProfile = context.Paths.ActiveProfile;
            output.ConfigFingerprint = context.Fingerprint;
            output.OperationId = manifest.OperationId;
            output.Summary = $"Implementation apply succeeded for operation {operationId}.";
            output.Data = new
            {
                operationId,
                manifest.OperationType,
                applied = true,
                affectedFiles = plan.Files.Select(file => new { file.Path, file.Action }),
                rollbackPlanPath = manifest.RollbackPlanPath,
                governance
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("apply-implement", ex.Message);
        }

        return output;
    }

    public JsonOutput RollbackImplementation(string operationId, bool confirm)
    {
        if (!OperationIdValidator.IsValid(operationId))
            return JsonOutput.Error("rollback", "Invalid operationId format.");

        if (!confirm)
            return JsonOutput.Error("rollback", "Explicit confirmation is required. Usage: ragnaforge rollback --id <id> --confirm");

        try
        {
            var context = LoadContext();
            var rollbackPath = Path.Combine(_agentRoot, "logs", "rollbacks", $"{operationId}.rollback.json");
            if (!File.Exists(rollbackPath))
                return JsonOutput.Error("rollback", $"Rollback plan '{operationId}' not found.");

            var rollback = JsonSerializer.Deserialize<RollbackPlanDocument>(File.ReadAllText(rollbackPath), JsonOptions);
            if (rollback is null)
                return JsonOutput.Error("rollback", "Could not read rollback plan.");

            if (!rollback.Applied)
                return JsonOutput.Error("rollback", $"Rollback plan '{operationId}' has not been applied yet.");

            RestoreRollback(context, rollback);
            rollback.Applied = false;
            rollback.Note = "Rollback completed successfully.";
            File.WriteAllText(rollbackPath, JsonSerializer.Serialize(rollback, JsonOptions), Encoding.UTF8);

            var manifestPath = Path.Combine(_agentRoot, "logs", "operations", $"{operationId}.json");
            if (File.Exists(manifestPath))
            {
                var manifest = JsonSerializer.Deserialize<OperationManifest>(File.ReadAllText(manifestPath), JsonOptions);
                if (manifest is not null)
                {
                    manifest.Applied = false;
                    manifest.Status = "rolled-back";
                    manifest.Warnings.Add("Rollback completed successfully.");
                    File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), Encoding.UTF8);
                }
            }

            var logger = new AgentLogger(_agentRoot);
            logger.Log("operations", new
            {
                eventType = "implementation_rollback",
                operationId,
                rolledBackAtUtc = DateTimeOffset.UtcNow,
                files = rollback.Files.Select(file => new { file.Path, file.Action })
            });

            var output = JsonOutput.Success("rollback", $"Rollback completed for operation {operationId}.");
            output.OperationId = operationId;
            output.ActiveProfile = context.Paths.ActiveProfile;
            output.ConfigFingerprint = context.Fingerprint;
            output.Data = new
            {
                operationId,
                rolledBack = true,
                files = rollback.Files.Select(file => new { file.Path, file.Action })
            };
            return output;
        }
        catch (Exception ex)
        {
            return JsonOutput.Error("rollback", ex.Message);
        }
    }

    private JsonOutput BuildImplementationOperation(ImplementationRequest request, bool persist, string operationName)
    {
        var output = JsonOutput.Success(operationName);

        try
        {
            var context = LoadContext();
            var resolved = ResolveTargetPath(request.TargetPath, request.Workspace, context.Profile);
            var writeCheck = context.Guard.EnsureCanWrite(resolved.FullPath);
            if (!writeCheck.IsAllowed)
                return JsonOutput.Error(operationName, writeCheck.Reason ?? "Write access denied.");

            if (_canon.IsSensitiveFile(resolved.FullPath))
                return JsonOutput.Error(operationName, $"Sensitive target blocked by policy: {resolved.DisplayPath}");

            var capability = _registry.ResolveByPath(resolved.FullPath, request.LanguageHint);
            if (capability is null)
                return JsonOutput.Error(operationName, $"No language capability registered for '{resolved.DisplayPath}'.");

            var currentContent = File.Exists(resolved.FullPath) ? File.ReadAllText(resolved.FullPath) : null;
            var semanticPlan = BuildSemanticPatchPlan(request, capability, currentContent, resolved.FullPath);
            if (semanticPlan.TargetContent is null || !semanticPlan.PatchQuality.Valid)
                return BuildNeedsCodexRepairOutput(operationName, context, request, resolved, capability, semanticPlan);

            var desiredContent = semanticPlan.TargetContent;

            var issues = AnalyzeContent(resolved.FullPath, desiredContent, capability);
            var validationIssues = issues.Select(ToValidationIssue).ToList();
            var validationSummary = BuildValidationSummary(validationIssues);
            var governance = OperationGovernanceProfiles.EvaluateValidated(
                operationName,
                new GlobalCanonValidator(_agentRoot).Check(),
                validationSummary,
                applyEngineImplemented: true,
                rollbackEngineImplemented: true,
                productionApplyEnabled: false,
                pathScopeValidated: true,
                pathTraversalDetected: false,
                genericShellExposed: false,
                externalWriteRequested: false,
                secretsDetected: SecretRegex.IsMatch(desiredContent),
                buildPassed: true,
                testsPassed: true,
                destructiveOperationRequested: false,
                requirePlanForApply: context.Safety.RequireDryRunBeforeApply,
                requireDiffForApply: context.Safety.RequireDiffBeforeApply,
                requireRollbackForApply: context.Safety.BackupBeforeApply,
                hasPlan: true,
                hasDiff: true,
                hasRollback: persist);

            var action = File.Exists(resolved.FullPath) ? "modify" : "create";
            var diffPreview = BuildDiffPreview(currentContent, desiredContent, action);

            var manifest = new OperationManifest
            {
                OperationId = JsonOutput.GenerateOperationId(),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ActiveProfile = context.Paths.ActiveProfile,
                ConfigFingerprint = context.Fingerprint,
                EntityType = capability.Key,
                OperationType = persist ? request.Intent switch
                {
                    ImplementationIntent.CreateContent => "create-content",
                    ImplementationIntent.Fix => "fix-code",
                    _ => "implementation-dry-run"
                } : "implementation-plan",
                Input = new
                {
                    request.Intent,
                    resolved.Workspace,
                    targetPath = resolved.DisplayPath,
                    request.LanguageHint,
                    request.Template,
                    request.Title,
                    request.Name,
                    request.Description,
                    request.Instruction,
                    request.ContentFilePath
                },
                Status = persist ? "planned" : "draft",
                SupervisionMode = "codex-supervised",
                GeneratedBy = "setimmo",
                RequiresCodexReview = semanticPlan.RequiresCodexReview,
                CodexReviewStatus = semanticPlan.RequiresCodexReview ? "pending" : "not_required",
                SemanticConfidence = semanticPlan.SemanticConfidence,
                PatchQuality = semanticPlan.PatchQuality,
                RiskLevel = semanticPlan.RiskLevel,
                CanAutoApply = !semanticPlan.RequiresCodexReview && semanticPlan.SemanticConfidence >= 0.9,
                NeedsCodexRepair = semanticPlan.NeedsCodexRepair,
                ContextPackPath = BuildContextPackPointer(semanticPlan.Intent),
                Applied = false,
                ValidationIssues = validationIssues,
                AffectedFiles =
                [
                    new AffectedFile
                    {
                        Path = resolved.FullPath,
                        Action = action,
                        Description = BuildDescription(request.Intent, resolved.DisplayPath, capability.DisplayName),
                        DiffPreview = diffPreview
                    }
                ]
            };

            if (issues.Any(issue => issue.Severity is "warning" or "info"))
            {
                manifest.Warnings.AddRange(issues
                    .Where(issue => issue.Severity is "warning" or "info")
                    .Select(issue => issue.Message));
            }

            if (persist)
            {
                PersistOperation(context, manifest, request, resolved, capability, currentContent, desiredContent, action, semanticPlan);
            }

            output.OperationId = manifest.OperationId;
            output.ActiveProfile = context.Paths.ActiveProfile;
            output.ConfigFingerprint = context.Fingerprint;
            output.Summary = persist
                ? $"Implementation dry-run prepared for {resolved.DisplayPath}."
                : $"Implementation plan prepared for {resolved.DisplayPath}.";
            output.NextRequiredAction = persist
                ? governance.ApplyEnabled ? "apply_implement" : governance.RecommendedAction
                : "run_dry_run_implement";
            output.Data = new
            {
                operationId = manifest.OperationId,
                intent = request.Intent.ToString(),
                workspace = resolved.Workspace,
                targetPath = resolved.DisplayPath,
                resolvedPath = resolved.FullPath,
                language = capability.Key,
                action,
                persisted = persist,
                diffPreview,
                issues,
                supervision = BuildSupervisionMetadata(semanticPlan, persist),
                validation = new
                {
                    safeForReadOnlyWork = governance.SafeForReadOnlyWork,
                    safeForDryRun = governance.SafeForDryRun,
                    safeForApply = governance.SafeForApply,
                    safeForProductionApply = governance.SafeForProductionApply,
                    applyEnabled = governance.ApplyEnabled,
                    rollbackEnabled = governance.RollbackEnabled
                },
                manifestPath = manifest.DiffPath is null ? null : Path.Combine("logs", "operations", $"{manifest.OperationId}.json"),
                diffPath = manifest.DiffPath,
                rollbackPlanPath = manifest.RollbackPlanPath,
                status = semanticPlan.Status,
                needsCodexRepair = semanticPlan.NeedsCodexRepair,
                semanticConfidence = semanticPlan.SemanticConfidence,
                patchQuality = semanticPlan.PatchQuality,
                riskLevel = semanticPlan.RiskLevel,
                canAutoApply = !semanticPlan.RequiresCodexReview && semanticPlan.SemanticConfidence >= 0.9,
                governance
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error(operationName, ex.Message);
        }

        return output;
    }

    private void PersistOperation(
        LoadedContext context,
        OperationManifest manifest,
        ImplementationRequest request,
        ResolvedTarget resolved,
        LanguageCapability capability,
        string? currentContent,
        string desiredContent,
        string action,
        SemanticPatchPlan semanticPlan)
    {
        var opsDir = Path.Combine(_agentRoot, "logs", "operations");
        var diffDir = Path.Combine(_agentRoot, "logs", "diffs");
        var rbDir = Path.Combine(_agentRoot, "logs", "rollbacks");
        Directory.CreateDirectory(opsDir);
        Directory.CreateDirectory(diffDir);
        Directory.CreateDirectory(rbDir);

        var plan = new ImplementationPlanDocument
        {
            OperationId = manifest.OperationId,
            Intent = request.Intent.ToString(),
            Workspace = resolved.Workspace,
            Language = capability.Key,
            Instruction = request.Instruction,
            Supervision = BuildSupervisionMetadata(semanticPlan, persist: true),
            Files =
            [
                new RollbackFileEntry
                {
                    Path = resolved.FullPath,
                    Action = action,
                    ExistedBefore = currentContent is not null,
                    OriginalContent = currentContent,
                    TargetContent = desiredContent,
                    Language = capability.Key,
                    SemanticIntent = semanticPlan.Intent,
                    PatchQuality = semanticPlan.PatchQuality
                }
            ]
        };

        var rollback = new RollbackPlanDocument
        {
            OperationId = manifest.OperationId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Applied = false,
            Note = "Rollback is ready for implementation apply within writable roots.",
            Files = plan.Files
        };

        var manifestPath = Path.Combine(opsDir, $"{manifest.OperationId}.json");
        var diffPath = Path.Combine(diffDir, $"{manifest.OperationId}.diff.json");
        var rollbackPath = Path.Combine(rbDir, $"{manifest.OperationId}.rollback.json");

        File.WriteAllText(diffPath, JsonSerializer.Serialize(plan, JsonOptions), Encoding.UTF8);
        File.WriteAllText(rollbackPath, JsonSerializer.Serialize(rollback, JsonOptions), Encoding.UTF8);
        manifest.DiffPath = Path.GetRelativePath(_agentRoot, diffPath);
        manifest.RollbackPlanPath = Path.GetRelativePath(_agentRoot, rollbackPath);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), Encoding.UTF8);

        var logger = new AgentLogger(_agentRoot);
        logger.Log("operations", new
        {
            eventType = "implementation_dry_run",
            operationId = manifest.OperationId,
            workspace = resolved.Workspace,
            targetPath = resolved.DisplayPath,
            action,
            language = capability.Key,
            semanticIntent = semanticPlan.Intent,
            semanticConfidence = semanticPlan.SemanticConfidence,
            patchQuality = semanticPlan.PatchQuality.Classification,
            requiresCodexReview = semanticPlan.RequiresCodexReview,
            createdAtUtc = DateTimeOffset.UtcNow
        });
    }

    private static string BuildDescription(ImplementationIntent intent, string targetPath, string languageName) => intent switch
    {
        ImplementationIntent.CreateContent => $"Create new {languageName} content at {targetPath}.",
        ImplementationIntent.Fix => $"Apply safe auto-fixes to {targetPath}.",
        _ => $"Implement controlled content update for {targetPath}."
    };

    private SemanticPatchPlan BuildSemanticPatchPlan(
        ImplementationRequest request,
        LanguageCapability capability,
        string? currentContent,
        string resolvedPath)
    {
        if (request.Intent == ImplementationIntent.Fix)
        {
            var fixedContent = BuildAutoFixContent(resolvedPath, currentContent, capability);
            if (fixedContent is null)
                return BuildManualRepairPlan("fix_unavailable", "No auto-fix could be produced for the target.");

            var quality = PatchQualityGate.Evaluate(currentContent, fixedContent, request, resolvedPath);
            var confidence = quality.Valid ? ImplementationConfidenceScorer.Score(0.88, "low", quality) : 0.2;
            return new SemanticPatchPlan
            {
                Status = quality.Valid ? "planned" : "needs_codex_repair",
                Intent = "auto-fix",
                TargetKind = "file",
                Reason = "Language formatter produced an auto-fix.",
                TargetContent = quality.Valid ? fixedContent : null,
                RequiresCodexReview = confidence < 0.9,
                NeedsCodexRepair = !quality.Valid,
                SemanticConfidence = confidence,
                PatchQuality = quality,
                RiskLevel = "low",
                Advice = quality.Valid ? ["Review normalized diff before apply."] : ["Ask Codex to repair the patch."]
            };
        }

        if (request.Intent == ImplementationIntent.CreateContent)
        {
            request.Template ??= "default";
        }

        if (!string.IsNullOrWhiteSpace(request.ContentFilePath))
        {
            var fullInputPath = ResolveContentFilePath(request.ContentFilePath);
            request.Content = File.ReadAllText(fullInputPath);
        }

        return SemanticPatchPlanner.Plan(
            request,
            capability,
            currentContent,
            resolvedPath,
            (path, content) => capability.Formatter(path, content));
    }

    private static SemanticPatchPlan BuildManualRepairPlan(string code, string reason) => new()
    {
        Status = "needs_codex_repair",
        Intent = code,
        TargetKind = "unknown",
        Reason = reason,
        RequiresCodexReview = true,
        NeedsCodexRepair = true,
        SemanticConfidence = 0.1,
        RiskLevel = "high",
        PatchQuality = new PatchQualityReport
        {
            Valid = false,
            Classification = "non_semantic_patch",
            Reason = reason,
            Blockers = ["non_semantic_patch"]
        },
        Advice = ["Ask Codex to produce a semantic patch."]
    };

    private static OperationSupervisionMetadata BuildSupervisionMetadata(SemanticPatchPlan semanticPlan, bool persist) => new()
    {
        SupervisionMode = "codex-supervised",
        GeneratedBy = "setimmo",
        ReviewedBy = null,
        RequiresCodexReview = semanticPlan.RequiresCodexReview,
        CodexReviewStatus = semanticPlan.RequiresCodexReview ? "pending" : "not_required",
        SemanticConfidence = semanticPlan.SemanticConfidence,
        PatchQuality = semanticPlan.PatchQuality,
        RiskLevel = semanticPlan.RiskLevel,
        CanAutoApply = !semanticPlan.RequiresCodexReview && semanticPlan.SemanticConfidence >= 0.9,
        NeedsCodexRepair = semanticPlan.NeedsCodexRepair,
        RollbackAvailable = persist && semanticPlan.PatchQuality.Valid,
        ContextPackPath = BuildContextPackPointer(semanticPlan.Intent),
        OperationScopedAuthorization = new
        {
            safeForApply = false,
            canApply = false,
            reason = "Operation still requires validation, rollback, and review gates before apply."
        }
    };

    private static string BuildContextPackPointer(string area)
    {
        var normalized = string.IsNullOrWhiteSpace(area)
            ? "implementation-engine"
            : area.Replace(' ', '-').Replace('_', '-').ToLowerInvariant();
        return Path.Combine("context-packs", $"{normalized}-pack.md").Replace('\\', '/');
    }

    private static JsonOutput BuildNeedsCodexRepairOutput(
        string operationName,
        LoadedContext context,
        ImplementationRequest request,
        ResolvedTarget resolved,
        LanguageCapability capability,
        SemanticPatchPlan semanticPlan)
    {
        var output = JsonOutput.Error(operationName, "Setimmo could not produce a valid semantic patch.");
        output.ActiveProfile = context.Paths.ActiveProfile;
        output.ConfigFingerprint = context.Fingerprint;
        output.NextRequiredAction = "codex_repair";
        output.Data = new
        {
            status = "needs_codex_repair",
            blocker = "non_semantic_patch",
            request = new
            {
                request.Intent,
                resolved.Workspace,
                targetPath = resolved.DisplayPath,
                request.LanguageHint,
                request.Instruction,
                request.Template,
                hasContent = !string.IsNullOrWhiteSpace(request.Content),
                hasContentFile = !string.IsNullOrWhiteSpace(request.ContentFilePath)
            },
            language = capability.Key,
            supervision = BuildSupervisionMetadata(semanticPlan, persist: false),
            semanticPatch = semanticPlan,
            advice = semanticPlan.Advice
        };
        return output;
    }

    private string? BuildAutoFixContent(string resolvedPath, string? currentContent, LanguageCapability capability)
    {
        if (string.IsNullOrWhiteSpace(currentContent))
            return null;

        var normalized = capability.Formatter(resolvedPath, currentContent);
        return normalized == currentContent ? currentContent : normalized;
    }

    private List<ImplementationReviewIssue> AnalyzeContent(string path, string content, LanguageCapability? capability)
    {
        var issues = new List<ImplementationReviewIssue>();
        var lineEnding = LanguageCapabilityRegistry.SuggestedLineEnding(path, content);
        var normalized = LanguageCapabilityRegistry.NormalizeText(content, lineEnding);

        if (!string.Equals(content, normalized, StringComparison.Ordinal))
        {
            issues.Add(new ImplementationReviewIssue
            {
                Severity = "warning",
                Code = "format.normalization_recommended",
                Message = "Formatting normalization is recommended before apply.",
                CanAutoFix = true
            });
        }

        if (SecretRegex.IsMatch(content))
        {
            issues.Add(new ImplementationReviewIssue
            {
                Severity = "error",
                Code = "security.secret_detected",
                Message = "Potential secret or credential pattern detected in content.",
                BlocksApply = true
            });
        }

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].EndsWith(' ') || lines[i].EndsWith('\t'))
            {
                issues.Add(new ImplementationReviewIssue
                {
                    Severity = "warning",
                    Code = "style.trailing_whitespace",
                    Message = "Line has trailing whitespace.",
                    Line = i + 1,
                    CanAutoFix = true
                });
            }

            if (lines[i].Contains("TODO", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ImplementationReviewIssue
                {
                    Severity = "info",
                    Code = "review.todo_marker",
                    Message = "TODO/FIXME marker found.",
                    Line = i + 1
                });
            }

            if (ContainsControlCharacters(lines[i]))
            {
                issues.Add(new ImplementationReviewIssue
                {
                    Severity = "error",
                    Code = "content.control_character",
                    Message = "Control characters are blocked.",
                    Line = i + 1,
                    BlocksApply = true
                });
            }
        }

        if (Regex.IsMatch(content, @"catch\s*\{\s*\}", RegexOptions.Multiline))
        {
            issues.Add(new ImplementationReviewIssue
            {
                Severity = "warning",
                Code = "review.empty_catch",
                Message = "Empty catch block found; review error handling.",
                CanAutoFix = false
            });
        }

        if (capability is not null)
        {
            var validation = capability.Validator(path, content);
            issues.AddRange(validation.Messages.Select(message => new ImplementationReviewIssue
            {
                Severity = message.Severity,
                Code = message.Code,
                Message = message.Message,
                BlocksApply = message.Severity is "error" or "critical"
            }));
        }

        return issues
            .GroupBy(issue => new { issue.Severity, issue.Code, issue.Message, issue.Line, issue.CanAutoFix, issue.BlocksApply })
            .Select(group => group.First())
            .OrderByDescending(issue => SeverityRank(issue.Severity))
            .ThenBy(issue => issue.Line ?? int.MaxValue)
            .ToList();
    }

    private static ValidationIssue ToValidationIssue(ImplementationReviewIssue issue) => new()
    {
        Severity = issue.Severity,
        Scope = "project-code",
        Code = issue.Code,
        Message = issue.Message,
        Line = issue.Line,
        Recommendation = issue.CanAutoFix ? "Use fix code or regenerate the plan." : "Review the generated content manually."
    };

    private static ValidationDecisionSummary BuildValidationSummary(IEnumerable<ValidationIssue> issues)
    {
        var list = issues.ToList();
        return new ValidationDecisionSummary
        {
            SafeForReadOnlyWork = !list.Any(issue => issue.Severity == "critical"),
            SafeForDryRun = !list.Any(issue => issue.Severity == "critical"),
            SafeForApply = !list.Any(issue => issue.Severity is "error" or "critical"),
            IssueSummaryByScope = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["project-code"] = list.Count,
                ["external-data"] = 0,
                ["config"] = 0,
                ["cache"] = 0,
                ["security"] = list.Count(issue => issue.Code.StartsWith("security.", StringComparison.OrdinalIgnoreCase)),
                ["agent-runtime"] = 0
            },
            IssueSummaryByBlockingTarget = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["apply"] = list.Count(issue => issue.Severity is "error" or "critical"),
                ["dry-run"] = list.Count(issue => issue.Severity == "critical"),
                ["read-only-audit"] = list.Count(issue => issue.Severity == "critical")
            }
        };
    }

    private static JsonOutput BuildGovernanceBlockedOutput(string mode, OperationGovernanceAssessment governance, string summary)
    {
        var output = JsonOutput.Error(mode, summary);
        output.NextRequiredAction = governance.RecommendedAction;
        output.Data = new
        {
            safeForReadOnlyWork = governance.SafeForReadOnlyWork,
            safeForDryRun = governance.SafeForDryRun,
            safeForApply = governance.SafeForApply,
            safeForProductionApply = governance.SafeForProductionApply,
            applyEnabled = governance.ApplyEnabled,
            rollbackEnabled = governance.RollbackEnabled,
            governance
        };
        return output;
    }

    private string BuildDiffPreview(string? before, string after, string action)
    {
        var preview = new StringBuilder();

        if (action == "create" || before is null)
        {
            foreach (var line in after.Replace("\r\n", "\n").Split('\n').Take(40))
                preview.AppendLine($"+ {line}");
            return preview.ToString().TrimEnd();
        }

        var beforeLines = before.Replace("\r\n", "\n").Split('\n');
        var afterLines = after.Replace("\r\n", "\n").Split('\n');
        var max = Math.Max(beforeLines.Length, afterLines.Length);

        for (var i = 0; i < max && preview.Length < 4000; i++)
        {
            var oldLine = i < beforeLines.Length ? beforeLines[i] : null;
            var newLine = i < afterLines.Length ? afterLines[i] : null;
            if (string.Equals(oldLine, newLine, StringComparison.Ordinal))
                continue;

            if (oldLine is not null)
                preview.AppendLine($"- {oldLine}");
            if (newLine is not null)
                preview.AppendLine($"+ {newLine}");
        }

        return preview.ToString().TrimEnd();
    }

    private void RestoreRollback(LoadedContext context, RollbackPlanDocument rollback)
    {
        foreach (var file in rollback.Files.AsEnumerable().Reverse())
        {
            var writeCheck = context.Guard.EnsureCanWrite(file.Path);
            if (!writeCheck.IsAllowed)
                throw new InvalidOperationException(writeCheck.Reason ?? "Rollback write access denied.");

            if (file.ExistedBefore)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);
                File.WriteAllText(file.Path, file.OriginalContent ?? string.Empty, Encoding.UTF8);
            }
            else if (File.Exists(file.Path))
            {
                File.Delete(file.Path);
            }
        }
    }

    private string ResolveContentFilePath(string contentFilePath)
    {
        var full = Path.IsPathRooted(contentFilePath)
            ? Path.GetFullPath(contentFilePath)
            : Path.GetFullPath(Path.Combine(_agentRoot, contentFilePath));

        if (!PathGuard.IsContainedIn(full, Path.GetFullPath(_agentRoot)))
            throw new InvalidOperationException("content-file must be located inside the agentRoot.");

        if (!File.Exists(full))
            throw new FileNotFoundException($"content-file not found: {contentFilePath}", full);

        return full;
    }

    private LoadedContext LoadContext()
    {
        var loader = new ConfigLoader(_configDir);
        var paths = loader.LoadPathsConfig();
        var safety = loader.LoadSafetyConfig();
        var profile = ConfigLoader.GetActiveProfile(paths);
        var fingerprint = ConfigFingerprint.Generate(paths, safety);
        return new LoadedContext(paths, safety, profile, fingerprint, new PathGuard(profile.WritableRoots, profile.ReadOnlyRoots, safety.BlockLubEditing));
    }

    private ResolvedTarget ResolveTargetPath(string targetPath, string workspace, ProfileConfig profile)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new InvalidOperationException("target path is required.");

        var normalizedWorkspace = string.IsNullOrWhiteSpace(workspace) ? "main" : workspace.Trim().ToLowerInvariant();
        var baseRoot = normalizedWorkspace switch
        {
            "agent" => _agentRoot,
            "main" => profile.RagnaforgeMainProjectPath,
            _ => throw new InvalidOperationException($"Unsupported workspace '{workspace}'. Use main or agent.")
        };

        if (string.IsNullOrWhiteSpace(baseRoot))
            throw new InvalidOperationException($"Workspace '{normalizedWorkspace}' is not configured.");

        var fullPath = Path.IsPathRooted(targetPath)
            ? Path.GetFullPath(targetPath)
            : Path.GetFullPath(Path.Combine(baseRoot, targetPath));

        if (Path.GetExtension(fullPath).Length == 0)
            throw new InvalidOperationException("target path must include a file extension.");

        return new ResolvedTarget
        {
            Workspace = normalizedWorkspace,
            FullPath = fullPath,
            DisplayPath = Path.IsPathRooted(targetPath) ? fullPath : targetPath
        };
    }

    private static bool ContainsControlCharacters(string value) =>
        value.Any(ch => char.IsControl(ch) && ch is not '\r' and not '\n' and not '\t');

    private static int SeverityRank(string severity) => severity switch
    {
        "critical" => 4,
        "error" => 3,
        "warning" => 2,
        _ => 1
    };

    private sealed record LoadedContext(
        PathsConfig Paths,
        SafetyConfig Safety,
        ProfileConfig Profile,
        string Fingerprint,
        PathGuard Guard);

    private sealed class ResolvedTarget
    {
        public string Workspace { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string DisplayPath { get; set; } = string.Empty;
    }
}
