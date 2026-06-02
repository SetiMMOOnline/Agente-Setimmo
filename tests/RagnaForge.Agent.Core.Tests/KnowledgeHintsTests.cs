using RagnaForge.Agent.Core.Entities;
using RagnaForge.Agent.Core.Knowledge;

namespace RagnaForge.Agent.Core.Tests;

public sealed class KnowledgeHintsTests
{
    [Fact]
    public void Ranking_LocalSourcesBeatExternalReferences()
    {
        Assert.True(KnowledgeTrustPolicy.PriorityForSource("local-project-data", "local") >
                    KnowledgeTrustPolicy.PriorityForSource("divine-pride", "internal_reference_library"));
        Assert.True(KnowledgeTrustPolicy.PriorityForSource("rathena-local", "local") >
                    KnowledgeTrustPolicy.PriorityForSource("ratemyserver", "internal_reference_library"));
        Assert.True(KnowledgeTrustPolicy.PriorityForSource("global-canon", "internal_governance") >
                    KnowledgeTrustPolicy.PriorityForSource("external-live", "external-live"));
    }

    [Fact]
    public void ExternalReferenceSources_NeverBlockAlone()
    {
        Assert.False(KnowledgeTrustPolicy.CanBlockAlone("divine-pride", "internal_reference_library"));
        Assert.False(KnowledgeTrustPolicy.CanBlockAlone("ratemyserver", "internal_reference_library"));
        Assert.False(KnowledgeTrustPolicy.CanBlockAlone("external-live", "external-live"));
    }

    [Fact]
    public void ControlledLookup_LocalOnlyAndNoLiveReference_BlockLiveLookup()
    {
        var service = new KnowledgeContextService(FindRepoRoot());

        var localOnly = service.DecideLiveLookup("item", 501, null, [], [], new KnowledgeLookupOptions
        {
            WithKnowledge = true,
            KnowledgeLocalOnly = true
        });
        var noLive = service.DecideLiveLookup("item", 501, null, [], [], new KnowledgeLookupOptions
        {
            WithKnowledge = true,
            NoLiveReference = true
        });

        Assert.False(localOnly.LiveLookup);
        Assert.Contains("knowledge-local-only", localOnly.DecisionReason);
        Assert.False(noLive.LiveLookup);
        Assert.Contains("no-live-reference", noLive.DecisionReason);
    }

    [Fact]
    public void ControlledLookup_BlocksBroadOrUnknownEntity()
    {
        var service = new KnowledgeContextService(FindRepoRoot());
        var decision = service.DecideLiveLookup("item", null, null, [], [], new KnowledgeLookupOptions { WithKnowledge = true });

        Assert.False(decision.LiveLookup);
        Assert.Equal(0, decision.RequestCount);
        Assert.False(decision.LinksFollowed);
        Assert.False(decision.BulkLookup);
        Assert.False(decision.RawHtmlStored);
    }

    [Fact]
    public void ControlledLookup_WhenUsefulReturnsPolicyUnavailableWarningWithoutHttp()
    {
        var service = new KnowledgeContextService(FindRepoRoot());
        var decision = service.DecideLiveLookup("item", 501, null, [], [], new KnowledgeLookupOptions
        {
            WithKnowledge = true,
            LiveSource = "divine-pride"
        });

        Assert.False(decision.LiveLookup);
        Assert.Equal(0, decision.RequestCount);
        Assert.Equal("divine-pride", decision.SelectedSource);
        Assert.NotNull(decision.Warning);
        Assert.Contains("no request was sent", decision.Warning);
        Assert.False(decision.LinksFollowed);
        Assert.False(decision.RangeLookup);
    }

    [Fact]
    public void EntityContext_LocalEntityWithReferenceProducesHintsAndProvenance()
    {
        var service = new KnowledgeContextService(FindRepoRoot());
        var local = new ItemEntry { Id = 501, AegisName = "Potion", Name = "Potion", Side = "server" };

        var context = service.BuildContext("item", 501, "Potion", local, new KnowledgeLookupOptions
        {
            WithKnowledge = true,
            KnowledgeLocalOnly = true
        });

        Assert.Contains(context.Hints, h => h.Category == "LocalEntityFound");
        Assert.All(context.Hints, h => Assert.NotEmpty(h.Provenance));
        Assert.False(context.SafeForApply);
        Assert.True(context.SafeForDryRun);
    }

    [Fact]
    public void LocalValidationErrorCreatesErrorHintWithLocalEvidence()
    {
        var issue = new ValidationIssue
        {
            Code = "LOCAL_ERROR",
            EntityType = "item",
            EntityId = "501",
            EntityName = "Potion",
            Message = "Local validation error.",
            SourceFile = "db/import/item_db.yml"
        };

        var hint = KnowledgeContextService.BuildLocalValidationErrorHint(issue);

        Assert.Equal("error", hint.Severity);
        Assert.Equal("LocalValidationError", hint.Category);
        Assert.True(hint.BlocksDryRun);
        Assert.Equal("local-project-data", hint.WinningSource);
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
