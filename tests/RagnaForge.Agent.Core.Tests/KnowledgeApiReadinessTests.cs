using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Knowledge;

namespace RagnaForge.Agent.Core.Tests;

public sealed class KnowledgeApiReadinessTests
{
    [Fact]
    public void ApiReadinessExport_ReflectsValidatorGovernedImplementationCapabilities()
    {
        var command = new ApiReadinessExportCommand(FindRepoRoot());

        var result = command.Execute();

        Assert.True(result.Ok);
        var json = result.ToJson();
        Assert.Contains("\"supportsApply\": true", json);
        Assert.Contains("\"supportsCodexSupervised\": true", json);
        Assert.Contains("\"supportsSemanticPatch\": true", json);
        Assert.Contains("\"safeForApply\": false", json);
        Assert.Contains("\"safeForProductionApply\": false", json);
        Assert.Contains("\"applyEnabled\": false", json);
        Assert.Contains("\"rollbackEnabled\": false", json);
        Assert.Contains("recommendedUiTabs", json);
        Assert.Contains("ragnaforge_api_readiness_export", json);
        Assert.Contains("ragnaforge_apply_implement", json);
        Assert.Contains("ragnaforge_review_code", json);
    }

    [Fact]
    public void KnowledgeService_BuildPackAssessments_ReturnsFreshAssessments()
    {
        var service = new KnowledgeService(FindRepoRoot());

        var assessments = service.BuildPackAssessments();

        Assert.NotEmpty(assessments);
        Assert.All(assessments, assessment =>
        {
            Assert.False(string.IsNullOrWhiteSpace(assessment.PackId));
            Assert.False(string.IsNullOrWhiteSpace(assessment.SchemaVersion));
            Assert.False(string.IsNullOrWhiteSpace(assessment.FreshnessState));
        });
    }

    [Fact]
    public void KnowledgeCommand_FreshnessAndPacks_AreReadOnlyAndSafeForApplyFalse()
    {
        var repoRoot = FindRepoRoot();
        var configDir = Path.Combine(repoRoot, "config");
        var packs = new KnowledgeCommand(configDir, repoRoot, "packs", new Dictionary<string, string>()).Execute();
        var freshness = new KnowledgeCommand(configDir, repoRoot, "freshness", new Dictionary<string, string>()).Execute();

        Assert.True(packs.Ok);
        Assert.True(freshness.Ok);
        Assert.Contains("\"safeForApply\": false", packs.ToJson());
        Assert.Contains("\"safeForApply\": false", freshness.ToJson());
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RagnaForge.Agent.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate RagnaForge.Agent.slnx from test output path.");
    }
}
