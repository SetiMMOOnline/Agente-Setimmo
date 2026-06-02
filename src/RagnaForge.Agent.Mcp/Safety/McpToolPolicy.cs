using System.Text.Json;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Mcp.Safety;

public static class McpToolPolicy
{
    public static readonly ISet<string> AllowedTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ragnaforge_status",
        "ragnaforge_doctor",
        "ragnaforge_baseline",
        "ragnaforge_health",
        "ragnaforge_scan_project",
        "ragnaforge_config_get",
        "ragnaforge_config_validate",
        "ragnaforge_profile_list",
        "ragnaforge_profile_validate",
        "ragnaforge_index_entities",
        "ragnaforge_find_item",
        "ragnaforge_find_npc",
        "ragnaforge_find_monster",
        "ragnaforge_find_map",
        "ragnaforge_validate",
        "ragnaforge_dry_run_item",
        "ragnaforge_dry_run_npc",
        "ragnaforge_dry_run_monster",
        "ragnaforge_dry_run_map",
        "ragnaforge_diff",
        "ragnaforge_report",
        "ragnaforge_report_list",
        "ragnaforge_report_read",
        "ragnaforge_security_policy",
        "ragnaforge_triage",
        "ragnaforge_rollback_list",
        "ragnaforge_rollback_dry_run",
        "ragnaforge_review_code",
        "ragnaforge_fix_code",
        "ragnaforge_create_content",
        "ragnaforge_plan_implement",
        "ragnaforge_dry_run_implement",
        "ragnaforge_apply_implement",
        "ragnaforge_rollback_implement",
        "ragnaforge_cleanup_safe",
        "ragnaforge_operations_list",
        "ragnaforge_operations_show",
        "ragnaforge_operations_compare",
        "ragnaforge_production_status",
        "ragnaforge_production_audit",
        "ragnaforge_production_approve",
        "ragnaforge_production_apply",
        "ragnaforge_production_rollback",
        "ragnaforge_grf_list",
        "ragnaforge_grf_inspect",
        "ragnaforge_grf_dry_run_extract",
        "ragnaforge_grf_extract",
        "ragnaforge_field_test_run",
        "ragnaforge_knowledge_sources",
        "ragnaforge_knowledge_source_explain",
        "ragnaforge_knowledge_search",
        "ragnaforge_knowledge_explain",
        "ragnaforge_knowledge_entry",
        "ragnaforge_knowledge_schema",
        "ragnaforge_knowledge_validate",
        "ragnaforge_knowledge_conflicts",
        "ragnaforge_knowledge_coverage",
        "ragnaforge_external_data_triage",
        "ragnaforge_pack_freshness",
        "ragnaforge_knowledge_source_freshness",
        "ragnaforge_knowledge_refresh_plan",
        "ragnaforge_knowledge_snapshots",
        "ragnaforge_learning_candidates",
        "ragnaforge_learning_report",
        "ragnaforge_authorized_source_notes",
        "ragnaforge_plan_create_entity",
        "ragnaforge_generate_knowledge_report",
        "ragnaforge_api_readiness_export",
        "ragnaforge_canon_check"
    };

    public static readonly ISet<string> BlockedTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ragnaforge_apply",
        "ragnaforge_rollback_confirm"
    };

    public static readonly ISet<string> MutatingTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ragnaforge_dry_run_item",
        "ragnaforge_dry_run_npc",
        "ragnaforge_dry_run_monster",
        "ragnaforge_dry_run_map",
        "ragnaforge_fix_code",
        "ragnaforge_create_content",
        "ragnaforge_dry_run_implement",
        "ragnaforge_apply_implement",
        "ragnaforge_rollback_implement",
        "ragnaforge_cleanup_safe",
        "ragnaforge_production_approve",
        "ragnaforge_production_apply",
        "ragnaforge_production_rollback",
        "ragnaforge_grf_dry_run_extract",
        "ragnaforge_grf_extract",
        "ragnaforge_field_test_run"
    };

    public static bool IsAllowed(string toolName) => AllowedTools.Contains(toolName);

    public static bool IsBlocked(string toolName) => BlockedTools.Contains(toolName);

    public static bool IsReadOnly(string toolName) => !MutatingTools.Contains(toolName);

    public static JsonOutput BlockedApply() => OperationGovernanceResponses.CreateApplyUnavailable();

    public static JsonOutput BlockedRollback() => OperationGovernanceResponses.CreateRollbackUnavailable();

    public static JsonOutput UnknownTool(string toolName) => JsonOutput.Error("mcp", $"Unknown or disallowed MCP tool: {toolName}");

    public static JsonOutput? ValidateOperationId(string? operationId, string mode = "mcp")
    {
        return OperationIdValidator.IsValid(operationId)
            ? null
            : JsonOutput.Error(mode, "Invalid operationId format.");
    }

    public static JsonOutput? ValidateArguments(string mode, JsonElement arguments)
    {
        foreach (var value in EnumerateSensitivePathValues(arguments))
        {
            if (ContainsUnsafePathText(value))
                return JsonOutput.Error(mode, "Safety policy violation: Directory traversal or absolute path is blocked.");
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSensitivePathValues(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String && IsSensitivePathField(property.Name))
            {
                var text = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    yield return text!;
            }
        }
    }

    private static bool IsSensitivePathField(string fieldName) =>
        fieldName.Equals("targetPath", StringComparison.OrdinalIgnoreCase) ||
        fieldName.Equals("contentFilePath", StringComparison.OrdinalIgnoreCase) ||
        fieldName.Equals("path", StringComparison.OrdinalIgnoreCase) ||
        fieldName.Equals("uri", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsUnsafePathText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Contains("..", StringComparison.Ordinal))
            return true;

        if (text.StartsWith("\\\\", StringComparison.Ordinal) ||
            text.StartsWith("/", StringComparison.Ordinal))
            return true;

        return Path.IsPathRooted(text) || text.Contains(":/", StringComparison.Ordinal) || text.Contains(":\\", StringComparison.Ordinal);
    }
}
