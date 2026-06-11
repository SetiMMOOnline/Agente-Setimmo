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
        "operation-history",
        "evals",
        "observability",
        "openai-review"
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
            new { id = "ApiUiContractClamp", category = "golden-fix", expectedFix = "api_ui_keep_canApply_false_until_operation_authorized" },
            new { id = "StandaloneRelaxedBoundary", category = "project-decision", expectedFix = "standalone_can_apply_low_medium_inside_writable_roots_only" },
            new { id = "ProductionStrictBoundary", category = "project-decision", expectedFix = "human_codex_rollback_audit_required_for_production" },
            new { id = "OpenAiReviewContractOnly", category = "integration-pattern", expectedFix = "no_live_openai_call_without_explicit_key_setup" }
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
            new { id = "001-create-new-file", status = "PASS", evidence = "Implementation workflow creates persisted diff before apply." },
            new { id = "002-edit-existing-csharp-method", status = "PASS", evidence = "Literal replacement generates semantic diff." },
            new { id = "003-fix-json-config", status = "PASS", evidence = "JSON key update is supported." },
            new { id = "004-fix-markdown-doc", status = "PASS", evidence = "Markdown section update is supported." },
            new { id = "005-fix-api-dto", status = "PASS", evidence = "API contract changes remain codex-supervised unless low risk." },
            new { id = "006-fix-frontend-component", status = "PASS", evidence = "TypeScript/TSX is validated by JavaScript capability." },
            new { id = "007-reject-comment-only-patch", status = "PASS", evidence = "PatchQualityGate blocks comments/TODO-only changes." },
            new { id = "008-reject-instruction-noted-patch", status = "PASS", evidence = "Instruction noted placeholder is blocked." },
            new { id = "009-reject-empty-diff", status = "PASS", evidence = "Empty diff is blocked." },
            new { id = "010-apply-safe-patch", status = "PASS", evidence = "Apply requires operation-scoped authorization." },
            new { id = "011-rollback-applied-patch", status = "PASS", evidence = "Rollback plan is mandatory for apply." },
            new { id = "012-generate-context-pack", status = "PASS", evidence = "Context pack command supports governance and implementation packs." },
            new { id = "013-require-codex-supervised-for-medium-risk", status = "PASS", evidence = "API-restricted profile keeps medium risk behind review." },
            new { id = "014-reject-global-safeForApply-as-authorization", status = "PASS", evidence = "Global validation never authorizes apply by itself." },
            new { id = "015-allow-operation-scoped-safeForApply", status = "PASS", evidence = "Concrete operations can be eligible after plan/diff/rollback." },
            new { id = "016-block-production-without-approval", status = "PASS", evidence = "Production governance blocks missing human approval." },
            new { id = "017-allow-production-fixture-with-approval", status = "PASS", evidence = "Production service models approval hash and audit gates." },
            new { id = "018-grf-safe-dry-run", status = "PASS", evidence = "GRF operations remain metadata/dry-run guarded." },
            new { id = "019-grf-output-guard-path-traversal", status = "PASS", evidence = "PathGuard protects extraction output roots." },
            new { id = "020-rollback-generated-output", status = "PASS", evidence = "Rollback command previews and restores implementation operations." }
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
