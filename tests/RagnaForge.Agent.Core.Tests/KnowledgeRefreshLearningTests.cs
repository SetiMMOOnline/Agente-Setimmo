using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Knowledge;
using RagnaForge.Agent.Mcp.Safety;
using RagnaForge.Agent.Mcp.Tools;

namespace RagnaForge.Agent.Core.Tests;

public sealed class KnowledgeRefreshLearningTests
{
    [Fact]
    public void KnowledgeLibrary_RegistersNewSourcesAsReadOnly()
    {
        var service = new KnowledgeService(FindRepoRoot());
        var sources = service.LoadSources();

        Assert.Contains(sources, source => source.Id == "rathena-board" && source.SourceType == "forum_reference" && source.ReadOnly);
        Assert.Contains(sources, source => source.Id == "robrowserlegacy" && source.SourceType == "github_repository_reference_library" && source.ReadOnly);
        Assert.Contains(sources, source => source.Id == "robrowserlegacy-remoteclient-js" && source.SourceType == "github_repository_reference_library" && source.ReadOnly);
        Assert.All(sources.Where(source => source.Id is "rathena-board" or "robrowserlegacy" or "robrowserlegacy-remoteclient-js"), source => Assert.False(source.CanBlock));
    }

    [Fact]
    public void KnowledgeFreshness_IncludesSourcesAndSnapshots()
    {
        var service = new KnowledgeService(FindRepoRoot());

        var json = JsonSerializer.Serialize(service.BuildFreshnessReport());

        Assert.Contains("totalSources", json);
        Assert.Contains("robrowserlegacy", json);
        Assert.Contains("rathena-board", json);
        Assert.Contains("sanitizedSnapshots", json);
    }

    [Fact]
    public void KnowledgeSnapshots_AreSanitizedWithoutRawContent()
    {
        var service = new KnowledgeService(FindRepoRoot());
        var snapshots = service.LoadSnapshots();

        Assert.NotEmpty(snapshots);
        Assert.Contains(snapshots, snapshot => snapshot.SourceId == "robrowserlegacy");
        Assert.All(snapshots, snapshot =>
        {
            Assert.True(snapshot.Sanitized);
            Assert.False(snapshot.RawStored);
        });
    }

    [Fact]
    public void LearningCandidates_AreReviewFirst_AndSafeForApplyFalse()
    {
        var service = new KnowledgeService(FindRepoRoot());
        var candidates = service.LoadLearningCandidates();

        Assert.NotEmpty(candidates);
        Assert.Contains(candidates, candidate => candidate.SourceId == "robrowserlegacy");
        Assert.All(candidates, candidate =>
        {
            Assert.True(candidate.HumanReviewRequired);
            Assert.False(candidate.RawHtmlStored);
            Assert.False(candidate.SecretStored);
            Assert.False(candidate.SafeForApply);
        });
    }

