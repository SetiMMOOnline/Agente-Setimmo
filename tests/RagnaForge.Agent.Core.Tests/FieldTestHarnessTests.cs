using System.Text.Json;
using RagnaForge.Agent.Core.Commands;

namespace RagnaForge.Agent.Core.Tests;

public sealed class FieldTestHarnessTests : IDisposable
{
    private readonly string _agentRoot;

    public FieldTestHarnessTests()
    {
        _agentRoot = Path.Combine(Path.GetTempPath(), $"setimmo_field_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_agentRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_agentRoot, recursive: true); } catch { }
    }

    [Fact]
    public void FieldTestRun_ExercisesStacksInsideSandboxOnly()
    {
        var output = new FieldTestCommand(_agentRoot, "test", "run", keepSandbox: true).Execute();

        Assert.True(output.Ok, string.Join(Environment.NewLine, output.Errors));
        Assert.True(output.SafeForAutomation);

        var json = JsonSerializer.Serialize(output.Data);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(6, root.GetProperty("total").GetInt32());
        Assert.Equal(6, root.GetProperty("passed").GetInt32());
        Assert.Equal(0, root.GetProperty("failed").GetInt32());
        Assert.True(root.GetProperty("writesConfinedToSandbox").GetBoolean());
        Assert.False(root.GetProperty("realProjectWrites").GetBoolean());
        Assert.False(root.GetProperty("shellExecuted").GetBoolean());
        Assert.True(root.GetProperty("safeForReadOnlyWork").GetBoolean());
        Assert.True(root.GetProperty("safeForDryRun").GetBoolean());
        Assert.False(root.GetProperty("safeForApply").GetBoolean());
        Assert.False(root.GetProperty("safeForProductionApply").GetBoolean());

        var relativeSandbox = root.GetProperty("sandboxRoot").GetString();
        Assert.False(string.IsNullOrWhiteSpace(relativeSandbox));
        var fullSandbox = Path.GetFullPath(Path.Combine(_agentRoot, relativeSandbox!));
        Assert.StartsWith(Path.Combine(_agentRoot, "temp", "field-tests"), fullSandbox, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FieldTestRun_RejectsUnknownShape()
    {
        var output = new FieldTestCommand(_agentRoot, "fixture", "run", keepSandbox: false).Execute();

        Assert.False(output.Ok);
        Assert.Contains("Usage: ragnaforge field test run", output.Errors.Single());
    }
}
