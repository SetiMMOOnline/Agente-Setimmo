using RagnaForge.Agent.Core.Implementation;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class ReviewCodeCommand
{
    private readonly ImplementationWorkflowService _workflow;
    private readonly string _targetPath;
    private readonly string _workspace;
    private readonly string? _languageHint;

    public ReviewCodeCommand(string configDir, string agentRoot, string targetPath, string workspace, string? languageHint)
    {
        _workflow = new ImplementationWorkflowService(configDir, agentRoot);
        _targetPath = targetPath;
        _workspace = workspace;
        _languageHint = languageHint;
    }

    public JsonOutput Execute() => _workflow.ReviewCode(_targetPath, _workspace, _languageHint);
}

public sealed class FixCodeCommand
{
    private readonly ImplementationWorkflowService _workflow;
    private readonly string _targetPath;
    private readonly string _workspace;
    private readonly string? _languageHint;

    public FixCodeCommand(string configDir, string agentRoot, string targetPath, string workspace, string? languageHint)
    {
        _workflow = new ImplementationWorkflowService(configDir, agentRoot);
        _targetPath = targetPath;
        _workspace = workspace;
        _languageHint = languageHint;
    }

    public JsonOutput Execute() => _workflow.FixCode(_targetPath, _workspace, _languageHint);
}

public sealed class CreateContentCommand
{
    private readonly ImplementationWorkflowService _workflow;
    private readonly ImplementationRequest _request;

    public CreateContentCommand(string configDir, string agentRoot, ImplementationRequest request)
    {
        _workflow = new ImplementationWorkflowService(configDir, agentRoot);
        _request = request;
    }

    public JsonOutput Execute() => _workflow.CreateContent(_request);
}

public sealed class PlanImplementCommand
{
    private readonly ImplementationWorkflowService _workflow;
    private readonly ImplementationRequest _request;

    public PlanImplementCommand(string configDir, string agentRoot, ImplementationRequest request)
    {
        _workflow = new ImplementationWorkflowService(configDir, agentRoot);
        _request = request;
    }

    public JsonOutput Execute() => _workflow.PlanImplement(_request);
}

public sealed class DryRunImplementCommand
{
    private readonly ImplementationWorkflowService _workflow;
    private readonly ImplementationRequest _request;

    public DryRunImplementCommand(string configDir, string agentRoot, ImplementationRequest request)
    {
        _workflow = new ImplementationWorkflowService(configDir, agentRoot);
        _request = request;
    }

    public JsonOutput Execute() => _workflow.DryRunImplement(_request);
}

public sealed class ApplyImplementCommand
{
    private readonly ImplementationWorkflowService _workflow;
    private readonly string _operationId;
    private readonly bool _confirm;

    public ApplyImplementCommand(string configDir, string agentRoot, string operationId, bool confirm)
    {
        _workflow = new ImplementationWorkflowService(configDir, agentRoot);
        _operationId = operationId;
        _confirm = confirm;
    }

    public JsonOutput Execute() => _workflow.ApplyImplementation(_operationId, _confirm);
}
