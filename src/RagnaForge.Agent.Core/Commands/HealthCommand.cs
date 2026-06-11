using System.Text.Json;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Scanning;

namespace RagnaForge.Agent.Core.Commands;

public sealed class HealthCommand
{
    private readonly string _configDir;
    private readonly string _agentRoot;

    public HealthCommand(string configDir, string agentRoot)
    {
        _configDir = configDir;
        _agentRoot = agentRoot;
    }

    public JsonOutput Execute()
    {
        var output = JsonOutput.Success("health");

        try
        {
            var loader = new ConfigLoader(_configDir);
            var pathsConfig = loader.LoadPathsConfig();
            var safetyConfig = loader.LoadSafetyConfig();
            var profile = ConfigLoader.GetActiveProfile(pathsConfig);
            var fingerprint = ConfigFingerprint.Generate(pathsConfig, safetyConfig);

            output.ActiveProfile = pathsConfig.ActiveProfile;
            output.ConfigFingerprint = fingerprint;

            var status = new StatusCommand(_configDir, _agentRoot).Execute();
            var doctor = new DoctorCommand(_configDir, _agentRoot).Execute();
            var validate = new ValidateCommand(_configDir, _agentRoot).Execute();
            var canon = new GlobalCanonValidator(_agentRoot).Check();

            if (!doctor.Ok)
            {
                return CopyFailure(
                    output,
                    doctor,
                    "Agent health is degraded because doctor checks failed.");
            }

            var projectInspection = AgentCacheInspector.InspectProjectIndex(
                _agentRoot,
                pathsConfig.ActiveProfile,
                fingerprint,
                profile.RagnaforgeMainProjectPath);
            var entitiesInspection = AgentCacheInspector.InspectEntityIndex(
                _agentRoot,
                pathsConfig.ActiveProfile,
                fingerprint);

            var validateData = ToElement(validate.Data);
            var validatorsWouldAllowApply = GetNestedBool(
                validateData,
                "operationAuthorization",
                "validatorsWouldAllowApply");
            var validationSummary = new ValidationDecisionSummary
            {
                SafeForReadOnlyWork = GetBool(validateData, "safeForReadOnlyWork"),
                SafeForDryRun = GetBool(validateData, "safeForDryRun"),
                SafeForApply = false
            };
            var governance = OperationGovernanceProfiles.EvaluateValidated(
                "health",
                canon,
                validationSummary,
                applyEngineImplemented: true,
                rollbackEngineImplemented: true);

            output.Summary = "Operational health summary is ready.";
            output.SafeForAutomation = status.Ok && doctor.Ok && governance.SafeForReadOnlyWork;
            output.NextRequiredAction = governance.RecommendedAction;

            output.Data = new
            {
                mode = "health",
                agent = new
                {
                    version = AgentVersion.Current,
                    activeProfile = pathsConfig.ActiveProfile,
                    configFingerprint = fingerprint
                },
                project = new
                {
                    root = profile.RagnaforgeMainProjectPath,
                    filesIndexed = projectInspection.Document?.Stats.FilesIndexed ?? 0,
                    cacheTrusted = projectInspection.Details.CacheTrusted,
                    cacheStaleReason = projectInspection.Details.CacheStaleReason,
                    cacheFingerprint = projectInspection.Details.CacheFingerprint,
                    activeFingerprint = projectInspection.Details.ActiveFingerprint,
                    cacheProfile = projectInspection.Details.CacheProfile,
                    activeProfile = projectInspection.Details.ActiveProfile,
                    recommendedAction = projectInspection.Details.RecommendedAction
                },
                entities = new
                {
                    items = entitiesInspection.Document?.Stats.ItemsFound ?? 0,
                    monsters = entitiesInspection.Document?.Stats.MonstersFound ?? 0,
                    npcs = entitiesInspection.Document?.Stats.NpcsFound ?? 0,
                    maps = entitiesInspection.Document?.Stats.MapsFound ?? 0,
                    trustedCounts = entitiesInspection.Details.CacheTrusted,
                    cacheStaleReason = entitiesInspection.Details.CacheStaleReason,
                    cacheFingerprint = entitiesInspection.Details.CacheFingerprint,
                    activeFingerprint = entitiesInspection.Details.ActiveFingerprint,
                    cacheProfile = entitiesInspection.Details.CacheProfile,
                    activeProfile = entitiesInspection.Details.ActiveProfile,
                    recommendedAction = entitiesInspection.Details.RecommendedAction
                },
                validation = new
                {
                    issues = GetInt(validateData, "totalIssues"),
                    errors = GetInt(validateData, "errors"),
                    warnings = GetInt(validateData, "warnings"),
                    safeForReadOnlyWork = governance.SafeForReadOnlyWork,
                    safeForDryRun = governance.SafeForDryRun,
                    safeForApply = false,
                    canApply = false,
                    safeForProductionApply = governance.SafeForProductionApply
                },
                capabilities = new
                {
                    supportsApply = true,
                    supportsRollback = true,
                    supportsDryRun = true,
                    supportsProductionApply = true,
                    supportsCodexSupervised = true,
                    supportsSemanticPatch = true,
                    supportsContextPacks = true,
                    supportsOperationHistory = true,
                    supportsGrfOperations = true
                },
                operationAuthorization = new
                {
                    safeForApply = false,
                    canApply = false,
                    applyEnabled = false,
                    rollbackEnabled = false,
                    validatorsWouldAllowApply,
                    reason = "Health is not a concrete operation authorization."
                },
                safety = new
                {
                    applyBlocked = true,
                    rollbackRealBlocked = false,
                    grfReadOnly = true,
                    lubEditingBlocked = safetyConfig.BlockLubEditing,
                    canonEnabled = canon.CanonEnabled,
                    safeForReadOnlyWork = canon.SafeForReadOnlyWork,
                    safeForDryRun = canon.SafeForDryRun,
                    safeForApply = false
                },
                governance,
                recommendedAction = output.NextRequiredAction
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("health", ex.Message);
        }

        return output;
    }

    private static JsonOutput CopyFailure(JsonOutput target, JsonOutput source, string summary)
    {
        target.Ok = false;
        target.Summary = summary;
        target.Errors.AddRange(source.Errors);
        target.Warnings.AddRange(source.Warnings);
        target.SafeForAutomation = false;
        target.NextRequiredAction = source.NextRequiredAction;
        target.Data = source.Data;
        return target;
    }

    private static JsonElement ToElement(object? data) =>
        data is null ? JsonDocument.Parse("{}").RootElement : JsonSerializer.SerializeToElement(data);

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var property) &&
               property.TryGetInt32(out var value)
            ? value
            : 0;
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind is JsonValueKind.True or JsonValueKind.False &&
               property.GetBoolean();
    }

    private static bool GetNestedBool(JsonElement element, string objectPropertyName, string boolPropertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(objectPropertyName, out var nested) &&
               nested.ValueKind == JsonValueKind.Object &&
               nested.TryGetProperty(boolPropertyName, out var property) &&
               property.ValueKind is JsonValueKind.True or JsonValueKind.False &&
               property.GetBoolean();
    }
}
