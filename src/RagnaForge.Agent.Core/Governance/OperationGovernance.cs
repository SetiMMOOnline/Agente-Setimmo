namespace RagnaForge.Agent.Core.Governance;

public enum ExecutionMode
{
    Observe,
    Plan,
    DryRun,
    Apply,
    Rollback,
    Production
}

public sealed class OperationGovernanceContext
{
    public string OperationName { get; set; } = "unknown";
    public bool CanonSafeForReadOnlyWork { get; set; } = true;
    public bool CanonSafeForDryRun { get; set; } = true;
    public bool CanonSafeForApply { get; set; } = true;
    public bool ValidationStateKnown { get; set; } = true;
    public bool ValidationSafeForReadOnlyWork { get; set; } = true;
    public bool ValidationSafeForDryRun { get; set; } = true;
    public bool ValidationSafeForApply { get; set; } = true;
    public bool PathScopeValidated { get; set; } = true;
    public bool PathTraversalDetected { get; set; }
    public bool DiffValid { get; set; } = true;
    public bool HasPlan { get; set; } = true;
    public bool HasDiff { get; set; } = true;
    public bool HasRollback { get; set; }
    public bool ApplyEngineImplemented { get; set; }
    public bool RollbackEngineImplemented { get; set; }
    public bool ProductionApplyEnabled { get; set; }
    public string ProductionEnvironment { get; set; } = "local";
    public bool ProductionHumanApprovalRecorded { get; set; }
    public bool ProductionApprovalHashMatches { get; set; }
    public bool ProductionApprovalExpired { get; set; }
    public bool ProductionScopeAuthorized { get; set; } = true;
    public bool ProductionAuditLogAvailable { get; set; } = true;
    public bool ProductionOperationClassified { get; set; } = true;
    public bool ProductionRiskWithinLimit { get; set; } = true;
    public bool GenericShellExposed { get; set; }
    public bool SecretsDetected { get; set; }
    public bool ExternalWriteRequested { get; set; }
    public bool BuildPassed { get; set; } = true;
    public bool TestsPassed { get; set; } = true;
    public bool DestructiveOperationRequested { get; set; }
    public bool RequirePlanForApply { get; set; }
    public bool RequireDiffForApply { get; set; }
    public bool RequireRollbackForApply { get; set; }
}

public sealed class OperationGovernanceFinding
{
    public string Id { get; init; } = string.Empty;
    public string Severity { get; init; } = "info";
    public string Message { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = "none";
    public bool BlocksReadOnlyWork { get; init; }
    public bool BlocksDryRun { get; init; }
    public bool BlocksApply { get; init; }
    public bool BlocksProductionApply { get; init; }
}

public sealed class OperationGovernanceAssessment
{
    public bool SafeForReadOnlyWork { get; init; }
    public bool SafeForDryRun { get; init; }
    public bool SafeForApply { get; init; }
    public bool SafeForProductionApply { get; init; }
    public bool ApplyEnabled { get; init; }
    public bool RollbackEnabled { get; init; }
    public string RecommendedAction { get; init; } = "none";
    public List<string> AllowedModes { get; init; } = [];
    public List<OperationGovernanceFinding> Findings { get; init; } = [];
}

public interface IOperationGovernanceValidator
{
    void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings);
}

public sealed class OperationGovernanceEngine
{
    private readonly IReadOnlyList<IOperationGovernanceValidator> _validators;

    public OperationGovernanceEngine(IEnumerable<IOperationGovernanceValidator>? validators = null)
    {
        _validators = validators?.ToList() ?? DefaultValidators();
    }

    public OperationGovernanceAssessment Evaluate(OperationGovernanceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = new List<OperationGovernanceFinding>();
        foreach (var validator in _validators)
            validator.Validate(context, findings);

        var safeForReadOnlyWork = findings.All(f => !f.BlocksReadOnlyWork);
        var safeForDryRun = findings.All(f => !f.BlocksDryRun);
        var safeForApply = findings.All(f => !f.BlocksApply);
        var safeForProductionApply = findings.All(f => !f.BlocksProductionApply);

        var allowedModes = new List<string>();
        if (safeForReadOnlyWork)
            allowedModes.Add(ExecutionMode.Observe.ToString());
        if (safeForReadOnlyWork)
            allowedModes.Add(ExecutionMode.Plan.ToString());
        if (safeForDryRun)
            allowedModes.Add(ExecutionMode.DryRun.ToString());
        if (safeForApply && context.ApplyEngineImplemented)
            allowedModes.Add(ExecutionMode.Apply.ToString());
        if (context.RollbackEngineImplemented)
            allowedModes.Add(ExecutionMode.Rollback.ToString());
        if (safeForProductionApply && context.ApplyEngineImplemented && context.ProductionApplyEnabled)
            allowedModes.Add(ExecutionMode.Production.ToString());

        return new OperationGovernanceAssessment
        {
            SafeForReadOnlyWork = safeForReadOnlyWork,
            SafeForDryRun = safeForDryRun,
            SafeForApply = safeForApply,
            SafeForProductionApply = safeForProductionApply,
            ApplyEnabled = context.ApplyEngineImplemented && safeForApply,
            RollbackEnabled = context.RollbackEngineImplemented,
            RecommendedAction = DetermineRecommendedAction(findings),
            AllowedModes = allowedModes,
            Findings = findings
        };
    }

