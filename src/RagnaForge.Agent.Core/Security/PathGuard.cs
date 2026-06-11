namespace RagnaForge.Agent.Core.Security;

/// <summary>
/// Protects the filesystem by enforcing writable/read-only roots,
/// blocking path traversal, blocking .lub editing, and ensuring
/// GRF repository stays read-only.
///
/// All paths come from the active profile and are never hardcoded.
/// </summary>
public sealed class PathGuard
{
    private readonly List<string> _writableRoots;
    private readonly List<string> _readOnlyRoots;
    private readonly bool _blockLubEditing;

    public PathGuard(
        IEnumerable<string> writableRoots,
        IEnumerable<string> readOnlyRoots,
        bool blockLubEditing = true)
    {
        _writableRoots = (writableRoots ?? []).Select(Normalize).ToList();
        _readOnlyRoots = (readOnlyRoots ?? []).Select(Normalize).ToList();
        _blockLubEditing = blockLubEditing;
    }

    /// <summary>
    /// Normalize a path: resolve to full path, trim trailing separators.
    /// Handles Windows paths with spaces, accents, apostrophes and backslashes.
    /// </summary>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Check if a path contains traversal components.
    /// </summary>
    public static bool ContainsTraversal(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;

        var segments = path.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(s => s == "..");
    }

