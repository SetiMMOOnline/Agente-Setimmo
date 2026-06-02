using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Governance;

namespace RagnaForge.Agent.Core.Tests;

public sealed class OperationGovernanceTests
{
    [Fact]
    public void WarningOnlyAssessment_DoesNotBlockApplySafety()
    {
        var assessment = OperationGovernanceProfiles.EvaluateValidated(
            "validated",
            canon: new()
            {
                Findings = [],
                CanonEnabled = true
            },
            validation: new ValidationDecisionSummary
            {
                SafeForReadOnlyWork = true,
                SafeForDryRun = true,
                SafeForApply = true
            });

        Assert.True(assessment.SafeForReadOnlyWork);
        Assert.True(assessment.SafeForDryRun);
        Assert.True(assessment.SafeForApply);
        Assert.False(assessment.ApplyEnabled);
        Assert.Equal("implement_apply_engine", assessment.RecommendedAction);
    }

    [Fact]
    public void UnknownValidation_BlocksApplyButNotDryRun()
    {
        var assessment = OperationGovernanceProfiles.EvaluateWithoutValidation("find");

        Assert.True(assessment.SafeForReadOnlyWork);
        Assert.True(assessment.SafeForDryRun);
        Assert.False(assessment.SafeForApply);
        Assert.Equal("run_validate", assessment.RecommendedAction);
    }

    [Fact]
    public void PathTraversal_BlocksEveryMode()
    {
        var assessment = OperationGovernanceProfiles.EvaluateValidated(
            "apply",
            canon: new GlobalCanonCheckResult(),
            validation: new ValidationDecisionSummary
            {
                SafeForReadOnlyWork = true,
                SafeForDryRun = true,
                SafeForApply = true
            },
            applyEngineImplemented: true,
            rollbackEngineImplemented: true,
            productionApplyEnabled: true,
            pathTraversalDetected: true);

        Assert.False(assessment.SafeForReadOnlyWork);
        Assert.False(assessment.SafeForDryRun);
        Assert.False(assessment.SafeForApply);
        Assert.False(assessment.SafeForProductionApply);
        Assert.Equal("fix_path_scope", assessment.RecommendedAction);
    }

    [Fact]
    public void ApplyPreview_RequiresARealOperationPlan()
    {
        var assessment = OperationGovernanceProfiles.EvaluateApplyRequestPreview();

        Assert.False(assessment.SafeForApply);
        Assert.False(assessment.ApplyEnabled);
        Assert.Equal("create_plan", assessment.RecommendedAction);
    }

    [Fact]
    public void RollbackPreview_ExposesRollbackCapability()
    {
        var assessment = OperationGovernanceProfiles.EvaluateRollbackRequestPreview();

        Assert.True(assessment.SafeForApply);
        Assert.True(assessment.RollbackEnabled);
        Assert.Equal("keep_dry_run_or_apply_review_mode_only", assessment.RecommendedAction);
    }
}