    public static OperationGovernanceAssessment EvaluateDefault(OperationGovernanceContext context) =>
        new OperationGovernanceEngine().Evaluate(context);

    private static IReadOnlyList<IOperationGovernanceValidator> DefaultValidators() =>
    [
        new CanonStateValidator(),
        new ValidationStateValidator(),
        new PathSafetyValidator(),
        new ShellExposureValidator(),
        new SecretLeakValidator(),
        new DestructiveOperationValidator(),
        new BuildAndTestValidator(),
        new ApplyCapabilityValidator(),
        new PlanDiffRollbackValidator(),
        new ProductionPromotionValidator()
    ];

    private static string DetermineRecommendedAction(IEnumerable<OperationGovernanceFinding> findings)
    {
        var finding = findings.FirstOrDefault(f =>
            f.BlocksReadOnlyWork || f.BlocksDryRun || f.BlocksApply);

        if (finding is not null)
            return finding.RecommendedAction;

        finding = findings.FirstOrDefault(f => f.BlocksProductionApply);

        if (finding is not null)
            return finding.RecommendedAction;

        return findings
            .FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.RecommendedAction) &&
                                 !f.RecommendedAction.Equals("none", StringComparison.OrdinalIgnoreCase))
            ?.RecommendedAction
            ?? "none";
    }
}

internal sealed class CanonStateValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (context.CanonSafeForReadOnlyWork)
            return;

        findings.Add(new OperationGovernanceFinding
        {
            Id = "canon_not_safe",
            Severity = "error",
            Message = "Global Canon marked the current environment as unsafe.",
            RecommendedAction = "fix_canon_violations",
            BlocksReadOnlyWork = true,
            BlocksDryRun = true,
            BlocksApply = true,
            BlocksProductionApply = true
        });
    }
}

internal sealed class ValidationStateValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (!context.ValidationStateKnown)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "validation_state_unknown",
                Severity = "warning",
                Message = "Validation has not been executed for this operation context.",
                RecommendedAction = "run_validate",
                BlocksApply = true,
                BlocksProductionApply = true
            });
            return;
        }

        if (!context.ValidationSafeForReadOnlyWork)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "validation_read_only_blocked",
                Severity = "error",
                Message = "Validation indicates the current state is not safe for read-only work.",
                RecommendedAction = "review_validation_issues",
                BlocksReadOnlyWork = true,
                BlocksDryRun = true,
                BlocksApply = true,
                BlocksProductionApply = true
            });
            return;
        }

        if (!context.ValidationSafeForDryRun)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "validation_dry_run_blocked",
                Severity = "error",
                Message = "Validation indicates dry-run is not safe in the current state.",
                RecommendedAction = "review_validation_issues",
                BlocksDryRun = true,
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }

        if (!context.ValidationSafeForApply)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "validation_apply_blocked",
                Severity = "error",
                Message = "Validation indicates apply is not safe in the current state.",
                RecommendedAction = "review_validation_issues_before_apply",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }
    }
}

internal sealed class PathSafetyValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (context.PathTraversalDetected)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "path_traversal_detected",
                Severity = "critical",
                Message = "A path traversal attempt was detected.",
                RecommendedAction = "fix_path_scope",
                BlocksReadOnlyWork = true,
                BlocksDryRun = true,
                BlocksApply = true,
                BlocksProductionApply = true
            });
            return;
        }

        if (!context.PathScopeValidated)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "path_scope_unvalidated",
                Severity = "error",
                Message = "Path scope has not been validated for the requested operation.",
                RecommendedAction = "validate_path_scope",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }

        if (context.ExternalWriteRequested)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "external_write_requested",
                Severity = "error",
                Message = "The requested operation would write outside the allowed workspace scope.",
                RecommendedAction = "restrict_write_scope",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }
    }
}

internal sealed class ShellExposureValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (!context.GenericShellExposed)
            return;

        findings.Add(new OperationGovernanceFinding
        {
            Id = "generic_shell_exposed",
            Severity = "critical",
            Message = "A generic shell escape would be exposed by this operation.",
            RecommendedAction = "remove_generic_shell",
            BlocksDryRun = true,
            BlocksApply = true,
            BlocksProductionApply = true
        });
    }
}

