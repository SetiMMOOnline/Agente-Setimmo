using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Implementation;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Production;
using RagnaForge.Agent.Mcp.Safety;
using RagnaForge.Agent.Mcp.Tools;

namespace RagnaForge.Agent.Core.Tests;

public sealed class ProductionAndGrfIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _agentRoot;
    private readonly string _configDir;
    private readonly string _mainProjectDir;
    private readonly string _grfExtractorDir;
    private readonly string? _previousGrfRoot;

    public ProductionAndGrfIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ragnaforge_production_{Guid.NewGuid():N}");
        _agentRoot = Path.Combine(_tempDir, "agent");
        _configDir = Path.Combine(_agentRoot, "config");
        _mainProjectDir = Path.Combine(_tempDir, "project");
        _grfExtractorDir = Path.Combine(_tempDir, "GRF_Extractor");

        Directory.CreateDirectory(_configDir);
        Directory.CreateDirectory(_mainProjectDir);
        Directory.CreateDirectory(_grfExtractorDir);
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "operations"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "diffs"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "rollbacks"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "inputs", "dry-run"));
        File.WriteAllText(Path.Combine(_grfExtractorDir, "sample.grf"), "fixture only");

        _previousGrfRoot = Environment.GetEnvironmentVariable("SETIMMO_GRF_EXTRACTOR_ROOT");
        Environment.SetEnvironmentVariable("SETIMMO_GRF_EXTRACTOR_ROOT", _grfExtractorDir);
        WriteConfigs();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("SETIMMO_GRF_EXTRACTOR_ROOT", _previousGrfRoot);
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void ProductionStatus_BlocksProductionWithoutApproval()
    {
        var dryRun = CreateHtmlDryRun();
        Assert.True(dryRun.Ok);

        var status = new ProductionCommand(
            _configDir,
            _agentRoot,
            "status",
            dryRun.OperationId,
            "production",
            approver: null,
            reason: null,
            ttlMinutes: 1440,
            confirm: false).Execute();

        Assert.True(status.Ok);
        var data = ToElement(status.Data);
        Assert.False(data.GetProperty("safeForProductionApply").GetBoolean());
        Assert.Contains("record_human_approval", status.NextRequiredAction);
    }

    [Fact]
    public void ProductionApproval_UnlocksFormalProductionSafetyForCurrentDiffHash()
    {
        var dryRun = CreateHtmlDryRun();
        Assert.True(dryRun.Ok);

        var approval = new ProductionCommand(
            _configDir,
            _agentRoot,
            "approve",
            dryRun.OperationId,
            "production",
            approver: "Human Reviewer",
            reason: "Fixture operation approved for production safety test.",
            ttlMinutes: 60,
            confirm: false).Execute();

        Assert.True(approval.Ok);
        var data = ToElement(approval.Data);
        Assert.True(data.GetProperty("safeForProductionApply").GetBoolean());
        Assert.True(File.Exists(Path.Combine(_agentRoot, "logs", "production", $"{dryRun.OperationId}.approval.json")));
    }

    [Fact]
    public void ProductionApply_UsesExistingImplementationApplyAndRollbackPlan()
    {
        var dryRun = CreateHtmlDryRun();
        Assert.True(dryRun.Ok);
        var approve = new ProductionCommand(
            _configDir,
            _agentRoot,
            "approve",
            dryRun.OperationId,
            "production",
            "Human Reviewer",
            "Approved for controlled test.",
            60,
            confirm: false).Execute();
        Assert.True(approve.Ok);

        var apply = new ProductionCommand(
            _configDir,
            _agentRoot,
            "apply",
            dryRun.OperationId,
            "production",
            null,
            null,
            60,
            confirm: true).Execute();

        Assert.True(apply.Ok);
        Assert.True(File.Exists(Path.Combine(_mainProjectDir, "docs", "generated.html")));

        var rollback = new ProductionCommand(
            _configDir,
            _agentRoot,
            "rollback",
            dryRun.OperationId,
            "production",
            null,
            null,
            60,
            confirm: true).Execute();

        Assert.True(rollback.Ok);
        Assert.False(File.Exists(Path.Combine(_mainProjectDir, "docs", "generated.html")));
    }

    [Fact]
    public void OperationsCommand_ListsAndShowsOperationHistory()
    {
        var dryRun = CreateHtmlDryRun();
        Assert.True(dryRun.Ok);

        var list = new OperationsCommand(_agentRoot, "list", null, null, null).Execute();
        var show = new OperationsCommand(_agentRoot, "show", dryRun.OperationId, null, null).Execute();

        Assert.True(list.Ok);
        Assert.True(show.Ok);
        Assert.Contains(dryRun.OperationId, JsonSerializer.Serialize(list.Data));
        Assert.Contains("implementation-dry-run", JsonSerializer.Serialize(show.Data));
    }

    [Fact]
    public void GrfIntegration_InspectsAndCreatesControlledMetadataOutputOnly()
    {
        var list = new GrfCommand(_configDir, _agentRoot, "list", null, null, confirm: false).Execute();
        var inspect = new GrfCommand(_configDir, _agentRoot, "inspect", "sample.grf", null, confirm: false).Execute();
        var dryRun = new GrfCommand(_configDir, _agentRoot, "dry-run-extract", "sample.grf", null, confirm: false).Execute();
        var extract = new GrfCommand(_configDir, _agentRoot, "extract", null, dryRun.OperationId, confirm: true).Execute();

        Assert.True(list.Ok);
        Assert.True(inspect.Ok);
        Assert.True(dryRun.Ok);
        Assert.True(extract.Ok);
        var data = ToElement(extract.Data);
        Assert.False(data.GetProperty("originalContainerModified").GetBoolean());
        Assert.False(data.GetProperty("realAssetPayloadCopied").GetBoolean());
        Assert.True(File.Exists(Path.Combine(_agentRoot, "temp", "grf-operations", dryRun.OperationId, "output", "EXTRACTION_MANIFEST.json")));
    }

    [Fact]
    public void McpRegistry_ExposesProductionOperationsAndGrfToolsThroughAllowlist()
    {
        Assert.True(McpToolPolicy.IsAllowed("ragnaforge_operations_list"));
        Assert.True(McpToolPolicy.IsAllowed("ragnaforge_production_status"));
        Assert.True(McpToolPolicy.IsAllowed("ragnaforge_grf_inspect"));
        Assert.True(McpToolPolicy.IsBlocked("ragnaforge_apply"));

        var registry = new McpToolRegistry(new McpToolContext(_agentRoot));
        var audit = registry.Execute("ragnaforge_production_audit", JsonDocument.Parse("{}").RootElement);
        var grfList = registry.Execute("ragnaforge_grf_list", JsonDocument.Parse("{}").RootElement);

        Assert.True(audit.Ok);
        Assert.True(grfList.Ok);
    }

    private JsonOutput CreateHtmlDryRun()
    {
        var request = new ImplementationRequest
        {
            Workspace = "main",
            TargetPath = "docs/generated.html",
            LanguageHint = "html",
            Template = "default",
            Title = "Production Fixture",
            Description = "Fixture"
        };

        return new DryRunImplementCommand(_configDir, _agentRoot, request).Execute();
    }

    private void WriteConfigs()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(_configDir, "ragnaforge.agent.json"),
            JsonSerializer.Serialize(new
            {
                agentName = "Production Test",
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
                        grfRepositoryPath = _grfExtractorDir,
                        grfEditorPath = Path.Combine(_tempDir, "editor"),
                        dbMode = "renewal",
                        writableRoots = new[] { _agentRoot, _mainProjectDir },
                        readOnlyRoots = new[] { _grfExtractorDir }
                    }
                }
            }, options));

        File.WriteAllText(Path.Combine(_configDir, "safety.json"),
            JsonSerializer.Serialize(new
            {
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