    /// <summary>
    /// Check if pathA contains pathB (pathB is inside pathA), using normalized containment.
    /// </summary>
    public static bool IsContainedIn(string child, string parent)
    {
        return child.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
               child.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if the normalized path is inside any of the writable roots.
    /// </summary>
    public bool IsInsideWritableRoot(string path)
    {
        var normalized = Normalize(path);
        return _writableRoots.Any(root => IsContainedIn(normalized, root));
    }

    /// <summary>
    /// Check if the normalized path is inside any of the read-only roots.
    /// </summary>
    public bool IsInsideReadOnlyRoot(string path)
    {
        var normalized = Normalize(path);
        return _readOnlyRoots.Any(root => IsContainedIn(normalized, root));
    }

    /// <summary>
    /// Ensure a path can be read (must be inside writable or read-only roots).
    /// </summary>
    public PathGuardResult EnsureCanRead(string path)
    {
        if (ContainsTraversal(path))
            return PathGuardResult.Blocked($"Path contains traversal: {path}");

        if (!IsInsideWritableRoot(path) && !IsInsideReadOnlyRoot(path))
            return PathGuardResult.Blocked($"Path is outside all allowed roots: {path}");

        return PathGuardResult.Allowed();
    }

    /// <summary>
    /// Ensure a path can be written to.
    /// Blocks: traversal, read-only roots, outside writable roots, .lub files.
    /// </summary>
    public PathGuardResult EnsureCanWrite(string path)
    {
        if (ContainsTraversal(path))
            return PathGuardResult.Blocked($"Path contains traversal: {path}");

        if (IsInsideReadOnlyRoot(path))
            return PathGuardResult.Blocked($"Path is inside a read-only root: {path}");

        if (!IsInsideWritableRoot(path))
            return PathGuardResult.Blocked($"Path is outside all writable roots: {path}");

        var fileTypeCheck = EnsureCanWriteFileType(path);
        if (!fileTypeCheck.IsAllowed)
            return fileTypeCheck;

        return PathGuardResult.Allowed();
    }

    /// <summary>
    /// Ensure the file type is allowed for writing.
    /// Blocks .lub files when blockLubEditing is enabled.
    /// </summary>
    public PathGuardResult EnsureCanWriteFileType(string path)
    {
        if (_blockLubEditing &&
            Path.GetExtension(path).Equals(".lub", StringComparison.OrdinalIgnoreCase))
        {
            return PathGuardResult.Blocked($"Editing .lub files is blocked: {path}");
        }

        return PathGuardResult.Allowed();
    }

    /// <summary>
    /// Validate that the active profile has safe path configuration.
    /// A writable root may contain a narrower read-only protected island.
    /// Exact overlap remains invalid, and GRF must remain protected by read-only roots.
    /// </summary>
    public static List<string> EnsureProfileIsSafe(
        Configuration.ProfileConfig profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var issues = new List<string>();
        var normalizedGrf = string.IsNullOrWhiteSpace(profile.GrfRepositoryPath)
            ? string.Empty
            : Normalize(profile.GrfRepositoryPath);

        var normalizedWritable = profile.WritableRoots.Select(Normalize).ToList();
        var normalizedReadOnly = profile.ReadOnlyRoots.Select(Normalize).ToList();

        if (!string.IsNullOrEmpty(normalizedGrf) &&
            !normalizedReadOnly.Any(r => IsContainedIn(normalizedGrf, r)))
        {
            issues.Add($"grfRepositoryPath '{profile.GrfRepositoryPath}' is not in readOnlyRoots.");
        }

        var grfProtectedByReadOnly = !string.IsNullOrEmpty(normalizedGrf) &&
                                     normalizedReadOnly.Any(r => IsContainedIn(normalizedGrf, r));

        if (!string.IsNullOrEmpty(normalizedGrf))
        {
            foreach (var wr in normalizedWritable)
            {
                if (normalizedGrf.Equals(wr, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(
                        $"grfRepositoryPath '{profile.GrfRepositoryPath}' exactly overlaps writableRoot '{wr}'. This must NOT happen.");
                    break;
                }

                if (IsContainedIn(normalizedGrf, wr) && !grfProtectedByReadOnly)
                {
                    issues.Add(
                        $"grfRepositoryPath '{profile.GrfRepositoryPath}' is contained within writableRoot '{wr}'. This must NOT happen.");
                    break;
                }
            }
        }

        foreach (var wr in normalizedWritable)
        {
            foreach (var ro in normalizedReadOnly)
            {
                if (wr.Equals(ro, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(
                        $"writableRoot '{wr}' exactly overlaps readOnlyRoot '{ro}'. This overlap is unsafe.");
                }

                if (!wr.Equals(ro, StringComparison.OrdinalIgnoreCase) && IsContainedIn(wr, ro))
                {
                    issues.Add(
                        $"readOnlyRoot '{ro}' contains writableRoot '{wr}'. This overlap is conflicting.");
                }
            }
        }

        var allPaths = new[]
        {
            profile.RagnaforgeMainProjectPath,
            profile.RathenaPath,
            profile.PatchPath,
            profile.GrfRepositoryPath,
            profile.GrfEditorPath
        };

        foreach (var p in allPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (ContainsTraversal(p))
                issues.Add($"Path contains traversal: {p}");
        }

        return issues;
    }

    /// <summary>
    /// Convenience: validate that grfRepositoryPath is read-only.
    /// </summary>
    public static List<string> EnsureGrfRepositoryIsReadOnly(
        Configuration.ProfileConfig profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.GrfRepositoryPath))
        {
            issues.Add("grfRepositoryPath is not configured.");
            return issues;
        }

        var normalizedGrf = Normalize(profile.GrfRepositoryPath);
        var normalizedReadOnly = profile.ReadOnlyRoots.Select(Normalize).ToList();

        if (!normalizedReadOnly.Any(r => IsContainedIn(normalizedGrf, r)))
            issues.Add($"grfRepositoryPath '{profile.GrfRepositoryPath}' is not in readOnlyRoots.");

        return issues;
    }
}

/// <summary>
/// Result of a PathGuard check.
/// </summary>
public sealed class PathGuardResult
{
    public bool IsAllowed { get; private init; }
    public string? Reason { get; private init; }

    public static PathGuardResult Allowed() => new() { IsAllowed = true };
    public static PathGuardResult Blocked(string reason) => new() { IsAllowed = false, Reason = reason };
}
