using System.Text.Json;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Commands;

namespace RagnaForge.Agent.Core.Tests;

public sealed class GlobalCanonTests
{
    [Fact]
    public void Policy_IsEnabled_AndApplyIsValidatorGoverned()
    {
        var policy = GlobalCanonPolicy.CreateDefault();

        Assert.True(policy.CanonEnabled);
        Assert.False(policy.SafeForApply);
        Assert.True(policy.IsBlockedOperation("please run apply now"));
        Assert.True(policy.IsBlockedOperation("rollback --confirm"));
        Assert.True(policy.IsBlockedOperation("grf apply"));
        Assert.True(policy.IsBlockedOperation("generic shell"));
    }

    [Fact]
    public void Policy_RecognizesSensitiveFilesAndArtifacts()
    {
        var policy = GlobalCanonPolicy.CreateDefault();

        Assert.True(policy.IsSensitiveFile(@"C:\agent\.env"));
        Assert.True(policy.IsSensitiveFile(@"C:\agent\repositories.local.json"));
        Assert.True(policy.IsSensitiveFile(@"C:\agent\client.grf"));
        Assert.True(policy.IsForbiddenArtifactDirectory(@"C:\agent\bin"));
        Assert.True(policy.IsForbiddenArtifactDirectory(@"C:\agent\obj"));
        Assert.True(policy.IsForbiddenArtifactDirectory(@"C:\agent\raw-html"));
        Assert.True(policy.IsForbiddenArtifactDirectory(@"C:\agent\external-cache"));
    }

    [Fact]
    public void Validator_ReturnsCriticalFindingForSensitiveFile()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, ".env"), "SECRET=example");

        var result = new GlobalCanonValidator(root).Check();

        Assert.True(result.CanonEnabled);
        Assert.False(result.SafeForApply);
        Assert.Contains(result.Findings, f => f.Severity == "critical" && f.Category == "sensitive-files");
    }

    [Fact]
    public void Validator_DetectsArtifactsWithoutBlockingReadOnlyWork()
    {
        var root = CreateTempRoot();
        Directory.CreateDirectory(Path.Combine(root, "bin"));
        Directory.CreateDirectory(Path.Combine(root, "obj"));

        var result = new GlobalCanonValidator(root).Check();

        Assert.True(result.SafeForReadOnlyWork);
        Assert.False(result.SafeForApply);
        Assert.Contains(result.Findings, f => f.Category == "forbidden-artifact" && f.Severity == "warning");
    }

    [Fact]
    public void CanonCommand_ReturnsJsonCompatibleData()
    {
        var root = CreateTempRoot();
        Directory.CreateDirectory(Path.Combine(root, "config"));
        Directory.CreateDirectory(Path.Combine(root, "docs"));
        File.WriteAllText(Path.Combine(root, "docs", "CANONE_GLOBAL_DE_REGRAS.md"), "# Canon");
        File.WriteAllText(Path.Combine(root, "config", "ragnaforge.agent.json"), """
        {"agentName":"Test","mode":"safe","primaryOperators":[]}
        """);
        File.WriteAllText(Path.Combine(root, "config", "safety.json"), """
        {
          "requireDryRunBeforeApply": true,
          "requireDiffBeforeApply": true,
          "requireValidationBeforeApply": true,
          "requireExplicitConfirmation": true,
          "backupBeforeApply": true,
          "blockOriginalGrfWrite": true,
          "blockLubEditing": true,
          "invalidateCacheOnPathChange": true,
          "cacheMustMatchActiveProfile": true
        }
        """);
        File.WriteAllText(Path.Combine(root, "config", "paths.json"), $$"""
        {
          "agentRoot": "{{JsonEsc(root)}}",
          "activeProfile": "test",
          "profiles": {
            "test": {
              "ragnaforgeMainProjectPath": "{{JsonEsc(root)}}",
              "rathenaPath": "{{JsonEsc(root)}}",
              "patchPath": "{{JsonEsc(root)}}",
              "grfRepositoryPath": "{{JsonEsc(root)}}",
              "grfEditorPath": "{{JsonEsc(root)}}",
              "dbMode": "renewal",
              "writableRoots": ["{{JsonEsc(root)}}"],
              "readOnlyRoots": ["{{JsonEsc(root)}}"]
            }
          }
        }
        """);

        var output = new CanonCommand(Path.Combine(root, "config"), root).Execute();
        var json = output.ToJson();
        var document = JsonDocument.Parse(json);

        Assert.True(output.Ok);
        Assert.Equal("canon-check", output.Mode);
        Assert.False(document.RootElement.GetProperty("data").GetProperty("safeForApply").GetBoolean());
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ragnaforge-canon-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string JsonEsc(string value) => value.Replace(@"\", @"\\");
}
