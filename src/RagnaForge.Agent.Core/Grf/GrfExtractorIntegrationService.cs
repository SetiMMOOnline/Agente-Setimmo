using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Logging;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Grf;

public sealed class GrfExtractionOperation
{
    [JsonPropertyName("operationId")] public string OperationId { get; set; } = string.Empty;
    [JsonPropertyName("createdAtUtc")] public DateTimeOffset CreatedAtUtc { get; set; }
    [JsonPropertyName("sourcePath")] public string SourcePath { get; set; } = string.Empty;
    [JsonPropertyName("outputRoot")] public string OutputRoot { get; set; } = string.Empty;
    [JsonPropertyName("mode")] public string Mode { get; set; } = "controlled-metadata-output";
    [JsonPropertyName("status")] public string Status { get; set; } = "planned";
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
}

public sealed class GrfExtractorIntegrationService
{
    private static readonly string[] SensitiveGameAssetExtensions =
    [
        ".grf", ".gpf", ".thor", ".spr", ".act", ".bmp", ".tga", ".rsw", ".gnd", ".gat", ".rsm", ".pal"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _configDir;
    private readonly string _agentRoot;

    public GrfExtractorIntegrationService(string configDir, string agentRoot)
    {
        _configDir = configDir;
        _agentRoot = Path.GetFullPath(agentRoot);
    }

    public JsonOutput List()
    {
        var context = LoadContext();
        var root = DiscoverExtractorRoot();
        var output = JsonOutput.Success("grf-list", "GRF tooling inventory generated.");
        output.Data = new
        {
            extractorRoot = root,
            extractorRootExists = root is not null && Directory.Exists(root),
            rootExecutables = root is null ? [] : Directory.GetFiles(root, "*.exe", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).OrderBy(x => x).ToArray(),
            sourceFolders = root is null ? [] : Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).OrderBy(x => x).ToArray(),
            configuredGrfRepositoryPath = context.Profile.GrfRepositoryPath,
            configuredGrfReadOnly = context.Guard.EnsureCanRead(context.Profile.GrfRepositoryPath).IsAllowed &&
                                    !context.Guard.EnsureCanWrite(context.Profile.GrfRepositoryPath).IsAllowed,
            availableContainers = root is null ? [] : FindContainers(root).Take(50).ToArray(),
            policy = BuildPolicy()
        };
        return output;
    }

    public JsonOutput Inspect(string source)
    {
        var resolved = ResolveContainer(source);
        if (resolved.Error is not null)
            return JsonOutput.Error("grf-inspect", resolved.Error);

        var file = new FileInfo(resolved.FullPath!);
        var output = JsonOutput.Success("grf-inspect", $"GRF container metadata inspected for {resolved.DisplayPath}.");
        output.Data = new
        {
            resolved.DisplayPath,
            extension = file.Extension,
            lengthBytes = file.Length,
            lastWriteTimeUtc = file.LastWriteTimeUtc,
            sourceRoot = resolved.SourceRoot,
            readOnly = true,
            extracted = false,
            entriesListed = false,
            note = "Metadata-only inspection. No GRF contents were extracted or modified.",
            policy = BuildPolicy()
        };
        return output;
    }

    public JsonOutput DryRunExtract(string source)
    {
        var resolved = ResolveContainer(source);
        if (resolved.Error is not null)
            return JsonOutput.Error("grf-dry-run-extract", resolved.Error);

        var operation = new GrfExtractionOperation
        {
            OperationId = JsonOutput.GenerateOperationId(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SourcePath = resolved.FullPath!,
            OutputRoot = Path.Combine(_agentRoot, "temp", "grf-operations"),
            Status = "planned"
        };
        operation.Warnings.Add("Controlled GRF extraction writes only metadata into an agent-owned temp operation folder.");
        operation.Warnings.Add("Original GRF/GPF/THOR files remain read-only and are never modified.");

        var dir = Path.Combine(_agentRoot, "logs", "grf-operations");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{operation.OperationId}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(operation, JsonOptions), Encoding.UTF8);

        new AgentLogger(_agentRoot).Log("grf", new
        {
            eventType = "grf_extract_dry_run",
            operation.OperationId,
            source = resolved.DisplayPath,
            operation.OutputRoot,
            atUtc = operation.CreatedAtUtc
        });

        var output = JsonOutput.Success("grf-dry-run-extract", "GRF extraction dry-run planned.");
        output.OperationId = operation.OperationId;
        output.Data = new
        {
            operation,
            operationPath = Path.GetRelativePath(_agentRoot, path),
            nextRequiredAction = "grf_extract_confirmed_in_controlled_output",
            policy = BuildPolicy()
        };
        output.NextRequiredAction = "grf_extract_confirmed_in_controlled_output";
        return output;
    }

