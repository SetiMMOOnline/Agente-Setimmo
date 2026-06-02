namespace RagnaForge.Agent.Core.Canon;

public sealed class GlobalCanonCheckResult
{
    public bool CanonEnabled { get; init; } = true;
    public bool ScopePolicyChecked { get; init; } = true;
    public bool DestructiveCommandsPolicyChecked { get; init; } = true;
    public bool SensitiveFilesPolicyChecked { get; init; } = true;
    public bool ApplyRollbackPolicyChecked { get; init; } = true;
    public bool ShellPolicyChecked { get; init; } = true;
    public bool ReportingPolicyChecked { get; init; } = true;
    public List<GlobalCanonFinding> Findings { get; init; } = [];
    public bool SafeForReadOnlyWork => !Findings.Any(f => f.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase));
    public bool SafeForDryRun => SafeForReadOnlyWork;
    public bool SafeForApply => false;
}

public sealed record GlobalCanonFinding(
    string Id,
    string Severity,
    string Category,
    string Message,
    string Evidence,
    string SuggestedAction);

public sealed class GlobalCanonValidator
{
    private readonly string _agentRoot;
    private readonly GlobalCanonPolicy _policy;

    public GlobalCanonValidator(string agentRoot, GlobalCanonPolicy? policy = null)
    {
        _agentRoot = Path.GetFullPath(agentRoot);
        _policy = policy ?? GlobalCanonPolicy.CreateDefault();
    }

    public GlobalCanonCheckResult Check()
    {
        var findings = new List<GlobalCanonFinding>();

        if (!Directory.Exists(_agentRoot))
        {
            findings.Add(new GlobalCanonFinding(
                "canon.agentRoot.missing",
                "critical",
                "scope",
                "agentRoot does not exist.",
                _agentRoot,
                "Configure RAGNAFORGE_AGENT_ROOT or run from a valid Agente Setimmo root."));
            return new GlobalCanonCheckResult { Findings = findings };
        }

        AddPolicyInvariantFindings(findings);
        ScanRepositorySurface(findings);
        CheckRequiredDocs(findings);

        return new GlobalCanonCheckResult { Findings = findings };
    }

    private static void AddPolicyInvariantFindings(List<GlobalCanonFinding> findings)
    {
        findings.Add(new GlobalCanonFinding(
            "canon.apply.blocked",
            "info",
            "apply-policy",
            "Real apply is validator-governed and cannot rely on Canon alone.",
            "safeForApply is derived from validators, not hardcoded in Canon",
            "Use validation, diff, rollback, secret, path, and capability checks before enabling apply."));

        findings.Add(new GlobalCanonFinding(
            "canon.rollback.blocked",
            "info",
            "rollback-policy",
            "Real rollback requires a dedicated validator-gated execution workflow.",
            "rollback list/dry-run only until execution engine exists",
            "Keep rollback confirm unavailable in CLI/API/MCP until rollback execution is implemented."));
    }

    private void ScanRepositorySurface(List<GlobalCanonFinding> findings)
    {
        var pending = new Queue<string>();
        pending.Enqueue(_agentRoot);

        while (pending.Count > 0)
        {
            var dir = pending.Dequeue();
            IEnumerable<string> subdirs;
            IEnumerable<string> files;

            try
            {
                subdirs = Directory.EnumerateDirectories(dir);
                files = Directory.EnumerateFiles(dir);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var subdir in subdirs)
            {
                var name = Path.GetFileName(subdir);
                if (name.Equals(".git", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (_policy.IsForbiddenArtifactDirectory(subdir))
                {
                    findings.Add(new GlobalCanonFinding(
                        $"canon.artifact.{name.ToLowerInvariant()}",
                        "warning",
                        "forbidden-artifact",
                        $"Generated or external artifact directory detected: {name}.",
                        ToRelative(subdir),
                        "Keep generated artifacts out of commits and validation bundles."));
                    continue;
                }

                pending.Enqueue(subdir);
            }

            foreach (var file in files)
            {
                if (_policy.IsSensitiveFile(file))
                {
                    findings.Add(new GlobalCanonFinding(
                        "canon.sensitive-file.detected",
                        "critical",
                        "sensitive-files",
                        "Sensitive or private asset file detected under agentRoot.",
                        ToRelative(file),
                        "Remove from versioned deliverables and keep only local templates or sanitized fixtures."));
                    continue;
                }

                CheckSmallTextFile(file, findings);
            }
        }
    }

    private void CheckSmallTextFile(string file, List<GlobalCanonFinding> findings)
    {
        var extension = Path.GetExtension(file);
        if (!IsTextLike(extension))
            return;

        try
        {
            var info = new FileInfo(file);
            if (info.Length > 512 * 1024)
                return;

            var content = File.ReadAllText(file);
            if (_policy.IsDestructiveCommand(content))
            {
                findings.Add(new GlobalCanonFinding(
                    "canon.destructive-token.detected",
                    "warning",
                    "destructive-commands",
                    "Destructive command token found in a text file.",
                    ToRelative(file),
                    "Verify this is documentation, test coverage, or guarded code only."));
            }

            if (ContainsSecretToken(content))
            {
                findings.Add(new GlobalCanonFinding(
                    "canon.secret-token.detected",
                    "critical",
                    "sensitive-files",
                    "Possible secret token found in a text file.",
                    ToRelative(file),
                    "Remove secrets and replace with example placeholders."));
            }

            if (content.Contains("Process.Start", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("cmd.exe", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("powershell.exe", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new GlobalCanonFinding(
                    "canon.shell-token.detected",
                    "warning",
                    "shell-policy",
                    "Shell or process execution token found.",
                    ToRelative(file),
                    "Verify execution is allowlisted, bounded, and not a generic shell escape."));
            }
        }
        catch
        {
            // Canon checks must never fail closed over an unreadable optional file.
        }
    }

    private void CheckRequiredDocs(List<GlobalCanonFinding> findings)
    {
        var canonDoc = Path.Combine(_agentRoot, "docs", "CANONE_GLOBAL_DE_REGRAS.md");
        if (!File.Exists(canonDoc))
        {
            findings.Add(new GlobalCanonFinding(
                "canon.doc.missing",
                "warning",
                "reporting",
                "Global Canon document is missing.",
                ToRelative(canonDoc),
                "Create docs/CANONE_GLOBAL_DE_REGRAS.md and keep it aligned with runtime policy."));
        }
    }

    private static bool IsTextLike(string extension) =>
        extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".yml", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsSecretToken(string content) =>
        content.Contains("sk" + "-", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("api" + "_key", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("private" + "_key", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("password" + "=", StringComparison.OrdinalIgnoreCase);

    private string ToRelative(string path) =>
        Path.GetRelativePath(_agentRoot, path).Replace('\\', '/');
}
