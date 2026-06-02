namespace RagnaForge.Agent.Core.Canon;

public sealed class GlobalCanonPolicy
{
    public bool CanonEnabled { get; init; } = true;
    public bool SafeForApply => false;

    public IReadOnlySet<string> BlockedOperationTokens { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "apply",
        "rollback --confirm",
        "--confirm APPLY",
        "--confirm ROLLBACK",
        "grf apply",
        "edit .lub",
        "generic shell",
        "free shell"
    };

    public IReadOnlySet<string> DestructiveCommandTokens { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "rm -rf",
        "rmdir",
        "rd /s",
        "del /s",
        "erase",
        "format",
        "diskpart",
        "Remove-Item -Recurse",
        "Directory.Delete",
        "File.Delete"
    };

    public IReadOnlySet<string> SensitiveFileNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".env",
        "repositories.local.json",
        "paths.local.json",
        "secrets.json"
    };

    public IReadOnlySet<string> SensitiveExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".grf",
        ".gpf",
        ".thor",
        ".spr",
        ".act",
        ".rsw",
        ".gnd",
        ".gat",
        ".rsm",
        ".pal"
    };

    public IReadOnlySet<string> ArtifactDirectoryNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        "TestResults",
        "node_modules",
        "dumps",
        "downloaded",
        "raw-html",
        "external-cache"
    };

    public static GlobalCanonPolicy CreateDefault() => new();

    public bool IsBlockedOperation(string value) =>
        ContainsAny(value, BlockedOperationTokens);

    public bool IsDestructiveCommand(string value) =>
        ContainsAny(value, DestructiveCommandTokens);

    public bool IsSensitiveFile(string path)
    {
        var fileName = Path.GetFileName(path);
        if (SensitiveFileNames.Contains(fileName))
            return true;

        if (fileName.EndsWith(".local.json", StringComparison.OrdinalIgnoreCase))
            return true;

        return SensitiveExtensions.Contains(Path.GetExtension(path));
    }

    public bool IsForbiddenArtifactDirectory(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return ArtifactDirectoryNames.Contains(name);
    }

    private static bool ContainsAny(string value, IEnumerable<string> tokens) =>
        tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
}
