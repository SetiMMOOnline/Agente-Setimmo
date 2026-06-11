using RagnaForge.Agent.Core.Implementation;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Security;

namespace RagnaForge.Agent.Core.Commands;

/// <summary>
/// Runs local stack fixtures in an agent-owned sandbox.
/// It never executes shell commands and never writes outside temp/field-tests.
/// </summary>
public sealed class FieldTestCommand
{
    private readonly string _agentRoot;
    private readonly string _noun;
    private readonly string _verb;
    private readonly bool _keepSandbox;
    private readonly LanguageCapabilityRegistry _registry;

    public FieldTestCommand(
        string agentRoot,
        string noun,
        string verb,
        bool keepSandbox,
        LanguageCapabilityRegistry? registry = null)
    {
        _agentRoot = Path.GetFullPath(agentRoot);
        _noun = noun;
        _verb = verb;
        _keepSandbox = keepSandbox;
        _registry = registry ?? new LanguageCapabilityRegistry();
    }

    public JsonOutput Execute()
    {
        if (!_noun.Equals("test", StringComparison.OrdinalIgnoreCase) ||
            !_verb.Equals("run", StringComparison.OrdinalIgnoreCase))
        {
            return JsonOutput.Error("field-test",
                "Usage: ragnaforge field test run --json [--keep-sandbox]");
        }

        var operationId = JsonOutput.GenerateOperationId();
        var fieldRoot = Path.GetFullPath(Path.Combine(_agentRoot, "temp", "field-tests"));
        var sandboxRoot = Path.GetFullPath(Path.Combine(fieldRoot, operationId));

        if (!PathGuard.IsContainedIn(sandboxRoot, fieldRoot))
        {
            return JsonOutput.Error("field-test", "Field test sandbox path is outside the allowed agent temp root.");
        }

        Directory.CreateDirectory(sandboxRoot);

        var scenarios = new List<FieldScenarioResult>();
        foreach (var fixture in BuildFixtures())
            scenarios.Add(RunScenario(sandboxRoot, fixture));

        var failed = scenarios.Count(s => !s.Passed);
        if (!_keepSandbox && failed == 0)
        {
            try { Directory.Delete(sandboxRoot, recursive: true); }
            catch
            {
                // Best-effort cleanup only. A retained sandbox is reported as a warning below.
            }
        }

        var sandboxRetained = Directory.Exists(sandboxRoot);
        var output = JsonOutput.Success("field-test",
            failed == 0
                ? "Field test harness passed across local stack fixtures."
                : "Field test harness found fixture failures.");
        output.OperationId = operationId;
        output.SafeForAutomation = failed == 0;
        output.NextRequiredAction = failed == 0 ? "none" : "review_field_test_failures";
        output.Warnings = sandboxRetained
            ? [$"Sandbox retained under {Path.GetRelativePath(_agentRoot, sandboxRoot)}."]
            : [];
        output.Data = new
        {
            runId = operationId,
            total = scenarios.Count,
            passed = scenarios.Count - failed,
            failed,
            sandboxRoot = Path.GetRelativePath(_agentRoot, sandboxRoot),
            sandboxRetained,
            writesConfinedToSandbox = true,
            realProjectWrites = false,
            shellExecuted = false,
            fixturesRepresented = scenarios.Select(s => s.Language).Distinct(StringComparer.OrdinalIgnoreCase).Order(),
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = false,
            safeForProductionApply = false,
            scenarios
        };

        if (failed > 0)
        {
            output.Ok = false;
            output.Errors.Add("One or more field test fixtures failed.");
        }

        return output;
    }

