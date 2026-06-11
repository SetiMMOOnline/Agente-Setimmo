using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Commands;

/// <summary>
/// Implements the 'ragnaforge status --json' command.
/// Read-only: loads configs, checks paths, reports state. Never modifies files.
/// </summary>
public sealed class StatusCommand
{
    private readonly string _configDir;
    private readonly string? _runtimeAgentRoot;

    public StatusCommand(string configDir, string? runtimeAgentRoot = null)
    {
        _configDir = configDir;
        _runtimeAgentRoot = string.IsNullOrWhiteSpace(runtimeAgentRoot)
            ? null
            : Path.GetFullPath(runtimeAgentRoot);
    }

    public JsonOutput Execute()
    {
        var output = JsonOutput.Success("status");
        var pathStatuses = new List<object>();

        try
        {
            var loader = new ConfigLoader(_configDir);
            var agentConfig = loader.LoadAgentConfig();
            var pathsConfig = loader.LoadPathsConfig();
            var safetyConfig = loader.LoadSafetyConfig();
            var profile = ConfigLoader.GetActiveProfile(pathsConfig);
            var fingerprint = ConfigFingerprint.Generate(pathsConfig, safetyConfig);
            var configuredAgentRoot = PathGuard.Normalize(pathsConfig.AgentRoot);
            var effectiveAgentRoot = _runtimeAgentRoot ?? configuredAgentRoot;
            var agentRootMismatch = !string.IsNullOrWhiteSpace(_runtimeAgentRoot) &&
                !string.Equals(configuredAgentRoot, effectiveAgentRoot, StringComparison.OrdinalIgnoreCase);

            output.ActiveProfile = pathsConfig.ActiveProfile;
            output.ConfigFingerprint = fingerprint;
            output.Summary = $"Agente Setimmo '{agentConfig.AgentName}' - profile: {pathsConfig.ActiveProfile}";

            if (agentRootMismatch)
            {
                output.Warnings.Add(
                    $"Configured agentRoot '{configuredAgentRoot}' differs from runtime agentRoot '{effectiveAgentRoot}'.");
            }

            var pathChecks = new Dictionary<string, string>
            {
                ["agentRoot"] = effectiveAgentRoot,
                ["ragnaforgeMainProjectPath"] = profile.RagnaforgeMainProjectPath,
                ["rathenaPath"] = profile.RathenaPath,
                ["patchPath"] = profile.PatchPath,
                ["grfRepositoryPath"] = profile.GrfRepositoryPath,
                ["grfEditorPath"] = profile.GrfEditorPath
            };

            var guard = new PathGuard(profile.WritableRoots, profile.ReadOnlyRoots,
                safetyConfig.BlockLubEditing);

            foreach (var (name, path) in pathChecks)
            {
                var exists = !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
                var isReadOnly = !string.IsNullOrWhiteSpace(path) && guard.IsInsideReadOnlyRoot(path);
                var isWritable = !string.IsNullOrWhiteSpace(path) &&
                                 guard.IsInsideWritableRoot(path) &&
                                 !isReadOnly;

                pathStatuses.Add(new
                {
                    name,
                    path,
                    exists,
                    writable = isWritable,
                    readOnly = isReadOnly
                });

                if (!exists && !string.IsNullOrWhiteSpace(path))
                    output.Warnings.Add($"{name}: directory does not exist at '{path}'");
            }

            var grfIssues = PathGuard.EnsureGrfRepositoryIsReadOnly(profile);
            if (grfIssues.Count > 0)
            {
                output.Warnings.AddRange(grfIssues);
            }

            var cacheDir = Path.Combine(effectiveAgentRoot, "cache", "agent");
            var cacheExists = Directory.Exists(cacheDir);
            var cacheIndexPath = Path.Combine(cacheDir, "project_index.json");
            var cacheIndexExists = File.Exists(cacheIndexPath);

            var cacheMatchesProfile = false;
            if (cacheIndexExists)
            {
                try
                {
                    var cacheJson = File.ReadAllText(cacheIndexPath);
                    cacheMatchesProfile = cacheJson.Contains(fingerprint, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    // Cache is optional; stale or unreadable cache only affects diagnostics.
                }
            }

            output.Data = new
            {
                agentName = agentConfig.AgentName,
                mode = agentConfig.Mode,
                primaryOperators = agentConfig.PrimaryOperators,
                config = new
                {
                    configuredAgentRoot,
                    effectiveAgentRoot,
                    runtimeAgentRoot = _runtimeAgentRoot,
                    agentRootMismatch
                },
                activeProfile = pathsConfig.ActiveProfile,
                dbMode = profile.DbMode,
                configFingerprint = fingerprint,
                paths = pathStatuses,
                grfProtected = grfIssues.Count == 0,
                lubEditingBlocked = safetyConfig.BlockLubEditing,
                cache = new
                {
                    directory = cacheDir,
                    directoryExists = cacheExists,
                    indexExists = cacheIndexExists,
                    matchesActiveFingerprint = cacheMatchesProfile
                },
                safety = new
                {
                    operationProfile = safetyConfig.GetNormalizedOperationProfile(),
                    operationProfileDescription = safetyConfig.DescribeOperationProfile(),
                    codexReviewThreshold = safetyConfig.GetCodexReviewThreshold(),
                    autoApplyThreshold = safetyConfig.GetAutoApplyThreshold(),
                    safetyConfig.RequireDryRunBeforeApply,
                    safetyConfig.RequireDiffBeforeApply,
                    safetyConfig.RequireExplicitConfirmation,
                    safetyConfig.BackupBeforeApply,
                    safetyConfig.BlockOriginalGrfWrite,
                    safetyConfig.BlockLubEditing,
                    safetyConfig.InvalidateCacheOnPathChange,
                    safetyConfig.CacheMustMatchActiveProfile
                }
            };

            if (output.Warnings.Count > 0)
                output.SafeForAutomation = false;
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("status", ex.Message);
        }

        return output;
    }
}
