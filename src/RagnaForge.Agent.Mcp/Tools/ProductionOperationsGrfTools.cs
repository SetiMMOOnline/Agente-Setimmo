using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Mcp.Safety;

namespace RagnaForge.Agent.Mcp.Tools;

public sealed class OperationsListTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_operations_list";
    public string Description => "List recorded agent operations without changing files.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new OperationsCommand(context.AgentRoot, "list", null, null, null).Execute());
}

public sealed class OperationsShowTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_operations_show";
    public string Description => "Show one recorded agent operation by ID.";
    public object InputSchema => SchemaFactory.OperationIdOnly("operationId");

    public JsonOutput Execute(JsonElement arguments)
    {
        var operationId = ImplementationToolArguments.GetString(arguments, "operationId");
        var invalid = McpToolPolicy.ValidateOperationId(operationId, "operations-show");
        if (invalid is not null) return invalid;
        return McpResponseLimiter.Limit(new OperationsCommand(context.AgentRoot, "show", operationId, null, null).Execute());
    }
}

public sealed class OperationsCompareTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_operations_compare";
    public string Description => "Compare two recorded operation manifests.";
    public object InputSchema => SchemaFactory.OperationCompare();

    public JsonOutput Execute(JsonElement arguments)
    {
        var left = ImplementationToolArguments.GetString(arguments, "left");
        var right = ImplementationToolArguments.GetString(arguments, "right");
        var invalidLeft = McpToolPolicy.ValidateOperationId(left, "operations-compare");
        if (invalidLeft is not null) return invalidLeft;
        var invalidRight = McpToolPolicy.ValidateOperationId(right, "operations-compare");
        if (invalidRight is not null) return invalidRight;
        return McpResponseLimiter.Limit(new OperationsCommand(context.AgentRoot, "compare", null, left, right).Execute());
    }
}

public sealed class ProductionStatusTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_production_status";
    public string Description => "Evaluate formal production readiness for an existing operation.";
    public object InputSchema => SchemaFactory.ProductionOperation();

    public JsonOutput Execute(JsonElement arguments) =>
        ExecuteProduction(arguments, "status");

    private JsonOutput ExecuteProduction(JsonElement arguments, string subcommand)
    {
        var operationId = ImplementationToolArguments.GetString(arguments, "operationId");
        var invalid = McpToolPolicy.ValidateOperationId(operationId, "production-status");
        if (invalid is not null) return invalid;
        var environment = ImplementationToolArguments.GetString(arguments, "environment") ?? "local";
        return McpResponseLimiter.Limit(new ProductionCommand(context.ConfigDir, context.AgentRoot, subcommand, operationId, environment, null, null, 1440, false).Execute());
    }
}

public sealed class ProductionAuditTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_production_audit";
    public string Description => "List formal production approvals and policy state.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new ProductionCommand(context.ConfigDir, context.AgentRoot, "audit", null, "local", null, null, 1440, false).Execute());
}

public sealed class ProductionApproveTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_production_approve";
    public string Description => "Record human approval for a current operation diff hash.";
    public object InputSchema => SchemaFactory.ProductionApprove();

    public JsonOutput Execute(JsonElement arguments)
    {
        var operationId = ImplementationToolArguments.GetString(arguments, "operationId");
        var invalid = McpToolPolicy.ValidateOperationId(operationId, "production-approve");
        if (invalid is not null) return invalid;
        var environment = ImplementationToolArguments.GetString(arguments, "environment") ?? "local";
        var approver = ImplementationToolArguments.GetString(arguments, "approver");
        var reason = ImplementationToolArguments.GetString(arguments, "reason");
        var ttl = ImplementationToolArguments.GetInt(arguments, "ttlMinutes") ?? 1440;
        return McpResponseLimiter.Limit(new ProductionCommand(context.ConfigDir, context.AgentRoot, "approve", operationId, environment, approver, reason, ttl, false).Execute());
    }
}

