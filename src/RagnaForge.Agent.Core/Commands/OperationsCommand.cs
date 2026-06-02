using System.Text.Json;
using RagnaForge.Agent.Core.Entities;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Commands;

public sealed class OperationsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _agentRoot;
    private readonly string _subcommand;
    private readonly string? _operationId;
    private readonly string? _leftId;
    private readonly string? _rightId;

    public OperationsCommand(string agentRoot, string subcommand, string? operationId, string? leftId, string? rightId)
    {
        _agentRoot = agentRoot;
        _subcommand = string.IsNullOrWhiteSpace(subcommand) ? "list" : subcommand;
        _operationId = operationId;
        _leftId = leftId;
        _rightId = rightId;
    }

    public JsonOutput Execute() =>
        _subcommand.ToLowerInvariant() switch
        {
            "list" => List(),
            "show" => Show(_operationId),
            "compare" => Compare(_leftId, _rightId),
            _ => JsonOutput.Error("operations", "Usage: ragnaforge operations list | show --operation <id> | compare --left <id> --right <id>")
        };

    private JsonOutput List()
    {
        var output = JsonOutput.Success("operations-list", "Operation history loaded.");
        var dir = Path.Combine(_agentRoot, "logs", "operations");
        var operations = new List<object>();
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(200))
            {
                if (TryReadManifest(file, out var manifest))
                {
                    operations.Add(new
                    {
                        manifest!.OperationId,
                        manifest.OperationType,
                        manifest.CreatedAtUtc,
                        manifest.Status,
                        manifest.Applied,
                        affectedFiles = manifest.AffectedFiles.Count,
                        warnings = manifest.Warnings.Count,
                        errors = manifest.Errors.Count,
                        path = Path.GetRelativePath(_agentRoot, file)
                    });
                }
            }
        }

        output.Data = new { operations, total = operations.Count };
        return output;
    }

    private JsonOutput Show(string? operationId)
    {
        if (!OperationIdValidator.IsValid(operationId))
            return JsonOutput.Error("operations-show", "Invalid or missing operationId.");

        var path = Path.Combine(_agentRoot, "logs", "operations", $"{operationId}.json");
        if (!File.Exists(path))
            return JsonOutput.Error("operations-show", $"Operation '{operationId}' not found.");

        if (!TryReadManifest(path, out var manifest))
            return JsonOutput.Error("operations-show", $"Operation '{operationId}' could not be read.");

        var output = JsonOutput.Success("operations-show", $"Operation {operationId} loaded.");
        output.OperationId = operationId!;
        output.Data = new
        {
            manifest,
            manifestPath = Path.GetRelativePath(_agentRoot, path),
            diffExists = AgentOwnedFileExists(manifest!.DiffPath),
            rollbackExists = AgentOwnedFileExists(manifest.RollbackPlanPath)
        };
        return output;
    }

    private JsonOutput Compare(string? leftId, string? rightId)
    {
        if (!OperationIdValidator.IsValid(leftId) || !OperationIdValidator.IsValid(rightId))
            return JsonOutput.Error("operations-compare", "Both --left and --right must be valid operation IDs.");

        var leftPath = Path.Combine(_agentRoot, "logs", "operations", $"{leftId}.json");
        var rightPath = Path.Combine(_agentRoot, "logs", "operations", $"{rightId}.json");
        if (!TryReadManifest(leftPath, out var left) || !TryReadManifest(rightPath, out var right))
            return JsonOutput.Error("operations-compare", "One or both operations could not be read.");

        var leftFiles = left!.AffectedFiles.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightFiles = right!.AffectedFiles.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var output = JsonOutput.Success("operations-compare", "Operation comparison generated.");
        output.Data = new
        {
            left = new { left.OperationId, left.OperationType, left.Status, left.Applied, affectedFiles = left.AffectedFiles.Count },
            right = new { right.OperationId, right.OperationType, right.Status, right.Applied, affectedFiles = right.AffectedFiles.Count },
            sharedFiles = leftFiles.Intersect(rightFiles, StringComparer.OrdinalIgnoreCase).ToArray(),
            onlyLeft = leftFiles.Except(rightFiles, StringComparer.OrdinalIgnoreCase).ToArray(),
            onlyRight = rightFiles.Except(leftFiles, StringComparer.OrdinalIgnoreCase).ToArray(),
            statusChanged = !string.Equals(left.Status, right.Status, StringComparison.OrdinalIgnoreCase),
            appliedChanged = left.Applied != right.Applied
        };
        return output;
    }

    private bool AgentOwnedFileExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || PathGuard.ContainsTraversal(relativePath))
            return false;

        var full = Path.GetFullPath(Path.Combine(_agentRoot, relativePath));
        return PathGuard.IsContainedIn(full, _agentRoot) && File.Exists(full);
    }

    private static bool TryReadManifest(string path, out OperationManifest? manifest)
    {
        manifest = null;
        if (!File.Exists(path))
            return false;

        try
        {
            manifest = JsonSerializer.Deserialize<OperationManifest>(File.ReadAllText(path), JsonOptions);
            return manifest is not null;
        }
        catch
        {
            return false;
        }
    }
}
