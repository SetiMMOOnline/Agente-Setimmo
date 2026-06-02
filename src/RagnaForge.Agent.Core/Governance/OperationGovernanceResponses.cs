using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Governance;

public static class OperationGovernanceResponses
{
    public static JsonOutput CreateApplyUnavailable() =>
        CreateUnavailable(
            "apply",
            "Use 'apply implement --operation <id> --confirm' after a validated dry-run. Direct apply without an operation remains unavailable.",
            OperationGovernanceProfiles.EvaluateApplyRequestPreview(),
            nextRequiredAction: "run_dry_run_implement",
            applyRequested: true,
            rollbackRequested: false);

    public static JsonOutput CreateRollbackUnavailable() =>
        CreateUnavailable(
            "rollback",
            "Use 'rollback --id <id> --confirm' only for an applied implementation operation. Direct rollback without an operation remains unavailable.",
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
