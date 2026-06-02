using RagnaForge.Agent.Core.Grf;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class GrfCommand
{
    private readonly GrfExtractorIntegrationService _service;
    private readonly string _subcommand;
    private readonly string? _source;
    private readonly string? _operationId;
    private readonly bool _confirm;

    public GrfCommand(string configDir, string agentRoot, string subcommand, string? source, string? operationId, bool confirm)
    {
        _service = new GrfExtractorIntegrationService(configDir, agentRoot);
        _subcommand = string.IsNullOrWhiteSpace(subcommand) ? "list" : subcommand;
        _source = source;
        _operationId = operationId;
        _confirm = confirm;
    }

    public JsonOutput Execute()
    {
        return _subcommand.ToLowerInvariant() switch
        {
            "list" => _service.List(),
            "inspect" => string.IsNullOrWhiteSpace(_source)
                ? JsonOutput.Error("grf-inspect", "Usage: ragnaforge grf inspect --source <container.grf>")
                : _service.Inspect(_source),
            "dry-run-extract" => string.IsNullOrWhiteSpace(_source)
                ? JsonOutput.Error("grf-dry-run-extract", "Usage: ragnaforge grf dry-run-extract --source <container.grf>")
                : _service.DryRunExtract(_source),
            "extract" => string.IsNullOrWhiteSpace(_operationId)
                ? JsonOutput.Error("grf-extract", "Usage: ragnaforge grf extract --operation <id> --confirm")
                : _service.Extract(_operationId, _confirm),
            "cleanup" => string.IsNullOrWhiteSpace(_operationId)
                ? JsonOutput.Error("grf-cleanup", "Usage: ragnaforge grf cleanup --operation <id> --confirm")
                : _service.Cleanup(_operationId, _confirm),
            _ => JsonOutput.Error("grf", "Usage: ragnaforge grf list|inspect|dry-run-extract|extract|cleanup")
        };
    }
}
