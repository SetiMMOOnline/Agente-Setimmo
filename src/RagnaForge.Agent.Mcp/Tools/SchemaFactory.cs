namespace RagnaForge.Agent.Mcp.Tools;

internal static class SchemaFactory
{
    public static object Empty() => new
    {
        type = "object",
        properties = new Dictionary<string, object>(),
        additionalProperties = false
    };

    public static object Find(bool allowId) => new
    {
        type = "object",
        properties = allowId
            ? new Dictionary<string, object>
            {
                ["id"] = new { type = "integer" },
                ["name"] = new { type = "string" }
            }
            : new Dictionary<string, object>
            {
                ["name"] = new { type = "string" }
            },
        additionalProperties = false
    };

    public static object Operation() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["operationId"] = new { type = "string" },
            ["last"] = new { type = "boolean" },
            ["format"] = new { type = "string", @enum = new[] { "json", "md" } }
        },
        additionalProperties = false
    };

    public static object OperationIdOnly(string propertyName) => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            [propertyName] = new { type = "string" }
        },
        required = new[] { propertyName },
        additionalProperties = false
    };

    public static object OperationIdWithConfirm() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["operationId"] = new { type = "string" },
            ["confirm"] = new { type = "boolean" }
        },
        required = new[] { "operationId", "confirm" },
        additionalProperties = false
    };

    public static object OperationCompare() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["left"] = new { type = "string" },
            ["right"] = new { type = "string" }
        },
        required = new[] { "left", "right" },
        additionalProperties = false
    };

    public static object ProductionOperation() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["operationId"] = new { type = "string" },
            ["environment"] = new { type = "string", @enum = new[] { "local", "development", "staging", "production" } }
        },
        required = new[] { "operationId" },
        additionalProperties = false
    };

    public static object ProductionApprove() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["operationId"] = new { type = "string" },
            ["environment"] = new { type = "string", @enum = new[] { "local", "development", "staging", "production" } },
            ["approver"] = new { type = "string" },
            ["reason"] = new { type = "string" },
            ["ttlMinutes"] = new { type = "integer", minimum = 5, maximum = 10080 }
        },
        required = new[] { "operationId", "approver", "reason" },
        additionalProperties = false
    };

    public static object ProductionConfirm() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["operationId"] = new { type = "string" },
            ["environment"] = new { type = "string", @enum = new[] { "local", "development", "staging", "production" } },
            ["confirm"] = new { type = "boolean" }
        },
        required = new[] { "operationId", "confirm" },
        additionalProperties = false
    };

    public static object GrfSource() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["source"] = new { type = "string" }
        },
        required = new[] { "source" },
        additionalProperties = false
    };

    public static object DryRun() => new
    {
        type = "object",
        additionalProperties = true
    };

    public static object ReviewOrFix() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["targetPath"] = new { type = "string" },
            ["workspace"] = new { type = "string", @enum = new[] { "main", "agent" } },
            ["language"] = new { type = "string" }
        },
        required = new[] { "targetPath" },
        additionalProperties = false
    };

    public static object CreateContent() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["targetPath"] = new { type = "string" },
            ["workspace"] = new { type = "string", @enum = new[] { "main", "agent" } },
            ["language"] = new { type = "string" },
            ["template"] = new { type = "string" },
            ["title"] = new { type = "string" },
            ["name"] = new { type = "string" },
            ["description"] = new { type = "string" },
            ["instruction"] = new { type = "string" },
            ["content"] = new { type = "string" },
            ["contentFilePath"] = new { type = "string" }
        },
        required = new[] { "targetPath", "language" },
        additionalProperties = false
    };

    public static object ImplementPlan() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["targetPath"] = new { type = "string" },
            ["workspace"] = new { type = "string", @enum = new[] { "main", "agent" } },
            ["language"] = new { type = "string" },
            ["template"] = new { type = "string" },
            ["title"] = new { type = "string" },
            ["name"] = new { type = "string" },
            ["description"] = new { type = "string" },
            ["instruction"] = new { type = "string" },
            ["content"] = new { type = "string" },
            ["contentFilePath"] = new { type = "string" }
        },
        required = new[] { "targetPath" },
        additionalProperties = false
    };

    public static object ApplyImplementation() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["operationId"] = new { type = "string" },
            ["confirm"] = new { type = "boolean" }
        },
        required = new[] { "operationId", "confirm" },
        additionalProperties = false
    };

    public static object RollbackImplementation() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["rollbackId"] = new { type = "string" },
            ["confirm"] = new { type = "boolean" }
        },
        required = new[] { "rollbackId", "confirm" },
        additionalProperties = false
    };

    public static object CleanupSafe() => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["includeLogs"] = new { type = "boolean" },
            ["includeCache"] = new { type = "boolean" },
            ["includeInputs"] = new { type = "boolean" }
        },
        additionalProperties = false
    };
}
