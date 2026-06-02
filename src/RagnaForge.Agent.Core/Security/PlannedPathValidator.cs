using RagnaForge.Agent.Core.Configuration;

namespace RagnaForge.Agent.Core.Security;

/// <summary>
/// Validates paths planned during dry-run to ensure safety before saving manifests.
/// </summary>
public static class PlannedPathValidator
{
    private static readonly HashSet<string> BlockedWriteExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".grf",
        ".gpf",
        ".thor",
        ".spr",
        ".act",
        ".bmp",
        ".tga",
        ".rsw",
        ".gnd",
        ".gat",
        ".rsm",
        ".pal",
        ".lub",
        ".env",
        ".key",
        ".pem",
        ".pfx"
    };

    private static readonly HashSet<string> BlockedDirectorySegments = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "node_modules",
        "bin",
        "obj",
        "TestResults",
        "dist",
        "cache",
        "logs",
        "tmp"
    };

    /// <summary>
    /// Validates a planned file path against safety rules.
    /// Blocks traversal, illegal extensions, and ensures it stays within writable roots.
    /// </summary>
    public static List<string> Validate(string path, ProfileConfig profile)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add("Planned path cannot be empty.");
            return errors;
        }

        if (Path.IsPathFullyQualified(path) && !profile.WritableRoots.Any(root => IsContainedIn(path, root)))
            errors.Add("Security violation: Absolute path is outside configured writable roots.");

        if (ContainsTraversal(path))
            errors.Add("Security violation: Path traversal detected in planned path.");

        var extension = Path.GetExtension(path);
        if (BlockedWriteExtensions.Contains(extension))
            errors.Add($"Security violation: Writing '{extension}' files is blocked by scope policy.");

        if (Path.GetFileName(path).Equals("repositories.local.json", StringComparison.OrdinalIgnoreCase))
            errors.Add("Security violation: repositories.local.json is local-only and cannot be a planned target.");

        foreach (var segment in GetPathSegments(path))
        {
            if (BlockedDirectorySegments.Contains(segment))
            {
                errors.Add($"Security violation: planned path targets generated or local-only directory '{segment}'.");
                break;
            }
        }

        var fileName = Path.GetFileName(path);
        if (fileName.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
            errors.Add($"Invalid characters in planned filename: {fileName}");

        var guard = new PathGuard(profile.WritableRoots, profile.ReadOnlyRoots);
        var result = guard.EnsureCanWrite(path);
        if (!result.IsAllowed)
            errors.Add(result.Reason ?? "Path is not allowed by policy.");

        return errors;
    }

    private static bool ContainsTraversal(string path)
    {
        var segments = GetPathSegments(path);
        return segments.Any(segment => segment == "..");
    }

    private static IReadOnlyList<string> GetPathSegments(string path) =>
        path.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

    private static bool IsContainedIn(string child, string parent)
    {
        var fullChild = Path.GetFullPath(child).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullChild.Equals(fullParent, StringComparison.OrdinalIgnoreCase) ||
               fullChild.StartsWith(fullParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