    public JsonOutput Extract(string operationId, bool confirm)
    {
        if (!OperationIdValidator.IsValid(operationId))
            return JsonOutput.Error("grf-extract", "Invalid operationId format.");
        if (!confirm)
            return JsonOutput.Error("grf-extract", "Explicit confirmation is required. Usage: ragnaforge grf extract --operation <id> --confirm");

        var path = Path.Combine(_agentRoot, "logs", "grf-operations", $"{operationId}.json");
        if (!File.Exists(path))
            return JsonOutput.Error("grf-extract", $"GRF extraction operation '{operationId}' not found.");

        var operation = JsonSerializer.Deserialize<GrfExtractionOperation>(File.ReadAllText(path), JsonOptions);
        if (operation is null)
            return JsonOutput.Error("grf-extract", "Could not read GRF extraction operation.");
        if (!File.Exists(operation.SourcePath))
            return JsonOutput.Error("grf-extract", "Source GRF container no longer exists.");

        var outputDir = Path.Combine(operation.OutputRoot, operation.OperationId, "output");
        if (!PathGuard.IsContainedIn(Path.GetFullPath(outputDir), Path.GetFullPath(Path.Combine(_agentRoot, "temp", "grf-operations"))))
            return JsonOutput.Error("grf-extract", "GRF extraction output is outside the controlled agent temp root.");

        Directory.CreateDirectory(outputDir);
        var manifestPath = Path.Combine(outputDir, "EXTRACTION_MANIFEST.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
        {
            operation.OperationId,
            operation.SourcePath,
            generatedAtUtc = DateTimeOffset.UtcNow,
            controlledOutputOnly = true,
            originalContainerModified = false,
            realAssetPayloadCopied = false,
            note = "This controlled operation creates metadata evidence only; it does not unpack private assets."
        }, JsonOptions), Encoding.UTF8);

        operation.Status = "completed-controlled-metadata-output";
        File.WriteAllText(path, JsonSerializer.Serialize(operation, JsonOptions), Encoding.UTF8);

        new AgentLogger(_agentRoot).Log("grf", new
        {
            eventType = "grf_extract_controlled_metadata_output",
            operation.OperationId,
            outputDir,
            atUtc = DateTimeOffset.UtcNow
        });

