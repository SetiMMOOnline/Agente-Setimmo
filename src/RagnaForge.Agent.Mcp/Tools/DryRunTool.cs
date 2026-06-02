using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;
using RagnaForge.Agent.Mcp.Safety;

namespace RagnaForge.Agent.Mcp.Tools;

public sealed class DryRunTool(McpToolContext context, string entityType) : IMcpTool
{
    public string Name => $"ragnaforge_dry_run_{entityType}";
    public string Description => $"Plan a {entityType} change without applying it. Writes only inside agentRoot logs and inputs/dry-run.";
    public object InputSchema => SchemaFactory.DryRun();

    public JsonOutput Execute(JsonElement arguments)
    {
        var operationId = RagnaForge.Agent.Core.Output.JsonOutput.GenerateOperationId();

        try
        {
            var loader = new ConfigLoader(context.ConfigDir);
            var pathsConfig = loader.LoadPathsConfig();
            var safetyConfig = loader.LoadSafetyConfig();
            var profile = ConfigLoader.GetActiveProfile(pathsConfig);
            var guard = new PathGuard(profile.WritableRoots, profile.ReadOnlyRoots, safetyConfig.BlockLubEditing);
            var inputStore = new McpDryRunInputStore(context.AgentRoot, guard);
            var inputPath = inputStore.Persist(operationId, arguments);

            return McpResponseLimiter.Limit(new DryRunCommand(
                context.ConfigDir, context.AgentRoot, entityType, inputPath).Execute());
        }
        catch (Exception ex)
        {
            return JsonOutput.Error("dry-run", ex.Message);
        }
    }
}
