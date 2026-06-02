using System.Text;
using System.Text.Json;
using RagnaForge.Agent.Core.Logging;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Mcp.Tools;

internal sealed class McpDryRunInputStore
{
    internal const int MaxPersistedBytes = 64 * 1024;
    internal static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _agentRoot;
    private readonly PathGuard _guard;

    public McpDryRunInputStore(string agentRoot, PathGuard guard)
    {
        _agentRoot = Path.GetFullPath(agentRoot);
        _guard = guard;
    }

    public string Persist(string operationId, JsonElement arguments)
    {
        var inputDir = Path.Combine(_agentRoot, "inputs", "dry-run");
        var inputPath = Path.Combine(inputDir, $"mcp-{operationId}.json");

        EnsureInsideAgentRoot(inputDir);
        EnsureInsideAgentRoot(inputPath);

        var writeCheck = _guard.EnsureCanWrite(inputPath);
        if (!writeCheck.IsAllowed)
            throw new InvalidOperationException(writeCheck.Reason ?? "MCP dry-run input write blocked.");

        Directory.CreateDirectory(inputDir);
        PruneExpiredInputs(inputDir);

        var json = JsonSerializer.Serialize(arguments, JsonOptions);
        var bytes = Encoding.UTF8.GetByteCount(json);
        if (bytes > MaxPersistedBytes)
            throw new InvalidOperationException(
                $"MCP dry-run payload exceeds the {MaxPersistedBytes} byte persistence limit.");

        File.WriteAllText(inputPath, json, Encoding.UTF8);

        var logger = new AgentLogger(_agentRoot);
        logger.EnsureDirectories();
        logger.Log("operations", new
        {
            kind = "mcp_dry_run_input_persisted",
            operationId,
            relativePath = Path.GetRelativePath(_agentRoot, inputPath),
            bytes,
            persistedAtUtc = DateTimeOffset.UtcNow,
            ttlHours = (int)Retention.TotalHours,
            controlledPersistence = true
        });

        return inputPath;
    }

    private void PruneExpiredInputs(string inputDir)
    {
        foreach (var file in Directory.EnumerateFiles(inputDir, "mcp-*.json", SearchOption.TopDirectoryOnly))
        {
            EnsureInsideAgentRoot(file);
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(file);
            if (age <= Retention)
                continue;

            File.Delete(file);
        }
    }

    private void EnsureInsideAgentRoot(string path)
    {
        var normalizedPath = PathGuard.Normalize(path);
        var normalizedAgentRoot = PathGuard.Normalize(_agentRoot);
        if (!PathGuard.IsContainedIn(normalizedPath, normalizedAgentRoot))
            throw new InvalidOperationException("MCP dry-run input path must stay inside agentRoot.");
    }
}
