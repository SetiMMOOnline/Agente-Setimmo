using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Configuration;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class CanonCommand
{
    private readonly string _configDir;
    private readonly string _agentRoot;

    public CanonCommand(string configDir, string agentRoot)
    {
        _configDir = configDir;
        _agentRoot = agentRoot;
    }

    public JsonOutput Execute()
    {
        var output = JsonOutput.Success("canon-check");

        try
        {
            var loader = new ConfigLoader(_configDir);
            var pathsConfig = loader.LoadPathsConfig();
            var safetyConfig = loader.LoadSafetyConfig();
            var fingerprint = ConfigFingerprint.Generate(pathsConfig, safetyConfig);
            var result = new GlobalCanonValidator(_agentRoot).Check();
            var criticalFindings = result.Findings.Count(f => f.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase));
            var warningFindings = result.Findings.Count(f => f.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));

            output.ActiveProfile = pathsConfig.ActiveProfile;
            output.ConfigFingerprint = fingerprint;
            output.Ok = criticalFindings == 0;
            output.SafeForAutomation = result.SafeForReadOnlyWork;
            output.Summary = criticalFindings == 0
                ? "Global Canon check completed without critical findings."
                : "Global Canon check found critical issues.";
            output.NextRequiredAction = criticalFindings == 0 ? "none" : "fix_canon_findings";
            output.Data = new
            {
                result.CanonEnabled,
                result.ScopePolicyChecked,
                result.DestructiveCommandsPolicyChecked,
                result.SensitiveFilesPolicyChecked,
                result.ApplyRollbackPolicyChecked,
                result.ShellPolicyChecked,
                result.ReportingPolicyChecked,
                result.Findings,
                result.SafeForReadOnlyWork,
                result.SafeForDryRun,
                result.SafeForApply,
                criticalFindings,
                warningFindings
            };
        }
        catch (Exception ex)
        {
            output = JsonOutput.Error("canon-check", ex.Message);
            output.NextRequiredAction = "fix_errors";
        }

        return output;
    }
}
