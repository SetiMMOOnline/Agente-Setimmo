using System.Text.Json;
using RagnaForge.Agent.Core.Knowledge;

namespace RagnaForge.Agent.Core.Tests;

public sealed class InternalReferenceLibraryTests
{
    [Theory]
    [InlineData("divine-pride.source.json", "divine-pride", "https://www.divine-pride.net/")]
    [InlineData("ratemyserver.source.json", "ratemyserver", "https://ratemyserver.net/")]
    public void ExternalReferenceLibrary_MetadataIsReadOnlyAndOffline(string fileName, string expectedId, string expectedUrl)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "knowledge", "sources", fileName);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var json = doc.RootElement;

        Assert.Equal(expectedId, json.GetProperty("id").GetString());
        Assert.Equal("internal_reference_library", json.GetProperty("sourceType").GetString());
        Assert.Equal(expectedUrl, json.GetProperty("externalReferenceUrl").GetString());
        Assert.True(json.GetProperty("readOnly").GetBoolean());
        Assert.Contains("No scraping", json.GetProperty("permissionNote").GetString());
        Assert.Contains("Local project", json.GetProperty("trustPolicy").GetString());
        Assert.Contains("prefer local", json.GetProperty("conflictPolicy").GetString(), StringComparison.OrdinalIgnoreCase);

        var entityTypes = json.GetProperty("supportedEntityTypes").EnumerateArray().Select(e => e.GetString()).ToHashSet();
        foreach (var entityType in new[] { "item", "monster", "map", "npc", "skill", "quest" })
            Assert.Contains(entityType, entityTypes);
    }

    [Fact]
    public void KnowledgeLibrary_ListsGlobalCanonAndReferenceLibraries()
    {
        var root = FindRepoRoot();
        var service = new KnowledgeService(root);
        var sources = service.LoadSources();

        Assert.Contains(sources, s => s.Id == "global-canon" && s.SourceType == "internal_governance");
        Assert.Contains(sources, s => s.Id == "divine-pride" && s.SourceType == "internal_reference_library");
        Assert.Contains(sources, s => s.Id == "ratemyserver" && s.SourceType == "internal_reference_library");
    }

    [Fact]
    public void KnowledgeSearch_RecognizesReferenceLibrariesWithoutNetwork()
    {
        var root = FindRepoRoot();
        var service = new KnowledgeService(root);

        var divinePride = service.Search(new KnowledgeQuery { Query = "Divine Pride provenance", Limit = 10 });
        var rateMyServer = service.Search(new KnowledgeQuery { Query = "RateMyServer provenance", Limit = 10 });

        Assert.Contains(divinePride, r => r.EntryId == "divine-pride.reference.policy");
        Assert.Contains(rateMyServer, r => r.EntryId == "ratemyserver.reference.policy");
    }

    [Fact]
    public void InternalReferenceFiles_DoNotContainOnlineAdaptersOrMassHtml()
    {
        var root = FindRepoRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "knowledge"), "*.*", SearchOption.AllDirectories)
            .Where(file => file.Contains("divine-pride", StringComparison.OrdinalIgnoreCase) ||
                           file.Contains("ratemyserver", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("HttpClient", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<html", content, StringComparison.OrdinalIgnoreCase);
        }
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
