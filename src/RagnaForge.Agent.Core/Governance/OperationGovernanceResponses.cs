using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Governance;

public static class OperationGovernanceResponses
{
    public static JsonOutput CreateApplyUnavailable() =>
        CreateUnavailable(
            "apply",
            "Direct apply is not the operational path. Prepare a concrete operation with plan/dry-run, then use 'apply implement --operation <id> --confirm'.",
            OperationGovernanceProfiles.EvaluateApplyRequestPreview(),
            nextRequiredAction: "run_dry_run_implement",
            applyRequested: true,
            rollbackRequested: false);

    public static JsonOutput CreateRollbackUnavailable() =>
        CreateUnavailable(
            "rollback",
            "Direct rollback is not the operational path. Select an applied operation and use 'rollback --id <id> --confirm'.",
            OperationGovernanceProfiles.EvaluateRollbackRequestPreview(),
            nextRequiredAction: "select_applied_operation",
            applyRequested: false,
            rollbackRequested: true);

    private static JsonOutput CreateUnavailable(
        string mode,
        string summary,
        OperationGovernanceAssessment governance,
        string nextRequiredAction,
        bool applyRequested,
        bool rollbackRequested)
    {
        return new JsonOutput
        {
            Ok = false,
            Mode = mode,
            Summary = summary,
            SafeForAutomation = false,
            NextRequiredAction = nextRequiredAction,
            Data = new
            {
                readOnlyMode = true,
                applyRequested,
                rollbackRequested,
                governance
            }
        };
    }
}
