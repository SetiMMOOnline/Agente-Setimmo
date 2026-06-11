using System.Text.Json;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Implementation;

namespace RagnaForge.Agent.Core.Tests;

public sealed class EvaluationObservabilityTests : IDisposable
{
    private readonly string _agentRoot;

    public EvaluationObservabilityTests()
    {
        _agentRoot = Path.Combine(Path.GetTempPath(), $"setimmo_eval_obs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_agentRoot);
        Directory.CreateDirectory(Path.Combine(_agentRoot, "logs", "operations"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "context-packs"));
        Directory.CreateDirectory(Path.Combine(_agentRoot, "knowledge", "lessons"));
        File.WriteAllText(Path.Combine(_agentRoot, "logs", "operations", "sample.json"), "{}");
        File.WriteAllText(Path.Combine(_agentRoot, "context-packs", "governance-pack.md"), "# Governance");
        File.WriteAllText(Path.Combine(_agentRoot, "knowledge", "lessons", "sample.md"), "# Lesson");
    }

    public void Dispose()
    {
        try { Directory.Delete(_agentRoot, recursive: true); } catch { }
    }

    [Fact]
    public void EvalRun_ReturnsOfflineCaseMatrix()
    {
        var output = new AgentEvalCommand(_agentRoot, "run").Execute();

        Assert.True(output.Ok, string.Join(Environment.NewLine, output.Errors));
        var data = ToElement(output.Data);
        Assert.True(data.GetProperty("total").GetInt32() >= 8);
        Assert.Equal(data.GetProperty("total").GetInt32(), data.GetProperty("passed").GetInt32());
        Assert.False(data.GetProperty("requiresOpenAiApiKey").GetBoolean());
        Assert.False(data.GetProperty("safeForApply").GetBoolean());
    }

    [Fact]
    public void ObservabilityReport_SummarizesArtifactsWithoutApplyAuthorization()
    {
        var output = new ObservabilityCommand(_agentRoot, "report").Execute();

        Assert.True(output.Ok);
        var data = ToElement(output.Data);
        Assert.Equal(1, data.GetProperty("operationManifests").GetInt32());
        Assert.Equal(1, data.GetProperty("contextPacks").GetInt32());
        Assert.False(data.GetProperty("metrics").GetProperty("safeForApply").GetBoolean());
    }

    [Fact]
    public void OpenAiReview_PreparesContractWithoutLiveCall()
    {
        var output = new OpenAiReviewCommand(_agentRoot, "review", operationId: null).Execute();

        Assert.True(output.Ok);
        var data = ToElement(output.Data);
        Assert.True(data.GetProperty("contractOnlyMode").GetBoolean());
        Assert.False(data.GetProperty("liveOpenAiCallExecuted").GetBoolean());
        Assert.True(data.GetProperty("requiresApiKeyForLiveReview").GetBoolean());
    }

    [Fact]
    public void FailureLearningRecorder_PersistsKnownSemanticFailure()
    {
        var plan = new SemanticPatchPlan
        {
            PatchQuality = new PatchQualityReport
            {
                Valid = false,
                Reason = "Generated patch does not implement a semantic change.",
                Blockers = ["non_semantic_patch"]
            }
        };

        var path = FailureLearningRecorder.RecordSemanticPatchFailure(
            _agentRoot,
            plan,
            new ImplementationRequest { Instruction = "do something impossible" },
            "src/Sample.cs");

        Assert.EndsWith("NonSemanticPatch.md", path);
        Assert.True(File.Exists(Path.Combine(_agentRoot, path.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Theory]
    [InlineData("standalone-relaxed", 0.72, "standalone-relaxed")]
    [InlineData("local-dev", 0.72, "standalone-relaxed")]
    [InlineData("api-restricted", 0.86, "api-restricted")]
    [InlineData("production-strict", 1.0, "production-strict")]
    public void SafetyProfiles_AreNamedAndThresholded(string input, double threshold, string normalized)
    {
        var safety = new SafetyConfig { OperationProfile = input };

        Assert.Equal(normalized, safety.GetNormalizedOperationProfile());
        Assert.Equal(threshold, safety.GetCodexReviewThreshold());
    }

    private static JsonElement ToElement(object? value) =>
        value is null ? JsonDocument.Parse("{}").RootElement : JsonSerializer.SerializeToElement(value);
}
