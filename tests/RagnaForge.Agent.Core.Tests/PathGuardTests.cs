using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Tests;

/// <summary>
/// Tests for PathGuard. Verifies filesystem protection is enforced
/// using temporary fixtures only.
/// </summary>
public class PathGuardTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _writableDir;
    private readonly string _readOnlyDir;
    private readonly PathGuard _guard;

    public PathGuardTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ragnaforge_test_{Guid.NewGuid():N}");
        _writableDir = Path.Combine(_tempDir, "writable");
        _readOnlyDir = Path.Combine(_tempDir, "readonly_grfs");

        Directory.CreateDirectory(_writableDir);
        Directory.CreateDirectory(_readOnlyDir);

        _guard = new PathGuard(
            [_writableDir],
            [_readOnlyDir],
            blockLubEditing: true);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void EnsureCanWrite_BlocksReadOnlyRoot()
    {
        var path = Path.Combine(_readOnlyDir, "data.grf");
        var result = _guard.EnsureCanWrite(path);

        Assert.False(result.IsAllowed);
        Assert.Contains("read-only", result.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCanWrite_BlocksTraversal()
    {
        var path = Path.Combine(_writableDir, "..", "escape", "file.txt");
        var result = _guard.EnsureCanWrite(path);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void Normalize_AcceptsValidWindowsPath()
    {
        var result = PathGuard.Normalize(_writableDir);
        Assert.False(string.IsNullOrEmpty(result));
        Assert.DoesNotContain("..", result);
    }

    [Fact]
    public void Normalize_AcceptsPathsWithSpaces()
    {
        var pathWithSpaces = Path.Combine(_tempDir, "path with spaces");
        Directory.CreateDirectory(pathWithSpaces);

        var result = PathGuard.Normalize(pathWithSpaces);
        Assert.Contains("path with spaces", result);
    }

    [Fact]
    public void Normalize_AcceptsPathsWithAccents()
    {
        var pathWithAccents = Path.Combine(_tempDir, "conteudo-com-acento");
        Directory.CreateDirectory(pathWithAccents);

        var result = PathGuard.Normalize(pathWithAccents);
        Assert.Contains("conteudo-com-acento", result);
    }

    [Fact]
    public void Normalize_AcceptsPathsWithApostrophes()
    {
        var pathWithApostrophe = Path.Combine(_tempDir, "GRF'S");
        Directory.CreateDirectory(pathWithApostrophe);

        var result = PathGuard.Normalize(pathWithApostrophe);
        Assert.Contains("GRF'S", result);
    }

    [Fact]
    public void EnsureCanWriteFileType_BlocksLub()
    {
        var lubPath = Path.Combine(_writableDir, "itemInfo.lub");
        var result = _guard.EnsureCanWriteFileType(lubPath);

        Assert.False(result.IsAllowed);
        Assert.Contains(".lub", result.Reason!);
    }

    [Fact]
    public void EnsureCanWrite_AllowsWritableRoot()
    {
        var path = Path.Combine(_writableDir, "test.yml");
        var result = _guard.EnsureCanWrite(path);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EnsureCanWrite_BlocksOutsideAllRoots()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), "some_other_dir", "file.txt");
        var result = _guard.EnsureCanWrite(outsidePath);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void ContainsTraversal_DetectsDoubleDot()
    {
        Assert.True(PathGuard.ContainsTraversal(@"C:\Users\..\secret"));
        Assert.True(PathGuard.ContainsTraversal(@"data\..\escape"));
        Assert.False(PathGuard.ContainsTraversal(@"C:\Users\Normal\Path"));
    }

    [Fact]
    public void EnsureCanRead_AllowsBothRoots()
    {
        var writablePath = Path.Combine(_writableDir, "file.txt");
        var readOnlyPath = Path.Combine(_readOnlyDir, "data.grf");

        Assert.True(_guard.EnsureCanRead(writablePath).IsAllowed);
        Assert.True(_guard.EnsureCanRead(readOnlyPath).IsAllowed);
    }

    [Fact]
    public void EnsureProfileIsSafe_CatchesGrfInWritableRoots()
    {
        var profile = new ProfileConfig
        {
            RagnaforgeMainProjectPath = _writableDir,
            RathenaPath = _writableDir,
            PatchPath = _writableDir,
            GrfRepositoryPath = _readOnlyDir,
            GrfEditorPath = _writableDir,
            WritableRoots = [_writableDir, _readOnlyDir],
            ReadOnlyRoots = [_readOnlyDir]
        };

        var issues = PathGuard.EnsureProfileIsSafe(profile);
        Assert.Contains(issues, i => i.Contains("exactly overlaps", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnsureProfileIsSafe_CatchesGrfNotInReadOnlyRoots()
    {
        var profile = new ProfileConfig
        {
            RagnaforgeMainProjectPath = _writableDir,
            RathenaPath = _writableDir,
            PatchPath = _writableDir,
            GrfRepositoryPath = _readOnlyDir,
            GrfEditorPath = _writableDir,
            WritableRoots = [_writableDir],
            ReadOnlyRoots = []
        };

        var issues = PathGuard.EnsureProfileIsSafe(profile);
        Assert.Contains(issues, i => i.Contains("readOnlyRoots", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnsureGrfRepositoryIsReadOnly_PassesWhenCorrect()
    {
        var profile = new ProfileConfig
        {
            GrfRepositoryPath = _readOnlyDir,
            ReadOnlyRoots = [_readOnlyDir]
        };

        var issues = PathGuard.EnsureGrfRepositoryIsReadOnly(profile);
        Assert.Empty(issues);
    }

    [Fact]
    public void EnsureProfileIsSafe_CatchesTraversalInPaths()
    {
        var profile = new ProfileConfig
        {
            RagnaforgeMainProjectPath = @"C:\Users\..\secret",
            RathenaPath = _writableDir,
            PatchPath = _writableDir,
            GrfRepositoryPath = _readOnlyDir,
            GrfEditorPath = _writableDir,
            WritableRoots = [_writableDir],
            ReadOnlyRoots = [_readOnlyDir]
        };

        var issues = PathGuard.EnsureProfileIsSafe(profile);
        Assert.Contains(issues, i => i.Contains("traversal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnsureProfileIsSafe_AllowsWritableRootContainingProtectedReadOnlyIsland()
    {
        var parentDir = Path.Combine(_tempDir, "parent");
        var childDir = Path.Combine(parentDir, "child_readonly");
        Directory.CreateDirectory(parentDir);
        Directory.CreateDirectory(childDir);

        var profile = new ProfileConfig
        {
            RagnaforgeMainProjectPath = _writableDir,
            RathenaPath = _writableDir,
            PatchPath = _writableDir,
            GrfRepositoryPath = childDir,
            GrfEditorPath = _writableDir,
            WritableRoots = [_writableDir, parentDir],
            ReadOnlyRoots = [childDir]
        };

        var issues = PathGuard.EnsureProfileIsSafe(profile);
        Assert.Empty(issues);
    }

    [Fact]
    public void EnsureProfileIsSafe_CatchesReadOnlyContainingWritable()
    {
        var parentDir = Path.Combine(_tempDir, "ro_parent");
        var childDir = Path.Combine(parentDir, "wr_child");
        Directory.CreateDirectory(parentDir);
        Directory.CreateDirectory(childDir);

        var profile = new ProfileConfig
        {
            RagnaforgeMainProjectPath = _writableDir,
            RathenaPath = _writableDir,
            PatchPath = _writableDir,
            GrfRepositoryPath = parentDir,
            GrfEditorPath = _writableDir,
            WritableRoots = [_writableDir, childDir],
            ReadOnlyRoots = [parentDir]
        };

        var issues = PathGuard.EnsureProfileIsSafe(profile);
        Assert.Contains(issues, i => i.Contains("contains writableRoot", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnsureProfileIsSafe_AllowsGrfInsideWritableRootWhenProtectedByReadOnlyIsland()
    {
        var parentWritable = Path.Combine(_tempDir, "writable_parent");
        var grfChild = Path.Combine(parentWritable, "grfs_subdir");
        Directory.CreateDirectory(parentWritable);
        Directory.CreateDirectory(grfChild);

        var profile = new ProfileConfig
        {
            RagnaforgeMainProjectPath = _writableDir,
            RathenaPath = _writableDir,
            PatchPath = _writableDir,
            GrfRepositoryPath = grfChild,
            GrfEditorPath = _writableDir,
            WritableRoots = [_writableDir, parentWritable],
            ReadOnlyRoots = [grfChild]
        };

        var issues = PathGuard.EnsureProfileIsSafe(profile);
        Assert.Empty(issues);
    }

    [Fact]
    public void EnsureCanWrite_BlocksProtectedReadOnlyIslandInsideWritableRoot()
    {
        var parentWritable = Path.Combine(_tempDir, "content_root");
        var protectedChild = Path.Combine(parentWritable, "GRF'S");
        Directory.CreateDirectory(parentWritable);
        Directory.CreateDirectory(protectedChild);

        var guard = new PathGuard([parentWritable], [protectedChild], blockLubEditing: true);
        var blockedPath = Path.Combine(protectedChild, "blocked.txt");
        var allowedPath = Path.Combine(parentWritable, "allowed.txt");

        Assert.False(guard.EnsureCanWrite(blockedPath).IsAllowed);
        Assert.True(guard.EnsureCanWrite(allowedPath).IsAllowed);
    }
}