internal sealed class SecretLeakValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (!context.SecretsDetected)
            return;

        findings.Add(new OperationGovernanceFinding
        {
            Id = "secret_detected",
            Severity = "critical",
            Message = "A secret or token leak was detected in the pending operation.",
            RecommendedAction = "remove_secrets",
            BlocksDryRun = true,
            BlocksApply = true,
            BlocksProductionApply = true
        });
    }
}

internal sealed class DestructiveOperationValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (!context.DestructiveOperationRequested)
            return;

        findings.Add(new OperationGovernanceFinding
        {
            Id = "destructive_operation_requires_policy",
            Severity = "error",
            Message = "Destructive operations require an explicit production policy and confirmation workflow.",
            RecommendedAction = "require_production_policy",
            BlocksApply = true,
            BlocksProductionApply = true
        });
    }
}

internal sealed class BuildAndTestValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (!context.BuildPassed)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "build_failed",
                Severity = "error",
                Message = "Build validation failed.",
                RecommendedAction = "fix_build",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }

        if (!context.TestsPassed)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "tests_failed",
                Severity = "error",
                Message = "Test validation failed.",
                RecommendedAction = "fix_tests",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }
    }
}

internal sealed class PlanDiffRollbackValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (context.RequirePlanForApply && !context.HasPlan)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "plan_missing",
                Severity = "error",
                Message = "No implementation plan is available for this operation.",
                RecommendedAction = "create_plan",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }

        if (context.RequireDiffForApply && (!context.HasDiff || !context.DiffValid))
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "diff_invalid",
                Severity = "error",
                Message = "The pending diff is missing or invalid.",
                RecommendedAction = "generate_valid_diff",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }

        if (context.RequireRollbackForApply && !context.HasRollback)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "rollback_missing",
                Severity = "error",
                Message = "Rollback or snapshot data is required before apply.",
                RecommendedAction = "generate_rollback_plan",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }
    }
}

internal sealed class ApplyCapabilityValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (!context.ApplyEngineImplemented)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "apply_engine_not_implemented",
                Severity = "warning",
                Message = "Apply is validator-governed, but the real apply engine is not implemented yet.",
                RecommendedAction = "implement_apply_engine"
            });
            return;
        }

        if (!context.RollbackEngineImplemented)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "rollback_engine_not_implemented",
                Severity = "error",
                Message = "Rollback support is required before enabling real apply.",
                RecommendedAction = "implement_rollback_engine",
                BlocksApply = true,
                BlocksProductionApply = true
            });
        }

        if (!context.ProductionApplyEnabled)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_mode_disabled",
                Severity = "warning",
                Message = "Production apply remains disabled for this build.",
                RecommendedAction = "keep_dry_run_or_apply_review_mode_only",
                BlocksProductionApply = true
            });
        }
    }
}

internal sealed class ProductionPromotionValidator : IOperationGovernanceValidator
{
    public void Validate(OperationGovernanceContext context, List<OperationGovernanceFinding> findings)
    {
        if (!context.ProductionApplyEnabled)
            return;

        if (!context.ProductionHumanApprovalRecorded)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_human_approval_missing",
                Severity = "error",
                Message = "Production promotion requires a recorded human approval.",
                RecommendedAction = "record_human_approval",
                BlocksProductionApply = true
            });
        }

        if (context.ProductionHumanApprovalRecorded && !context.ProductionApprovalHashMatches)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_approval_hash_mismatch",
                Severity = "critical",
                Message = "Production approval does not match the current diff hash.",
                RecommendedAction = "review_and_reapprove_current_diff",
                BlocksProductionApply = true
            });
        }

        if (context.ProductionApprovalExpired)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_approval_expired",
                Severity = "error",
                Message = "Production approval has expired.",
                RecommendedAction = "record_fresh_human_approval",
                BlocksProductionApply = true
            });
        }

        if (!context.ProductionScopeAuthorized)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_scope_not_authorized",
                Severity = "critical",
                Message = "Production operation scope is not authorized by policy.",
                RecommendedAction = "restrict_or_authorize_scope",
                BlocksProductionApply = true
            });
        }

        if (!context.ProductionAuditLogAvailable)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_audit_log_missing",
                Severity = "error",
                Message = "Production operation requires an audit log entry.",
                RecommendedAction = "create_audit_log",
                BlocksProductionApply = true
            });
        }

        if (!context.ProductionOperationClassified)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_operation_unclassified",
                Severity = "error",
                Message = "Production operation must be classified before apply.",
                RecommendedAction = "classify_operation_risk",
                BlocksProductionApply = true
            });
        }

        if (!context.ProductionRiskWithinLimit)
        {
            findings.Add(new OperationGovernanceFinding
            {
                Id = "production_risk_too_high",
                Severity = "critical",
                Message = "Production operation risk exceeds the allowed policy limit.",
                RecommendedAction = "reduce_scope_or_escalate_review",
                BlocksProductionApply = true
            });
        }
    }
}
