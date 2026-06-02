using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Implementation;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Mcp.Safety;

namespace RagnaForge.Agent.Mcp.Tools;

public sealed class ReviewCodeTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_review_code";
    public string Description => "Review code inside allowed roots without changing files.";
    public object InputSchema => SchemaFactory.ReviewOrFix();

    public JsonOutput Execute(JsonElement arguments)
    {
        var targetPath = ImplementationToolArguments.GetString(arguments, "targetPath");
        if (string.IsNullOrWhiteSpace(targetPath))
            return JsonOutput.Error("review-code", "Missing required argument: targetPath");

        var workspace = ImplementationToolArguments.GetString(arguments, "workspace") ?? "main";
        var language = ImplementationToolArguments.GetString(arguments, "language");
        return McpResponseLimiter.Limit(new ReviewCodeCommand(context.ConfigDir, context.AgentRoot, targetPath, workspace, language).Execute());
    }
}

public sealed class FixCodeTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_fix_code";
    public string Description => "Generate a validated fix diff and persist it inside agentRoot for later apply.";
    public object InputSchema => SchemaFactory.ReviewOrFix();

    public JsonOutput Execute(JsonElement arguments)
    {
        var targetPath = ImplementationToolArguments.GetString(arguments, "targetPath");
        if (string.IsNullOrWhiteSpace(targetPath))
            return JsonOutput.Error("fix-code", "Missing required argument: targetPath");

        var workspace = ImplementationToolArguments.GetString(arguments, "workspace") ?? "main";
        var language = ImplementationToolArguments.GetString(arguments, "language");
        return McpResponseLimiter.Limit(new FixCodeCommand(context.ConfigDir, context.AgentRoot, targetPath, workspace, language).Execute());
    }
}

public sealed class CreateContentTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_create_content";
    public string Description => "Create a new scaffolded file through a validator-governed implementation diff.";
    public object InputSchema => SchemaFactory.CreateContent();

    public JsonOutput Execute(JsonElement arguments)
    {
        var request = ImplementationToolArguments.ParseImplementationRequest(arguments, defaultWorkspace: "main");
        if (string.IsNullOrWhiteSpace(request.TargetPath))
            return JsonOutput.Error("create-content", "Missing required argument: targetPath");
        if (string.IsNullOrWhiteSpace(request.LanguageHint))
            return JsonOutput.Error("create-content", "Missing required argument: language");

        request.Intent = ImplementationIntent.CreateContent;
        return McpResponseLimiter.Limit(new CreateContentCommand(context.ConfigDir, context.AgentRoot, request).Execute());
    }
}

public sealed class PlanImplementTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_plan_implement";
    public string Description => "Draft an implementation plan without persisting changes.";
    public object InputSchema => SchemaFactory.ImplementPlan();

    public JsonOutput Execute(JsonElement arguments)
    {
        var request = ImplementationToolArguments.ParseImplementationRequest(arguments, defaultWorkspace: "main");
        if (string.IsNullOrWhiteSpace(request.TargetPath))
            return JsonOutput.Error("plan-implement", "Missing required argument: targetPath");

        request.Intent = ImplementationIntent.Implement;
        return McpResponseLimiter.Limit(new PlanImplementCommand(context.ConfigDir, context.AgentRoot, request).Execute());
    }
}

public sealed class DryRunImplementTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_dry_run_implement";
    public string Description => "Persist a validated implementation diff and rollback plan inside agentRoot without touching the target file.";
    public object InputSchema => SchemaFactory.ImplementPlan();

    public JsonOutput Execute(JsonElement arguments)
    {
        var request = ImplementationToolArguments.ParseImplementationRequest(arguments, defaultWorkspace: "main");
        if (string.IsNullOrWhiteSpace(request.TargetPath))
            return JsonOutput.Error("dry-run-implement", "Missing required argument: targetPath");

        request.Intent = ImplementationIntent.Implement;
        return McpResponseLimiter.Limit(new DryRunImplementCommand(context.ConfigDir, context.AgentRoot, request).Execute());
    }
}

public sealed class ApplyImplementTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_apply_implement";
    public string Description => "Apply a previously validated implementation diff inside allowed writable roots.";
    public object InputSchema => SchemaFactory.ApplyImplementation();

    public JsonOutput Execute(JsonElement arguments)
    {
        var operationId = ImplementationToolArguments.GetString(arguments, "operationId");
        var invalid = McpToolPolicy.ValidateOperationId(operationId, "apply-implement");
        if (invalid is not null)
            return invalid;

        var confirm = ImplementationToolArguments.GetBool(arguments, "confirm");
        return McpResponseLimiter.Limit(new ApplyImplementCommand(context.ConfigDir, context.AgentRoot, operationId!, confirm).Execute());
    }
}

public sealed class RollbackImplementTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_rollback_implement";
    public string Description => "Rollback an applied implementation operation previously recorded by the agent.";
    public object InputSchema => SchemaFactory.RollbackImplementation();

    public JsonOutput Execute(JsonElement arguments)
    {
        var rollbackId = ImplementationToolArguments.GetString(arguments, "rollbackId");
        var invalid = McpToolPolicy.ValidateOperationId(rollbackId, "rollback-implement");
        if (invalid is not null)
            return invalid;

        var confirm = ImplementationToolArguments.GetBool(arguments, "confirm");
        return McpResponseLimiter.Limit(new RollbackCommand(context.ConfigDir, context.AgentRoot, rollbackId, list: false, dryRun: false, confirm: confirm).Execute());
    }
}

public sealed class CleanupSafeTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_cleanup_safe";
    public string Description => "Clean regenerable agent artifacts only, never source or external Ragnarok data.";
    public object InputSchema => SchemaFactory.CleanupSafe();

    public JsonOutput Execute(JsonElement arguments)
    {
        var includeLogs = ImplementationToolArguments.GetBool(arguments, "includeLogs");
        var includeCache = ImplementationToolArguments.GetBool(arguments, "includeCache");
        var includeInputs = ImplementationToolArguments.GetBool(arguments, "includeInputs");
        return McpResponseLimiter.Limit(new CleanupCommand(context.AgentRoot, includeLogs, includeCache, includeInputs).Execute());
    }
}

internal static class ImplementationToolArguments
{
    public static ImplementationRequest ParseImplementationRequest(JsonElement arguments, string defaultWorkspace)
    {
        return new ImplementationRequest
        {
            Workspace = GetString(arguments, "workspace") ?? defaultWorkspace,
            TargetPath = GetString(arguments, "targetPath") ?? string.Empty,
            LanguageHint = GetString(arguments, "language"),
            Template = GetString(arguments, "template"),
            Title = GetString(arguments, "title"),
            Name = GetString(arguments, "name"),
            Description = GetString(arguments, "description"),
            Instruction = GetString(arguments, "instruction"),
            ContentFilePath = GetString(arguments, "contentFilePath"),
            Content = GetString(arguments, "content")
        };
    }

    public static string? GetString(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object &&
        arguments.TryGetProperty(name, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    public static bool GetBool(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object &&
        arguments.TryGetProperty(name, out var property) &&
        property.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        property.GetBoolean();

    public static int? GetInt(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object &&
        arguments.TryGetProperty(name, out var property) &&
        property.ValueKind == JsonValueKind.Number &&
        property.TryGetInt32(out var value)
            ? value
            : null;
}
