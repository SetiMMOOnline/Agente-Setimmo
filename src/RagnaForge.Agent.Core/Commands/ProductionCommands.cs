using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Production;

namespace RagnaForge.Agent.Core.Commands;

public sealed class ProductionCommand
{
    private readonly ProductionWorkflowService _workflow;
    private readonly string _subcommand;
    private readonly string? _operationId;
    private readonly string _environment;
    private readonly string? _approver;
    private readonly string? _reason;
    private readonly int _ttlMinutes;
    private readonly bool _confirm;

    public ProductionCommand(
        string configDir,
        string agentRoot,
        string subcommand,
        string? operationId,
        string environment,
        string? approver,
        string? reason,
        int ttlMinutes,
        bool confirm)
    {
        _workflow = new ProductionWorkflowService(configDir, agentRoot);
        _subcommand = string.IsNullOrWhiteSpace(subcommand) ? "status" : subcommand;
        _operationId = operationId;
        _environment = string.IsNullOrWhiteSpace(environment) ? "local" : environment;
        _approver = approver;
        _reason = reason;
        _ttlMinutes = ttlMinutes;
        _confirm = confirm;
    }

    public JsonOutput Execute()
    {
        if (_subcommand.Equals("audit", StringComparison.OrdinalIgnoreCase))
            return _workflow.Audit();

        if (string.IsNullOrWhiteSpace(_operationId))
            return JsonOutput.Error("production", "Missing required argument: --operation <id>");

        return _subcommand.ToLowerInvariant() switch
        {
            "plan" => _workflow.Plan(_operationId, _environment),
            "review" => _workflow.Review(_operationId, _environment),
            "status" => _workflow.Status(_operationId, _environment),
            "approve" => _workflow.Approve(_operationId, _environment, _approver ?? string.Empty, _reason ?? string.Empty, _ttlMinutes),
            "apply" => _workflow.Apply(_operationId, _environment, _confirm),
            "rollback" => _workflow.Rollback(_operationId, _environment, _confirm),
            _ => JsonOutput.Error("production", "Usage: ragnaforge production plan|review|approve|apply|rollback|status|audit --operation <id>")
        };
    }
}
