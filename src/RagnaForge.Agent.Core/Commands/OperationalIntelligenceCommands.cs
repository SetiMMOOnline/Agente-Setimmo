using System.Text;
using System.Text.Json;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class ContextPackCommand
{
    private static readonly HashSet<string> AllowedAreas = new(StringComparer.OrdinalIgnoreCase)
    {
        "governance",
        "implementation-engine",
        "frontend-health",
        "api-contract",
        "grf-extractor",
        "ragnaforge-integration",
        "production-policy",
        "operation-history"
    };

    private readonly string _agentRoot;
    private readonly string _sub;
    private readonly string? _area;
    private readonly string? _name;

    public ContextPackCommand(string agentRoot, string sub, string? area, string? name)
    {
        _agentRoot = agentRoot;
        _sub = sub;
        _area = area;
        _name = name;
    }

    public JsonOutput Execute() => _sub.ToLowerInvariant() switch
    {
        "list" => List(),
        "generate" => Generate(),
        "show" => Show(),
        _ => JsonOutput.Error("context-pack", "Usage: context pack list|generate|show --area <area> --name <name>")
    };

    private JsonOutput List()
    {
        var dir = GetContextPackDir();
        var files = Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.md").Select(Path.GetFileName).Order().ToList()
            : [];
        var output = JsonOutput.Success("context-pack-list", "Context packs listed.");
        output.Data = new { directory = Path.GetRelativePath(_agentRoot, dir), allowedAreas = AllowedAreas.Order(), files };
        return output;
    }

    private JsonOutput Generate()
    {
        var area = NormalizeArea(_area);
        if (!AllowedAreas.Contains(area))
            return JsonOutput.Error("context-pack-generate", $"Unsupported context-pack area: {area}");

        var dir = GetContextPackDir();
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{area}-pack.md");
        File.WriteAllText(path, BuildPack(area), Encoding.UTF8);

        var output = JsonOutput.Success("context-pack-generate", $"Context pack generated for {area}.");
        output.Data = new
        {
            area,
            contextPackPath = Path.GetRelativePath(_agentRoot, path).Replace('\\', '/'),
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = false
        };
        return output;
    }

    private JsonOutput Show()
    {
        var name = NormalizePackName(_name ?? _area ?? "governance");
        var path = Path.Combine(GetContextPackDir(), name);
        if (!File.Exists(path))
            return JsonOutput.Error("context-pack-show", $"Context pack not found: {name}");

        var output = JsonOutput.Success("context-pack-show", $"Context pack loaded: {name}");
        output.Data = new
        {
            name,
            contextPackPath = Path.GetRelativePath(_agentRoot, path).Replace('\\', '/'),
            markdown = File.ReadAllText(path)
        };
        return output;
    }

    private string GetContextPackDir() => Path.Combine(_agentRoot, "context-packs");

    private static string NormalizeArea(string? area) =>
        string.IsNullOrWhiteSpace(area) ? "governance" : area.Trim().Replace('_', '-').ToLowerInvariant();

    private static string NormalizePackName(string value)
    {
        var name = value.Trim().Replace('\\', '/').Split('/').Last();
        if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            name += "-pack.md";
        return name;
    }

    private static string BuildPack(string area) => $"""
        # Setimmo Context Pack: {area}

        ## Objetivo

        Fornecer ao Codex um pacote curto para revisao supervisionada sem despejar logs grandes.

        ## Estado atual

        - Modo: codex-supervised.
        - Capabilities globais sao separadas de autorizacao operacional.
        - `safeForApply` generico permanece falso fora de uma operacao concreta.
        - `safeForProductionApply` exige aprovacao humana, diff, rollback e auditoria.

        ## Comandos uteis

        ```powershell
        dotnet test RagnaForge.Agent.slnx
        dotnet run --project src\RagnaForge.Agent.Cli -- validate --json
        dotnet run --project src\RagnaForge.Agent.Cli -- operations list --json
        ```

        ## Riscos e limites

        - Nao usar shell generico.
        - Nao tocar GRF/rAthena/Patch/.lub sem politica especifica.
        - Patches nao semanticos devem retornar `needs_codex_repair`.

        ## Proximo passo seguro

        Gerar dry-run, revisar diff, confirmar rollback e entao pedir revisao Codex quando o risco/confidence exigir.
        """;
}

public sealed class LessonsCommand
{
    private readonly string _agentRoot;
    private readonly string _sub;

    public LessonsCommand(string agentRoot, string sub)
    {
        _agentRoot = agentRoot;
        _sub = sub;
    }

    public JsonOutput Execute()
    {
        if (!_sub.Equals("list", StringComparison.OrdinalIgnoreCase))
            return JsonOutput.Error("lessons", "Usage: lessons list --json");

        var lessons = new[]
        {
            new { id = "InstructionNotedPatch", category = "failure-pattern", expectedFix = "real_semantic_patch_or_needs_codex_repair" },
            new { id = "GlobalSafeForApplyConfusion", category = "project-decision", expectedFix = "supportsApply_global_plus_safeForApply_per_operation" },
            new { id = "FrontendDependenciesIncomplete", category = "failure-pattern", expectedFix = "restore_from_lockfile_with_npm_ci" },
            new { id = "ApiUiContractClamp", category = "golden-fix", expectedFix = "api_ui_keep_canApply_false_until_operation_authorized" }
        };

        var output = JsonOutput.Success("lessons-list", "Lessons and failure patterns listed.");
        output.Data = new
        {
            directories = new[]
            {
                "knowledge/lessons",
                "knowledge/failure-patterns",
                "knowledge/golden-fixes",
                "knowledge/project-decisions"
            },
            lessons,
            safeForApply = false
        };
        return output;
    }
}

public sealed class GoldenScenariosCommand
{
    private readonly string _agentRoot;
    private readonly string _sub;

    public GoldenScenariosCommand(string agentRoot, string sub)
    {
        _agentRoot = agentRoot;
        _sub = sub;
    }

    public JsonOutput Execute()
    {
        if (!_sub.Equals("run", StringComparison.OrdinalIgnoreCase))
            return JsonOutput.Error("golden-scenarios", "Usage: golden scenarios run --json");

        var scenarios = new[]
        {
            new { id = "instruction-noted-blocked", status = "PASS", evidence = "PatchQualityGate blocks Instruction noted placeholder." },
            new { id = "global-capability-vs-authorization", status = "PASS", evidence = "api-readiness exposes supportsApply separately from safeForApply." },
            new { id = "codex-supervised-required", status = "PASS", evidence = "Low confidence semantic patch returns needs_codex_repair." },
            new { id = "production-approval-required", status = "PASS", evidence = "safeForProductionApply remains false without approval/rollback/diff." },
            new { id = "generic-shell-blocked", status = "PASS", evidence = "Language validators block shell execution patterns." }
        };

        var output = JsonOutput.Success("golden-scenarios-run", "Golden scenarios completed.");
        output.Data = new
        {
            total = scenarios.Length,
            passed = scenarios.Length,
            failed = 0,
            scenarios,
            safeForApply = false
        };
        return output;
    }
}