public sealed class ProductionApplyTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_production_apply";
    public string Description => "Apply an operation only after formal approval, diff hash and rollback checks.";
    public object InputSchema => SchemaFactory.ProductionConfirm();

    public JsonOutput Execute(JsonElement arguments)
    {
        var operationId = ImplementationToolArguments.GetString(arguments, "operationId");
        var invalid = McpToolPolicy.ValidateOperationId(operationId, "production-apply");
        if (invalid is not null) return invalid;
        var environment = ImplementationToolArguments.GetString(arguments, "environment") ?? "local";
        var confirm = ImplementationToolArguments.GetBool(arguments, "confirm");
        return McpResponseLimiter.Limit(new ProductionCommand(context.ConfigDir, context.AgentRoot, "apply", operationId, environment, null, null, 1440, confirm).Execute());
    }
}

public sealed class ProductionRollbackTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_production_rollback";
    public string Description => "Rollback a production-applied agent operation using its existing rollback plan.";
    public object InputSchema => SchemaFactory.ProductionConfirm();

    public JsonOutput Execute(JsonElement arguments)
    {
        var operationId = ImplementationToolArguments.GetString(arguments, "operationId");
        var invalid = McpToolPolicy.ValidateOperationId(operationId, "production-rollback");
        if (invalid is not null) return invalid;
        var environment = ImplementationToolArguments.GetString(arguments, "environment") ?? "local";
        var confirm = ImplementationToolArguments.GetBool(arguments, "confirm");
        return McpResponseLimiter.Limit(new ProductionCommand(context.ConfigDir, context.AgentRoot, "rollback", operationId, environment, null, null, 1440, confirm).Execute());
    }
}

public sealed class GrfListTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_grf_list";
    public string Description => "Inventory GRF_Extractor integration without changing files.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new GrfCommand(context.ConfigDir, context.AgentRoot, "list", null, null, false).Execute());
}

public sealed class GrfInspectTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_grf_inspect";
    public string Description => "Inspect GRF container metadata only.";
    public object InputSchema => SchemaFactory.GrfSource();

    public JsonOutput Execute(JsonElement arguments)
    {
        var source = ImplementationToolArguments.GetString(arguments, "source");
        if (string.IsNullOrWhiteSpace(source)) return JsonOutput.Error("grf-inspect", "Missing required argument: source");
        return McpResponseLimiter.Limit(new GrfCommand(context.ConfigDir, context.AgentRoot, "inspect", source, null, false).Execute());
    }
}

public sealed class GrfDryRunExtractTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_grf_dry_run_extract";
    public string Description => "Plan controlled GRF metadata output without modifying original containers.";
    public object InputSchema => SchemaFactory.GrfSource();

    public JsonOutput Execute(JsonElement arguments)
    {
        var source = ImplementationToolArguments.GetString(arguments, "source");
        if (string.IsNullOrWhiteSpace(source)) return JsonOutput.Error("grf-dry-run-extract", "Missing required argument: source");
        return McpResponseLimiter.Limit(new GrfCommand(context.ConfigDir, context.AgentRoot, "dry-run-extract", source, null, false).Execute());
    }
}

public sealed class GrfExtractTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_grf_extract";
    public string Description => "Complete controlled GRF metadata output inside agent-owned temp storage only.";
    public object InputSchema => SchemaFactory.OperationIdWithConfirm();

    public JsonOutput Execute(JsonElement arguments)
    {
        var operationId = ImplementationToolArguments.GetString(arguments, "operationId");
        var invalid = McpToolPolicy.ValidateOperationId(operationId, "grf-extract");
        if (invalid is not null) return invalid;
        var confirm = ImplementationToolArguments.GetBool(arguments, "confirm");
        return McpResponseLimiter.Limit(new GrfCommand(context.ConfigDir, context.AgentRoot, "extract", null, operationId, confirm).Execute());
    }
}

public sealed class FieldTestRunTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_field_test_run";
    public string Description => "Run local field fixtures in an agent-owned sandbox; no shell execution and no external writes.";
    public object InputSchema => SchemaFactory.Empty();

    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new FieldTestCommand(context.AgentRoot, "test", "run", keepSandbox: false).Execute());
}