    private FieldScenarioResult RunScenario(string sandboxRoot, FieldFixture fixture)
    {
        var scenarioRoot = Path.GetFullPath(Path.Combine(sandboxRoot, fixture.Key));
        var targetPath = Path.GetFullPath(Path.Combine(scenarioRoot, fixture.FileName));

        if (!PathGuard.IsContainedIn(targetPath, scenarioRoot) ||
            !PathGuard.IsContainedIn(scenarioRoot, sandboxRoot))
        {
            return FieldScenarioResult.Failed(fixture, "path_guard_blocked",
                "Fixture target path escaped the sandbox boundary.");
        }

        var capability = _registry.ResolveByPath(targetPath, fixture.Language);
        if (capability is null)
        {
            return FieldScenarioResult.Failed(fixture, "language_not_detected",
                "No language capability matched the fixture target.");
        }

        var review = capability.Validator(targetPath, fixture.InitialContent);
        var desiredContent = capability.Formatter(targetPath, fixture.TargetContent);
        var targetReview = capability.Validator(targetPath, desiredContent);
        var planOk = review.Valid && targetReview.Valid && !string.IsNullOrWhiteSpace(desiredContent);
        var dryRunOk = planOk && !File.Exists(targetPath);
        var applyOk = false;
        var rollbackOk = false;
        var messages = review.Messages
            .Concat(targetReview.Messages)
            .Select(m => new { m.Severity, m.Code, m.Message })
            .ToList();

        if (planOk)
        {
            Directory.CreateDirectory(scenarioRoot);
            File.WriteAllText(targetPath, desiredContent);
            applyOk = File.Exists(targetPath) &&
                      PathGuard.IsContainedIn(Path.GetFullPath(targetPath), sandboxRoot);

            if (applyOk)
            {
                File.Delete(targetPath);
                rollbackOk = !File.Exists(targetPath);
            }
        }

        var passed = planOk && dryRunOk && applyOk && rollbackOk;
        return new FieldScenarioResult(
            fixture.Key,
            fixture.Language,
            fixture.FileName,
            review.Valid,
            planOk,
            dryRunOk,
            applyOk,
            rollbackOk,
            passed,
            passed ? "pass" : "fail",
            messages);
    }

    private static IReadOnlyList<FieldFixture> BuildFixtures() =>
    [
        new("csharp", "csharp", "SamplePolicy.cs",
            "namespace FieldSandbox;\r\npublic static class SamplePolicy { public static bool Enabled => true; }\r\n",
            "namespace FieldSandbox;\r\npublic static class SamplePolicy { public static bool Enabled => false; }\r\n"),
        new("javascript", "javascript", "sample.ts",
            "export const status = \"draft\";\n",
            "export const status = \"validated\";\n"),
        new("html", "html", "index.html",
            "<main><h1>Draft</h1></main>\n",
            "<main><h1>Validated</h1></main>\n"),
        new("css", "css", "styles.css",
            ".status { color: red; }\n",
            ".status { color: green; }\n"),
        new("php", "php", "SampleHandler.php",
            "<?php\nfinal class SampleHandler { public static function status(): string { return 'draft'; } }\n",
            "<?php\nfinal class SampleHandler { public static function status(): string { return 'validated'; } }\n"),
        new("java", "java", "SampleStatus.java",
            "public final class SampleStatus { public String value() { return \"draft\"; } }\n",
            "public final class SampleStatus { public String value() { return \"validated\"; } }\n"),
        new("c", "c", "sample.c",
            "int status(void) { return 0; }\n",
            "int status(void) { return 1; }\n"),
        new("cpp", "cpp", "SampleStatus.cpp",
            "class SampleStatus { public: const char* value() const { return \"draft\"; } };\n",
            "class SampleStatus { public: const char* value() const { return \"validated\"; } };\n"),
        new("python", "python", "sample.py",
            "def status():\n    return \"draft\"\n",
            "def status():\n    return \"validated\"\n"),
        new("lua", "lua", "sample.lua",
            "local status = \"draft\"\nreturn status\n",
            "local status = \"validated\"\nreturn status\n"),
        new("powershell", "powershell", "sample.ps1",
            "[CmdletBinding()]\r\nparam()\r\nWrite-Output \"draft\"\r\n",
            "[CmdletBinding()]\r\nparam()\r\nWrite-Output \"validated\"\r\n"),
        new("shell", "shell", "sample.sh",
            "#!/usr/bin/env bash\nprintf '%s\\n' 'draft'\n",
            "#!/usr/bin/env bash\nprintf '%s\\n' 'validated'\n"),
        new("node-package", "node", "package.json",
            "{\"name\":\"setimmo-field-fixture\",\"private\":true,\"scripts\":{\"test\":\"echo draft\"}}\n",
            "{\"name\":\"setimmo-field-fixture\",\"private\":true,\"scripts\":{\"test\":\"echo validated\"}}\n")
    ];

    private sealed record FieldFixture(
        string Key,
        string Language,
        string FileName,
        string InitialContent,
        string TargetContent);

    private sealed record FieldScenarioResult(
        string Key,
        string Language,
        string FileName,
        bool ReviewPassed,
        bool PlanPassed,
        bool DryRunPassed,
        bool ApplyPassed,
        bool RollbackPassed,
        bool Passed,
        string Status,
        object Messages)
    {
        public static FieldScenarioResult Failed(FieldFixture fixture, string code, string message) =>
            new(fixture.Key, fixture.Language, fixture.FileName, false, false, false, false, false, false, "fail",
                new[] { new { Severity = "error", Code = code, Message = message } });
    }
}
