using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Knowledge;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Mcp.Safety;

namespace RagnaForge.Agent.Mcp.Tools;

public sealed class KnowledgeSourcesTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_sources";
    public string Description => "List all registered RagnaKnowledge sources. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "sources", []).Execute());
}

public sealed class KnowledgeSearchTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_search";
    public string Description => "Search for RagnaKnowledge entries based on query terms. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["query"] = new { type = "string", maxLength = 512, description = "Query search terms (e.g. 'item_db', 'map dependencies')" }
        },
        required = new[] { "query" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var dict = new Dictionary<string, string>();
        if (KnowledgeToolArguments.TryGetString(arguments, "query", out var query))
            dict["query"] = query;

        return McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "search", dict).Execute());
    }
}

public sealed class KnowledgeSourceExplainTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_source_explain";
    public string Description => "Explain a registered knowledge source, including refresh and authorization notes. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["id"] = new { type = "string", maxLength = 128, description = "Knowledge source ID." }
        },
        required = new[] { "id" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var dict = new Dictionary<string, string> { ["action"] = "explain" };
        if (KnowledgeToolArguments.TryGetString(arguments, "id", out var id))
            dict["id"] = id;

        return McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "source", dict).Execute());
    }
}

public sealed class KnowledgeExplainTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_explain";
    public string Description => "Get a detailed explanation for a given topic or entity. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["topic"] = new { type = "string", maxLength = 512, description = "Topic to explain (e.g. 'rsw gnd gat map files', 'duplicate NPC names')" }
        },
        required = new[] { "topic" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var dict = new Dictionary<string, string>();
        if (KnowledgeToolArguments.TryGetString(arguments, "topic", out var topic))
            dict["topic"] = topic;

        return McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "explain", dict).Execute());
    }
}

public sealed class KnowledgeEntryTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_entry";
    public string Description => "Retrieve a specific RagnaKnowledge entry details by ID. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["id"] = new { type = "string", maxLength = 128, description = "Specific entry ID (e.g. 'rathena.item.db_yaml')" }
        },
        required = new[] { "id" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var dict = new Dictionary<string, string>();
        if (KnowledgeToolArguments.TryGetString(arguments, "id", out var id))
            dict["id"] = id;

        return McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "entry", dict).Execute());
    }
}

public sealed class KnowledgeSchemaTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_schema";
    public string Description => "Get property and validation constraints schema for a given entity type. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["entity"] = new { type = "string", maxLength = 32, @enum = new[] { "item", "equipment", "mob", "npc", "map", "asset" }, description = "Entity type to show schema reference for." }
        },
        required = new[] { "entity" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var dict = new Dictionary<string, string>();
        if (KnowledgeToolArguments.TryGetString(arguments, "entity", out var entity))
            dict["entity"] = entity;

        return McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "schema", dict).Execute());
    }
}

public sealed class KnowledgeValidateTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_validate";
    public string Description => "Validate RagnaKnowledge packs integrity. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "validate", []).Execute());
}

public sealed class KnowledgeConflictsTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_conflicts";
    public string Description => "List knowledge conflicts and human-review recommendations. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["entityType"] = new { type = "string", maxLength = 32, description = "Optional entity type filter." }
        },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var dict = new Dictionary<string, string>();
        if (KnowledgeToolArguments.TryGetString(arguments, "entityType", out var entityType))
            dict["entityType"] = entityType;
        return McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "conflicts", dict).Execute());
    }
}

public sealed class KnowledgeCoverageTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_coverage";
    public string Description => "Summarize knowledge coverage by entity type. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "coverage", []).Execute());
}

public sealed class ExternalDataTriageTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_external_data_triage";
    public string Description => "Classify external-data issues into stable categories and risk levels. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["format"] = new { type = "string", @enum = new[] { "json", "md" }, description = "Optional output format." }
        },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var format = KnowledgeToolArguments.TryGetString(arguments, "format", out var requestedFormat) ? requestedFormat : "json";
        return McpResponseLimiter.Limit(new TriageCommand(context.ConfigDir, context.AgentRoot, true, format).Execute());
    }
}

public sealed class PackFreshnessTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_pack_freshness";
    public string Description => "List knowledge pack freshness and metadata warnings. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "freshness", []).Execute());
}

public sealed class KnowledgeSourceFreshnessTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_source_freshness";
    public string Description => "List knowledge source freshness, snapshots, and review warnings. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "freshness", []).Execute());
}

public sealed class KnowledgeRefreshPlanTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_refresh_plan";
    public string Description => "Return the conservative online refresh plan for registered sources. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "refresh", new Dictionary<string, string> { ["action"] = "plan" }).Execute());
}

public sealed class KnowledgeSnapshotsTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_knowledge_snapshots";
    public string Description => "List sanitized source snapshots. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "snapshots", []).Execute());
}

public sealed class LearningCandidatesTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_learning_candidates";
    public string Description => "List review-first learning candidates. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "learn", new Dictionary<string, string> { ["action"] = "candidates" }).Execute());
}