    [Fact]
    public void LearningObserve_BlocksSecretsAndRawHtml()
    {
        var service = new LearningService(new KnowledgeService(FindRepoRoot()));

        var secret = Assert.Throws<InvalidOperationException>(() => service.Observe("robrowserlegacy", "secrets", "token=abc"));
        var html = Assert.Throws<InvalidOperationException>(() => service.Observe("robrowserlegacy", "html", "<html>bad</html>"));

        Assert.Contains("cannot store secrets", secret.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot store raw html", html.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LearningObserve_AllowsAuthorizedRobrowserTechnicalNote()
    {
        var service = new LearningService(new KnowledgeService(FindRepoRoot()));

        var candidate = service.Observe("robrowserlegacy", "browser stack", "Use Vite and wsProxy notes as architecture context.");

        Assert.Equal("robrowserlegacy", candidate.SourceId);
        Assert.Equal("candidate", candidate.Status);
        Assert.True(candidate.HumanReviewRequired);
        Assert.False(candidate.SafeForApply);
    }

    [Fact]
    public void LearningApproveRejectPromote_AreReadOnly()
    {
        var service = new LearningService(new KnowledgeService(FindRepoRoot()));

        var approve = service.Approve("learning-robrowserlegacy-browser-stack", dryRun: true);
        var reject = service.Reject("learning-rathena-board-routing", "context is too weak");
        var promote = service.Promote("learning-remoteclient-grf-pipeline", dryRun: true);

        Assert.Equal(0, approve.WritesPerformed);
        Assert.Equal("approved", approve.ResultingStatus);
        Assert.Equal(0, reject.WritesPerformed);
        Assert.Equal("rejected", reject.ResultingStatus);
        Assert.Equal(0, promote.WritesPerformed);
        Assert.NotNull(promote.PromotionPlan);
        Assert.False(promote.PromotionPlan!.WriteAllowed);
    }

    [Fact]
    public void KnowledgeSearch_SeesNewAuthorizedLibraries()
    {
        var service = new KnowledgeService(FindRepoRoot());

        var browserMatches = service.Search(new KnowledgeQuery { Query = "WebGL pathfinding", Limit = 10 });
        var remoteClientMatches = service.Search(new KnowledgeQuery { Query = "DATA.INI cache", Limit = 10 });

        Assert.Contains(browserMatches, match => match.EntryId == "robrowserlegacy.browser.stack");
        Assert.Contains(remoteClientMatches, match => match.EntryId == "robrowserlegacy-remoteclient-js.grf.pipeline");
    }

    [Fact]
    public void KnowledgeCommand_SourceExplainRefreshPlanSnapshotsAndLearningWork()
    {
        var root = FindRepoRoot();
        var configDir = Path.Combine(root, "config");

        var sourceExplain = new KnowledgeCommand(configDir, root, "source", new Dictionary<string, string> { ["action"] = "explain", ["id"] = "robrowserlegacy" }).Execute();
        var refreshPlan = new KnowledgeCommand(configDir, root, "refresh", new Dictionary<string, string> { ["action"] = "plan" }).Execute();
        var refreshDue = new KnowledgeCommand(configDir, root, "refresh", new Dictionary<string, string> { ["action"] = "due" }).Execute();
        var snapshots = new KnowledgeCommand(configDir, root, "snapshots", new Dictionary<string, string>()).Execute();
        var learnCandidates = new KnowledgeCommand(configDir, root, "learn", new Dictionary<string, string> { ["action"] = "candidates" }).Execute();
        var learnReport = new KnowledgeCommand(configDir, root, "learn", new Dictionary<string, string> { ["action"] = "report" }).Execute();

        Assert.True(sourceExplain.Ok);
        Assert.True(refreshPlan.Ok);
        Assert.True(refreshDue.Ok);
        Assert.True(snapshots.Ok);
        Assert.True(learnCandidates.Ok);
        Assert.True(learnReport.Ok);
        Assert.Contains("\"safeForApply\": false", sourceExplain.ToJson());
        Assert.Contains("metadata-only", refreshPlan.ToJson());
        Assert.Contains("robrowserlegacy-2026-05-23", snapshots.ToJson());
        Assert.Contains("Learning Candidates Report", learnReport.ToJson());
    }

    [Fact]
    public void OnlineRefresh_RathenaBoard_IsSkippedByPolicyWithoutCrawler()
    {
        var service = new KnowledgeService(FindRepoRoot());
        var refresh = new OnlineKnowledgeRefreshService(FindRepoRoot(), service, metadataClient: new FakeGithubMetadataClient());

        var result = refresh.Run("rathena-board", runAll: false, mode: "metadata").Single();

        Assert.Equal("skipped_by_policy", result.Status);
        Assert.Equal(0, result.RequestCount);
        Assert.False(result.LinksFollowed);
        Assert.False(result.PaginationUsed);
        Assert.False(result.BulkLookup);
        Assert.False(result.RawHtmlStored);
        Assert.False(result.DumpStored);
    }

    [Fact]
    public void OnlineRefresh_GithubMetadata_UsesMockAndAvoidsCodeCopy()
    {
        var service = new KnowledgeService(FindRepoRoot());
        var refresh = new OnlineKnowledgeRefreshService(FindRepoRoot(), service, metadataClient: new FakeGithubMetadataClient());

        var result = refresh.Run("robrowserlegacy", runAll: false, mode: "metadata").Single();

        Assert.Equal("refreshed", result.Status);
        Assert.True(result.AuthorizedUse);
        Assert.True(result.CodeAnalysisAllowed);
        Assert.True(result.SelectiveIncorporationAllowed);
        Assert.False(result.RawHtmlStored);
        Assert.False(result.RawContentStored);
        Assert.False(result.CodeCopied);
        Assert.False(result.Persisted);
    }

    [Fact]
    public void PlanCreate_ReceivesAuthorizedSourceHints()
    {
        var root = FindRepoRoot();
        var configDir = Path.Combine(root, "config");
        var plan = new PlanCommand(configDir, root, "item", 501, "Potion", null, new KnowledgeLookupOptions { WithKnowledge = true }).Execute();

        Assert.True(plan.Ok);
        var json = plan.ToJson();
        Assert.Contains("knowledge.source.remoteclient.asset-pipeline", json);
        Assert.Contains("knowledge.source.robrowser.browser-client", json);
        Assert.Contains("\"safeForApply\": false", json);
    }

    [Fact]
    public void ApiReadinessExport_IncludesRefreshLearningAndAuthorizedSources()
    {
        var command = new ApiReadinessExportCommand(FindRepoRoot());

        var result = command.Execute();
        var json = result.ToJson();

        Assert.True(result.Ok);
        Assert.Contains("supportedLearningFeatures", json);
        Assert.Contains("supportedRefreshFeatures", json);
        Assert.Contains("supportedOnlineSources", json);
        Assert.Contains("supportedAuthorizedCodeSources", json);
        Assert.Contains("Knowledge Sources", json);
    }

    [Fact]
    public void McpPolicy_ExposesReadOnlyRefreshAndLearningToolsOnly()
    {
        var root = FindRepoRoot();
        var registry = new McpToolRegistry(new McpToolContext(root));
        var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("ragnaforge_knowledge_source_explain", toolNames);
        Assert.Contains("ragnaforge_knowledge_source_freshness", toolNames);
        Assert.Contains("ragnaforge_knowledge_refresh_plan", toolNames);
        Assert.Contains("ragnaforge_knowledge_snapshots", toolNames);
        Assert.Contains("ragnaforge_learning_candidates", toolNames);
        Assert.Contains("ragnaforge_learning_report", toolNames);
        Assert.Contains("ragnaforge_authorized_source_notes", toolNames);
        Assert.DoesNotContain("ragnaforge_apply", toolNames);
        Assert.DoesNotContain("ragnaforge_rollback_confirm", toolNames);
        Assert.False(McpToolPolicy.IsAllowed("ragnaforge_apply"));
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

    private sealed class FakeGithubMetadataClient : IOnlineKnowledgeMetadataClient
    {
        public Task<GithubMetadataSnapshot> FetchGithubMetadataAsync(KnowledgeSource source, KnowledgeSourceSnapshot? previousSnapshot, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GithubMetadataSnapshot
            {
                SourceId = source.Id,
                RepoOwner = "test-owner",
                RepoName = source.Id,
                Url = source.ExternalReferenceUrl ?? source.Url,
                DefaultBranch = "main",
                LatestCommitSha = "abc123def456",
                LatestCommitDate = "2026-05-23T00:00:00Z",
                License = source.License,
                Stars = 1,
                Forks = 2,
                TopLevelFiles = ["README.md", "src", "tests"],
                ReadmeSummary = "Authorized repository metadata for testing.",
                UpdateDetected = true,
                PreviousSnapshotVersion = previousSnapshot?.SourceVersion,
                NewSnapshotVersion = "abc123def456",
                AuthorizedUse = true,
                CodeAnalysisAllowed = true,
                SelectiveIncorporationAllowed = true,
                RawContentStored = false,
                CodeCopied = false,
                CanBlock = false
            });
        }
    }
}
