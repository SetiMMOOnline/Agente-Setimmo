using System.Text.Json;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Mcp.Safety;

namespace RagnaForge.Agent.Mcp.Tools;

public sealed class SecurityPolicyTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_security_policy";
    public string Description => "Return Agente Setimmo security policy constraints and safety guarantees. Read-only.";
    public object InputSchema => SchemaFactory.Empty();

    public JsonOutput Execute(JsonElement arguments)
    {
        var output = JsonOutput.Success(Name);
        try
        {
            var safetyPath = Path.Combine(context.ConfigDir, "safety.json");
            object? safetyConfig = null;

            if (File.Exists(safetyPath))
            {
                safetyConfig = JsonSerializer.Deserialize<object>(File.ReadAllText(safetyPath));
            }

            var canon = new GlobalCanonValidator(context.AgentRoot).Check();
            var governance = OperationGovernanceProfiles.EvaluateValidated(
                "mcp-security-policy",
                canon,
                new RagnaForge.Agent.Core.Commands.ValidationDecisionSummary
                {
                    SafeForReadOnlyWork = true,
                    SafeForDryRun = true,
                    SafeForApply = true
                },
                applyEngineImplemented: true,
                rollbackEngineImplemented: true,
                productionApplyEnabled: false);
            output.Summary = "Security policy loaded successfully. Apply and rollback are validator-governed, scoped to writable roots, and still blocked for GRF, Patch/client, rAthena, and .lub targets.";
            output.Data = new
            {
                readOnlyMode = false,
                writeOperationsPermitted = true,
                applyBlocked = !governance.ApplyEnabled,
                rollbackRealBlocked = !governance.RollbackEnabled,
                genericApplyBlocked = true,
                genericRollbackBlocked = true,
                operationScopedApplyAvailable = governance.ApplyEnabled,
                operationScopedRollbackAvailable = governance.RollbackEnabled,
                applyGovernedByValidators = true,
                blockOriginalGrfWrite = true,
                blockLubEditing = true,
                safeForReadOnlyWork = governance.SafeForReadOnlyWork,
                safeForDryRun = governance.SafeForDryRun,
                safeForApply = governance.SafeForApply,
                safeForProductionApply = governance.SafeForProductionApply,
                mcpEnforcedRules = new[]
                {
                    "No shell command execution allowed",
                    "Path traversal is strictly blocked (PathGuard containment)",
                    "Only validator-governed implementation tools may write, and only inside writableRoots",
                    "Response payload size protection enabled"
                },
                implementationTools = new[]
                {
                    "ragnaforge_review_code",
                    "ragnaforge_fix_code",
                    "ragnaforge_create_content",
                    "ragnaforge_plan_implement",
                    "ragnaforge_dry_run_implement",
                    "ragnaforge_apply_implement",
                    "ragnaforge_rollback_implement",
                    "ragnaforge_cleanup_safe"
                },
                governance,
                safetyConfig
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error(Name, ex.Message);
        }

        return McpResponseLimiter.Limit(output);
    }
}