public sealed class LearningReportTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_learning_report";
    public string Description => "Generate a markdown summary of learning candidates. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "learn", new Dictionary<string, string> { ["action"] = "report" }).Execute());
}

public sealed class AuthorizedSourceNotesTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_authorized_source_notes";
    public string Description => "Return authorization, provenance, and license notes for a source. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["id"] = new { type = "string", maxLength = 128, description = "Knowledge source ID." }
        },
        required = new[] { "id" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var dict = new Dictionary<string, string> { ["action"] = "explain" };
        if (KnowledgeToolArguments.TryGetString(arguments, "id", out var id))
            dict["id"] = id;

        return McpResponseLimiter.Limit(new KnowledgeCommand(context.ConfigDir, context.AgentRoot, "source", dict).Execute());
    }
}

public sealed class PlanCreateEntityTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_plan_create_entity";
    public string Description => "Generate a dry-run creation plan with knowledge context. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["entityType"] = new { type = "string", @enum = new[] { "item", "equipment", "monster", "npc", "map", "skill", "quest" } },
            ["id"] = new { type = "integer" },
            ["name"] = new { type = "string", maxLength = 256 },
            ["map"] = new { type = "string", maxLength = 64 },
            ["knowledgeLocalOnly"] = new { type = "boolean" },
            ["noLiveReference"] = new { type = "boolean" },
            ["liveSource"] = new { type = "string", @enum = new[] { "auto", "divine-pride", "ratemyserver" } }
        },
        required = new[] { "entityType" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var entityType = KnowledgeToolArguments.TryGetString(arguments, "entityType", out var parsedEntityType)
            ? parsedEntityType
            : "item";
        int? id = KnowledgeToolArguments.TryGetInt(arguments, "id", out var parsedId) ? parsedId : null;
        var name = KnowledgeToolArguments.TryGetString(arguments, "name", out var parsedName) ? parsedName : null;
        var map = KnowledgeToolArguments.TryGetString(arguments, "map", out var parsedMap) ? parsedMap : null;

        var options = new KnowledgeLookupOptions
        {
            WithKnowledge = true,
            KnowledgeLocalOnly = KnowledgeToolArguments.TryGetBool(arguments, "knowledgeLocalOnly", out var localOnly) && localOnly,
            NoLiveReference = KnowledgeToolArguments.TryGetBool(arguments, "noLiveReference", out var noLive) && noLive,
            LiveSource = KnowledgeToolArguments.TryGetString(arguments, "liveSource", out var liveSource) ? liveSource : "auto"
        };

        return McpResponseLimiter.Limit(new PlanCommand(context.ConfigDir, context.AgentRoot, entityType, id, name, map, options).Execute());
    }
}

public sealed class GenerateKnowledgeReportTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_generate_knowledge_report";
    public string Description => "Generate read-only knowledge or readiness markdown reports for humans. Read-only.";
    public object InputSchema => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            ["reportType"] = new { type = "string", @enum = new[] { "knowledge", "external-data", "readiness-summary" } }
        },
        required = new[] { "reportType" },
        additionalProperties = false
    };

    public JsonOutput Execute(JsonElement arguments)
    {
        var reportType = KnowledgeToolArguments.TryGetString(arguments, "reportType", out var parsedReportType)
            ? parsedReportType
            : "knowledge";

        return reportType switch
        {
            "external-data" => McpResponseLimiter.Limit(new ExternalDataReportCommand(context.AgentRoot, "md").Execute()),
            "readiness-summary" => McpResponseLimiter.Limit(new ReadinessSummaryReportCommand(context.AgentRoot, "md").Execute()),
            _ => McpResponseLimiter.Limit(new KnowledgeReportCommand(context.AgentRoot, "md").Execute())
        };
    }
}

public sealed class ApiReadinessExportTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_api_readiness_export";
    public string Description => "Export the stable API/UI readiness contract for this agent build. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new ApiReadinessExportCommand(context.AgentRoot).Execute());
}

public sealed class CanonCheckTool(McpToolContext context) : IMcpTool
{
    public string Name => "ragnaforge_canon_check";
    public string Description => "Run the global canon read-only policy checks. Read-only.";
    public object InputSchema => SchemaFactory.Empty();
    public JsonOutput Execute(JsonElement arguments) =>
        McpResponseLimiter.Limit(new CanonCommand(context.ConfigDir, context.AgentRoot).Execute());
}

internal static class KnowledgeToolArguments
{
    public static bool TryGetString(JsonElement arguments, string propertyName, out string value)
    {
        value = string.Empty;
        if (!arguments.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            return false;

        value = element.GetString() ?? string.Empty;
        return true;
    }

    public static bool TryGetInt(JsonElement arguments, string propertyName, out int value)
    {
        value = 0;
        if (!arguments.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Number)
            return false;

        return element.TryGetInt32(out value);
    }

    public static bool TryGetBool(JsonElement arguments, string propertyName, out bool value)
    {
        value = false;
        if (!arguments.TryGetProperty(propertyName, out var element) || element.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            return false;

        value = element.GetBoolean();
        return true;
    }
}
