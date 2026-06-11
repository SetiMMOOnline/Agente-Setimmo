using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Implementation;

namespace RagnaForge.Agent.Core.Tests;

public sealed class ImplementationWorkflowTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _agentRoot;
    private readonly string _configDir;
    private readonly string _mainProjectDir;

    public ImplementationWorkflowTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ragnaforge_implementation_{Guid.NewGuid():N}");
        _agentRoot = Path.Combine(_tempDir, "agent");
        _configDir = Path.Combine(_agentRoot, "config");
        _mainProjectDir = Path.Combine(_tempDir, "project");

        Directory.CreateDirectory(_configDir);
        Directory.CreateDirectory(_mainProjectDir);
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "operations"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "diffs"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "rollbacks"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "inputs", "dry-run"));
        WriteConfigs();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void CreateContentCommand_PersistsDryRunWithoutWritingTarget()
    {
        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "docs/generated.html",
            LanguageHint = "html",
            Template = "default",
            Title = "Generated",
            Description = "Dry-run only"
        };

        var result = new CreateContentCommand(_configDir, _agentRoot, request).Execute();

        Assert.True(result.Ok);
        var data = ToElement(result.Data);
        Assert.True(data.GetProperty("validation").GetProperty("applyEnabled").GetBoolean());
        Assert.False(File.Exists(Path.Combine(_mainProjectDir, "docs", "generated.html")));
        Assert.True(File.Exists(Path.Combine(_agentRoot, "logs", "operations", $"{result.OperationId}.json")));
    }

    [Fact]
    public void ApplyImplementCommand_WritesFile_AndRollbackRestores()
    {
        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "scripts/generated.ps1",
            LanguageHint = "powershell",
            Template = "default",
            Name = "Invoke-TestAction",
            Description = "Created for apply test."
        };

        var dryRun = new CreateContentCommand(_configDir, _agentRoot, request).Execute();
        Assert.True(dryRun.Ok);

        var apply = new ApplyImplementCommand(_configDir, _agentRoot, dryRun.OperationId, confirm: true).Execute();
        Assert.True(apply.Ok);
        var targetPath = Path.Combine(_mainProjectDir, "scripts", "generated.ps1");
        Assert.True(File.Exists(targetPath));

        var report = new ReportCommand(_agentRoot, dryRun.OperationId, last: false, format: "md").Execute();
        Assert.True(report.Ok);
        var reportData = ToElement(report.Data);
        var reportPath = Path.Combine(_agentRoot, reportData.GetProperty("reportPath").GetString()!.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(reportPath));
        var markdown = File.ReadAllText(reportPath);
        Assert.Contains("Applied", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nothing was applied.", markdown, StringComparison.OrdinalIgnoreCase);

        var rollback = new RollbackCommand(_configDir, _agentRoot, dryRun.OperationId, list: false, dryRun: false, confirm: true).Execute();
        Assert.True(rollback.Ok);
        Assert.False(File.Exists(targetPath));
    }

    [Fact]
    public void FixCodeCommand_CreatesNormalizedImplementationDiff()
    {
        var targetPath = Path.Combine(_mainProjectDir, "src", "Sample.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, "namespace Sample;  \r\n\r\npublic class Demo { }\t");

        var result = new FixCodeCommand(_configDir, _agentRoot, "src/Sample.cs", "main", "csharp").Execute();

        Assert.True(result.Ok);
        var diffPath = Path.Combine(_agentRoot, "logs", "diffs", $"{result.OperationId}.diff.json");
        Assert.True(File.Exists(diffPath));
        var diffDocument = JsonSerializer.Deserialize<ImplementationPlanDocument>(File.ReadAllText(diffPath));
        Assert.NotNull(diffDocument);
        Assert.DoesNotContain("}\t", diffDocument!.Files[0].TargetContent);
    }

    [Fact]
    public void DryRunImplement_BlocksInstructionNotedPlaceholder()
    {
        var targetPath = Path.Combine(_mainProjectDir, "src", "Sample.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, "namespace Sample;\n\npublic class Demo { }\n");

        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "src/Sample.cs",
            LanguageHint = "csharp",
            Instruction = "Add a new complete feature that Setimmo cannot infer."
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.False(result.Ok);
        var data = ToElement(result.Data);
        Assert.Equal("needs_codex_repair", data.GetProperty("status").GetString());
        Assert.Equal("non_semantic_patch", data.GetProperty("blocker").GetString());
        Assert.DoesNotContain("Instruction noted", result.ToJson(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DryRunImplement_GeneratesSemanticLiteralReplacement()
    {
        var targetPath = Path.Combine(_mainProjectDir, "src", "Sample.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, "namespace Sample;\n\npublic class Demo { public bool Enabled() => false; }\n");

        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "src/Sample.cs",
            LanguageHint = "csharp",
            Instruction = "replace '=> false' with '=> true'"
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.True(result.Ok);
        var data = ToElement(result.Data);
        Assert.Equal("planned", data.GetProperty("status").GetString());
        Assert.Equal("semantic", data.GetProperty("supervision").GetProperty("patchQuality").GetProperty("classification").GetString());
        Assert.Contains("=> true", data.GetProperty("diffPreview").GetString() ?? string.Empty);
        var diffPath = Path.Combine(_agentRoot, "logs", "diffs", $"{result.OperationId}.diff.json");
        var plan = JsonSerializer.Deserialize<ImplementationPlanDocument>(File.ReadAllText(diffPath));
        Assert.Contains("=> true", plan!.Files[0].TargetContent);
        Assert.DoesNotContain("Instruction noted", plan.Files[0].TargetContent);
    }

    [Fact]
    public void DryRunImplement_BlocksCommentOnlyPatch()
    {
        var targetPath = Path.Combine(_mainProjectDir, "src", "Sample.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, "namespace Sample;\n\npublic class Demo { }\n");

        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "src/Sample.cs",
            LanguageHint = "csharp",
            Instruction = "add line \"// TODO: implement later\""
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.False(result.Ok);
        var data = ToElement(result.Data);
        Assert.Contains("comment_or_todo_only_patch", data.GetProperty("semanticPatch").GetProperty("patchQuality").GetProperty("blockers").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public void DryRunImplement_UpdatesJsonConfigSemantically()
    {
        var targetPath = Path.Combine(_mainProjectDir, "config", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, "{\"enabled\":false,\"name\":\"demo\"}");

        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "config/settings.json",
            LanguageHint = "json",
            Instruction = "set enabled to true"
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.True(result.Ok);
        var diffPath = Path.Combine(_agentRoot, "logs", "diffs", $"{result.OperationId}.diff.json");
        var plan = JsonSerializer.Deserialize<ImplementationPlanDocument>(File.ReadAllText(diffPath));
        Assert.Contains("\"enabled\": true", plan!.Files[0].TargetContent);
        Assert.True(plan.Supervision.RequiresCodexReview);
    }

    [Fact]
    public void DryRunImplement_LocalDevProfile_RelaxesMediumRiskReview()
    {
        RewriteSafetyConfig("local-dev");

        var targetPath = Path.Combine(_agentRoot, "temp", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, "{\"enabled\":false,\"name\":\"demo\"}");

        var request = new ImplementationRequest
        {
            Workspace = "agent",
            TargetPath = "temp/settings.json",
            LanguageHint = "json",
            Instruction = "set enabled to true"
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.True(result.Ok);
        var data = ToElement(result.Data);
        Assert.Equal("standalone-relaxed", data.GetProperty("governanceProfile").GetString());
        Assert.False(data.GetProperty("supervision").GetProperty("requiresCodexReview").GetBoolean());
        Assert.True(data.GetProperty("canAutoApply").GetBoolean());
    }

    [Fact]
    public void DryRunImplement_CreatesMissingFileFromCreateInstruction_AndSupportsApplyRollback()
    {
        RewriteSafetyConfig("local-dev");

        var request = new ImplementationRequest
        {
            Workspace = "agent",
            TargetPath = "temp/validation-smoke.ps1",
            LanguageHint = "powershell",
            Instruction = "Create a safe smoke script that confirms the implementation validation path."
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.True(result.Ok);
        var data = ToElement(result.Data);
        Assert.Equal("create", data.GetProperty("action").GetString());
        Assert.Equal("planned", data.GetProperty("status").GetString());
        Assert.Equal("semantic", data.GetProperty("patchQuality").GetProperty("classification").GetString());
        Assert.True(data.GetProperty("canAutoApply").GetBoolean());

        var targetPath = Path.Combine(_agentRoot, "temp", "validation-smoke.ps1");
        Assert.False(File.Exists(targetPath));

        var apply = new ApplyImplementCommand(_configDir, _agentRoot, result.OperationId, confirm: true).Execute();
        Assert.True(apply.Ok);
        Assert.True(File.Exists(targetPath));

        var contents = File.ReadAllText(targetPath);
        Assert.Contains("Write-Host", contents, StringComparison.Ordinal);

        var rollback = new RollbackCommand(_configDir, _agentRoot, result.OperationId, list: false, dryRun: false, confirm: true).Execute();
        Assert.True(rollback.Ok);
        Assert.False(File.Exists(targetPath));
    }

    [Fact]
    public void DryRunImplement_MissingTargetWithoutCreateIntent_StillRequiresCodexRepair()
    {
        RewriteSafetyConfig("local-dev");

        var request = new ImplementationRequest
        {
            Workspace = "agent",
            TargetPath = "temp/replace-missing.ps1",
            LanguageHint = "powershell",
            Instruction = "replace 'old' with 'new'"
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.False(result.Ok);
        var data = ToElement(result.Data);
        Assert.Equal("needs_codex_repair", data.GetProperty("status").GetString());
        Assert.Equal("non_semantic_patch", data.GetProperty("blocker").GetString());
    }

    [Fact]
    public void ReviewCodeCommand_ReportsSecretsAndTodos()
    {
        var targetPath = Path.Combine(_mainProjectDir, "src", "danger.js");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        var secret = "sk" + "-test-secret-value";
        File.WriteAllText(targetPath, $"// TODO\nconst token = '{secret}';\n");

        var result = new ReviewCodeCommand(_configDir, _agentRoot, "src/danger.js", "main", "javascript").Execute();

        Assert.True(result.Ok);
        var data = ToElement(result.Data);
        var issues = data.GetProperty("issues").EnumerateArray().Select(issue => issue.GetProperty("code").GetString()).ToList();
        Assert.Contains("review.todo_marker", issues);
        Assert.Contains("security.secret_detected", issues);
    }

    [Fact]
    public void DryRunImplement_BlocksSensitiveTargets()
    {
        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "assets/client.lub",
            LanguageHint = "lua",
            Content = "print('blocked')"
        };

        var result = new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();

        Assert.False(result.Ok);
        Assert.Contains(".lub", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    private void WriteConfigs()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(_configDir, "ragnaforge.agent.json"),
            JsonSerializer.Serialize(new
            {
                agentName = "Implementation Test",
                mode = "local-orchestrator",
                primaryOperators = new[] { "Test" },
                defaultOutputFormat = "json",
                cacheEnabled = true,
                logsEnabled = true
            }, options));

        File.WriteAllText(Path.Combine(_configDir, "paths.json"),
            JsonSerializer.Serialize(new
            {
                agentRoot = _agentRoot,
                activeProfile = "test",
                profiles = new Dictionary<string, object>
                {
                    ["test"] = new
                    {
                        ragnaforgeMainProjectPath = _mainProjectDir,
                        rathenaPath = Path.Combine(_tempDir, "rathena"),
                        patchPath = Path.Combine(_tempDir, "patch"),
                        grfRepositoryPath = Path.Combine(_tempDir, "grfs"),
                        grfEditorPath = Path.Combine(_tempDir, "editor"),
                        dbMode = "renewal",
                        writableRoots = new[] { _agentRoot, _mainProjectDir },
                        readOnlyRoots = new[] { Path.Combine(_tempDir, "grfs") }
                    }
                }
            }, options));

        RewriteSafetyConfig("strict");
    }

    private void RewriteSafetyConfig(string operationProfile)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(_configDir, "safety.json"),
            JsonSerializer.Serialize(new
            {
                operationProfile,
                requireDryRunBeforeApply = true,
                requireDiffBeforeApply = true,
                requireValidationBeforeApply = true,
                requireExplicitConfirmation = true,
                backupBeforeApply = true,
                blockOriginalGrfWrite = true,
                blockLubEditing = true,
                invalidateCacheOnPathChange = true,
                cacheMustMatchActiveProfile = true
            }, options));
    }

    private static JsonElement ToElement(object? value) =>
        value is null ? JsonDocument.Parse("{}").RootElement : JsonSerializer.SerializeToElement(value);
}