        var output = JsonOutput.Success("grf-extract", "Controlled GRF operation completed without modifying the source container.");
        output.OperationId = operation.OperationId;
        output.Data = new
        {
            operation,
            outputDir,
            manifestPath,
            originalContainerModified = false,
            realAssetPayloadCopied = false,
            policy = BuildPolicy()
        };
        return output;
    }

    public JsonOutput Cleanup(string operationId, bool confirm)
    {
        if (!OperationIdValidator.IsValid(operationId))
            return JsonOutput.Error("grf-cleanup", "Invalid operationId format.");
        if (!confirm)
            return JsonOutput.Error("grf-cleanup", "Explicit confirmation is required. Usage: ragnaforge grf cleanup --operation <id> --confirm");

        var dir = Path.Combine(_agentRoot, "temp", "grf-operations", operationId);
        if (!Directory.Exists(dir))
            return JsonOutput.Success("grf-cleanup", "No controlled GRF output exists for this operation.");

        var full = Path.GetFullPath(dir);
        var allowedRoot = Path.GetFullPath(Path.Combine(_agentRoot, "temp", "grf-operations"));
        if (!PathGuard.IsContainedIn(full, allowedRoot))
            return JsonOutput.Error("grf-cleanup", "Cleanup target is outside the controlled agent temp root.");

        Directory.Delete(full, recursive: true);
        return JsonOutput.Success("grf-cleanup", "Controlled GRF output was cleaned from agent-owned temp storage.");
    }

    private ResolvedGrf ResolveContainer(string source)
    {
        if (string.IsNullOrWhiteSpace(source) || ContainsControlCharacters(source))
            return ResolvedGrf.Fail("GRF source is required and cannot contain control characters.");
        if (source.Contains("..", StringComparison.Ordinal))
            return ResolvedGrf.Fail("Path traversal is blocked for GRF sources.");

        var context = LoadContext();
        var root = DiscoverExtractorRoot();
        var candidates = new List<(string Root, string FullPath)>();

        if (Path.IsPathRooted(source))
        {
            candidates.Add(("absolute", Path.GetFullPath(source)));
        }
        else
        {
            if (root is not null)
                candidates.Add(("grf-extractor", Path.GetFullPath(Path.Combine(root, source))));
            if (!string.IsNullOrWhiteSpace(context.Profile.GrfRepositoryPath))
                candidates.Add(("configured-grf-repository", Path.GetFullPath(Path.Combine(context.Profile.GrfRepositoryPath, source))));
        }

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate.FullPath))
                continue;
            if (!IsAllowedContainerExtension(candidate.FullPath))
                return ResolvedGrf.Fail("Only .grf, .gpf and .thor containers are accepted.");

            var insideExtractor = root is not null && PathGuard.IsContainedIn(candidate.FullPath, root);
            var insideRepository = !string.IsNullOrWhiteSpace(context.Profile.GrfRepositoryPath) &&
                                   PathGuard.IsContainedIn(candidate.FullPath, Path.GetFullPath(context.Profile.GrfRepositoryPath));
            if (!insideExtractor && !insideRepository)
                return ResolvedGrf.Fail("GRF source must be inside GRF_Extractor or the configured read-only GRF repository.");

            var readCheck = context.Guard.EnsureCanRead(candidate.FullPath);
            if (!insideExtractor && !readCheck.IsAllowed)
                return ResolvedGrf.Fail("Configured GRF repository is not readable by current profile policy.");

            return ResolvedGrf.Ok(candidate.FullPath, Path.GetFileName(candidate.FullPath), candidate.Root);
        }

        return ResolvedGrf.Fail("GRF source was not found in allowed roots.");
    }

    private string? DiscoverExtractorRoot()
    {
        var explicitRoot = Environment.GetEnvironmentVariable("SETIMMO_GRF_EXTRACTOR_ROOT");
        if (!string.IsNullOrWhiteSpace(explicitRoot) && Directory.Exists(explicitRoot))
            return Path.GetFullPath(explicitRoot);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var candidate = Path.Combine(desktop, "GRF_Extractor");
        return Directory.Exists(candidate) ? Path.GetFullPath(candidate) : null;
    }

    private IEnumerable<object> FindContainers(string root)
    {
        var files = Enumerable.Empty<string>();
        try
        {
            files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(IsAllowedContainerExtension)
                .Where(file => !IsProtectedOutputOrDump(root, file));
        }
        catch
        {
            files = Directory.EnumerateFiles(root, "*.*", SearchOption.TopDirectoryOnly)
                .Where(IsAllowedContainerExtension);
        }

        foreach (var file in files.OrderBy(file => Path.GetRelativePath(root, file)))
        {
            var info = new FileInfo(file);
            yield return new
            {
                name = info.Name,
                relativePath = Path.GetRelativePath(root, info.FullName),
                info.Length,
                info.LastWriteTimeUtc
            };
        }
    }

    private static bool IsProtectedOutputOrDump(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var segments = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment =>
            segment.Equals("GRFs_Extraidas", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("Dump", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private LoadedContext LoadContext()
    {
        var loader = new ConfigLoader(_configDir);
        var paths = loader.LoadPathsConfig();
        var safety = loader.LoadSafetyConfig();
        var profile = ConfigLoader.GetActiveProfile(paths);
        return new LoadedContext(profile, new PathGuard(profile.WritableRoots, profile.ReadOnlyRoots, safety.BlockLubEditing));
    }

    private static bool IsAllowedContainerExtension(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".grf", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".gpf", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".thor", StringComparison.OrdinalIgnoreCase);
    }

    private static object BuildPolicy() => new
    {
        originalContainersReadOnly = true,
        privateAssetPayloadCopied = false,
        outputRoot = "agent temp/grf-operations only",
        blockedExtensions = SensitiveGameAssetExtensions,
        noLubEditing = true,
        noGenericShell = true
    };

    private static bool ContainsControlCharacters(string value) =>
        value.Any(ch => char.IsControl(ch) && ch is not '\r' and not '\n' and not '\t');

    private sealed record LoadedContext(ProfileConfig Profile, PathGuard Guard);

    private sealed class ResolvedGrf
    {
        public string? FullPath { get; private init; }
        public string DisplayPath { get; private init; } = string.Empty;
        public string SourceRoot { get; private init; } = string.Empty;
        public string? Error { get; private init; }

        public static ResolvedGrf Ok(string fullPath, string displayPath, string sourceRoot) =>
            new() { FullPath = fullPath, DisplayPath = displayPath, SourceRoot = sourceRoot };

        public static ResolvedGrf Fail(string error) => new() { Error = error };
    }
}
