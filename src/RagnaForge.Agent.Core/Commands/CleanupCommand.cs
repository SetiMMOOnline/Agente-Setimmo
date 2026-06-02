using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Commands;

/// <summary>
/// Removes regenerable local artifacts inside agentRoot only.
/// Never touches source, docs, curated knowledge or external project paths.
/// </summary>
public sealed class CleanupCommand
{
    private readonly string _agentRoot;
    private readonly bool _includeLogs;
    private readonly bool _includeCache;
    private readonly bool _includeInputs;
    private readonly string _runtimeBaseDirectory;

    public CleanupCommand(string agentRoot, bool includeLogs, bool includeCache, bool includeInputs)
    {
        _agentRoot = Path.GetFullPath(agentRoot);
        _includeLogs = includeLogs;
        _includeCache = includeCache;
        _includeInputs = includeInputs;
        _runtimeBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
    }

    public JsonOutput Execute()
    {
        var output = JsonOutput.Success("cleanup");
        var removed = new List<string>();

        try
        {
            RemoveDirectories("src", "bin", removed);
            RemoveDirectories("src", "obj", removed);
            RemoveDirectories("tests", "bin", removed);
            RemoveDirectories("tests", "obj", removed);
            RemoveDirectories(".", "TestResults", removed);

            RemoveFiles(_agentRoot, "*.trx", removed);
            RemoveFiles(_agentRoot, "*.tsbuildinfo", removed);
            RemoveFileIfExists(Path.Combine(_agentRoot, "tests_output.txt"), removed);

            if (_includeCache)
                RemoveGeneratedFiles(Path.Combine(_agentRoot, "cache"), removed);

            if (_includeLogs)
                RemoveGeneratedFiles(Path.Combine(_agentRoot, "logs"), removed);

            if (_includeInputs)
                RemoveFiles(Path.Combine(_agentRoot, "inputs", "dry-run"), "mcp-*.json", removed);

            output.Summary = $"Cleanup completed - removed {removed.Count} regenerable artifact(s).";
            output.Data = new
            {
                includeLogs = _includeLogs,
                includeCache = _includeCache,
                includeInputs = _includeInputs,
                removedCount = removed.Count,
                removed
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("cleanup", ex.Message);
        }

        return output;
    }

    private void RemoveDirectories(string rootRelativePath, string directoryName, List<string> removed)
    {
        var basePath = ResolveInsideAgentRoot(rootRelativePath);
        if (!Directory.Exists(basePath))
            return;

        foreach (var dir in Directory.EnumerateDirectories(basePath, directoryName, SearchOption.AllDirectories))
        {
            var fullPath = ResolveInsideAgentRoot(Path.GetRelativePath(_agentRoot, dir));
            if (ContainsRuntimeDirectory(fullPath))
                continue;

            Directory.Delete(fullPath, true);
            removed.Add(Path.GetRelativePath(_agentRoot, fullPath));
        }
    }

    private void RemoveFiles(string rootPath, string filter, List<string> removed)
    {
        if (!Directory.Exists(rootPath))
            return;

        foreach (var file in Directory.EnumerateFiles(rootPath, filter, SearchOption.AllDirectories))
        {
            RemoveFileIfExists(file, removed);
        }
    }

    private void RemoveGeneratedFiles(string rootPath, List<string> removed)
    {
        if (!Directory.Exists(rootPath))
            return;

        foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFileName(file), ".gitkeep", StringComparison.OrdinalIgnoreCase))
                continue;

            RemoveFileIfExists(file, removed);
        }
    }

    private void RemoveFileIfExists(string path, List<string> removed)
    {
        if (!File.Exists(path))
            return;

        var fullPath = ResolveInsideAgentRoot(Path.GetRelativePath(_agentRoot, path));
        File.Delete(fullPath);
        removed.Add(Path.GetRelativePath(_agentRoot, fullPath));
    }

    private string ResolveInsideAgentRoot(string relativeOrAbsolutePath)
    {
        var candidate = Path.IsPathRooted(relativeOrAbsolutePath)
            ? Path.GetFullPath(relativeOrAbsolutePath)
            : Path.GetFullPath(Path.Combine(_agentRoot, relativeOrAbsolutePath));
        var normalizedRoot = PathGuard.Normalize(_agentRoot);
        var normalizedCandidate = PathGuard.Normalize(candidate);

        if (!PathGuard.IsContainedIn(normalizedCandidate, normalizedRoot))
            throw new InvalidOperationException($"Cleanup path escapes agentRoot: {candidate}");

        return candidate;
    }

    private bool ContainsRuntimeDirectory(string candidateDirectory)
    {
        var normalizedCandidate = PathGuard.Normalize(candidateDirectory);
        var normalizedRuntime = PathGuard.Normalize(_runtimeBaseDirectory);
        return PathGuard.IsContainedIn(normalizedRuntime, normalizedCandidate);
    }
}
