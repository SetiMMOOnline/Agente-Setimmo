using RagnaForge.Agent.Core.Commands;

namespace RagnaForge.Agent.Core.Tests;

public sealed class CleanupCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _agentRoot;

    public CleanupCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ragnaforge_cleanup_test_{Guid.NewGuid():N}");
        _agentRoot = Path.Combine(_tempDir, "agent");

        Directory.CreateDirectory(Path.Combine(_agentRoot, "src", "Feature", "bin"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "tests", "Feature", "obj"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "cache", "agent"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "reports"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "inputs", "dry-run"));

        File.WriteAllText(Path.Combine(_agentRoot, "src", "Feature", "bin", "temp.dll"), "x");
        File.WriteAllText(Path.Combine(_agentRoot, "tests", "Feature", "obj", "temp.txt"), "x");
        File.WriteAllText(Path.Combine(_agentRoot, "cache", "agent", "entities_index.json"), "{}");
        File.WriteAllText(Path.Combine(_agentRoot, "logs", "reports", "temp.report.md"), "# temp");
        File.WriteAllText(Path.Combine(_agentRoot, "inputs", "dry-run", "mcp-temp.json"), "{}");
        File.WriteAllText(Path.Combine(_agentRoot, "tests_output.txt"), "temp");
        File.WriteAllText(Path.Combine(_agentRoot, "cache", "agent", ".gitkeep"), string.Empty);
        File.WriteAllText(Path.Combine(_agentRoot, "logs", "reports", ".gitkeep"), string.Empty);
        File.WriteAllText(Path.Combine(_agentRoot, "inputs", "dry-run", ".gitkeep"), string.Empty);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void CleanupCommand_RemovesRegenerableArtifacts_AndPreservesGitkeep()
    {
        var result = new CleanupCommand(_agentRoot, includeLogs: true, includeCache: true, includeInputs: true).Execute();

        Assert.True(result.Ok);
        Assert.False(Directory.Exists(Path.Combine(_agentRoot, "src", "Feature", "bin")));
        Assert.False(Directory.Exists(Path.Combine(_agentRoot, "tests", "Feature", "obj")));
        Assert.False(File.Exists(Path.Combine(_agentRoot, "cache", "agent", "entities_index.json")));
        Assert.False(File.Exists(Path.Combine(_agentRoot, "logs", "reports", "temp.report.md")));
        Assert.False(File.Exists(Path.Combine(_agentRoot, "inputs", "dry-run", "mcp-temp.json")));
        Assert.False(File.Exists(Path.Combine(_agentRoot, "tests_output.txt")));
        Assert.True(File.Exists(Path.Combine(_agentRoot, "cache", "agent", ".gitkeep")));
        Assert.True(File.Exists(Path.Combine(_agentRoot, "logs", "reports", ".gitkeep")));
        Assert.True(File.Exists(Path.Combine(_agentRoot, "inputs", "dry-run", ".gitkeep")));
    }
}
