using RagnaForge.Agent.Core.Commands;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Implementation;
using RagnaForge.Agent.Core.Knowledge;
using RagnaForge.Agent.Core.Logging;
using RagnaForge.Agent.Core.Output;
using RagnaForge.Agent.Core.Runtime;

namespace RagnaForge.Agent.Cli;

/// <summary>
/// Agente Setimmo CLI entry point.
/// All commands return JSON. Fatal errors also return JSON.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception ex)
        {
            Console.WriteLine(JsonOutput.Fatal(ex.Message).ToJson());
            return 2;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0) { PrintUsage(); return 1; }

        var command = args[0].ToLowerInvariant();

        if (command is "--help" or "-h" or "help") { PrintUsage(); return 0; }
        if (command is "--version" or "version")
        {
            Console.WriteLine($"{{\"version\":\"{RagnaForge.Agent.Core.AgentVersion.Current}\",\"name\":\"Agente Setimmo\"}}");
            return 0;
        }

        var resolution = AgentRootResolver.Resolve(AppContext.BaseDirectory);
        var agentRoot = resolution.AgentRoot;
        var configDir = Path.Combine(agentRoot, "config");

        if (!resolution.ConfigExists)
            return EmitWithoutLog(new JsonOutput
            {
                Ok = false,
                Mode = command,
                Summary = "RagnaForge agentRoot could not be resolved.",
                Errors =
                [
                    $"Missing required config file: {Path.Combine(configDir, "paths.json")}",
                    $"Set {AgentRootResolver.EnvironmentVariable} to the Agente Setimmo root or run scripts/install.ps1 again."
                ],
                NextRequiredAction = "configure_agent_root",
                SafeForAutomation = false,
                Data = new { attemptedAgentRoot = agentRoot, resolutionSource = resolution.Source }
            });

        switch (command)
        {
            case "status": return Emit(agentRoot, new StatusCommand(configDir, agentRoot).Execute(), "status");
            case "doctor": return Emit(agentRoot, new DoctorCommand(configDir, agentRoot).Execute(), "doctor");
            case "baseline": return Emit(agentRoot, new BaselineCommand(configDir, agentRoot).Execute(), "baseline");
            case "health": return Emit(agentRoot, new HealthCommand(configDir, agentRoot).Execute(), "health");

            case "scan":
                if (!HasFlag(args, "--project"))
                    return Emit(agentRoot, JsonOutput.Error("scan",
                        "Missing required argument: --project. Usage: ragnaforge scan --project --json"), "scan");
                return Emit(agentRoot, new ScanCommand(configDir, agentRoot).Execute(), "scan");

            case "config":
                return RunConfig(args, configDir, agentRoot);
            case "profile":
                return RunProfile(args, configDir, agentRoot);
            case "index":
                return RunIndex(args, configDir, agentRoot);
            case "find":
                return RunFind(args, configDir, agentRoot);
            case "validate":
                return RunValidate(args, configDir, agentRoot);
            case "canon":
                return RunCanon(args, configDir, agentRoot);
            case "triage":
                return RunTriage(args, configDir, agentRoot);
            case "dry-run":
                return RunDryRun(args, configDir, agentRoot);
            case "cleanup":
                return RunCleanup(args, agentRoot);
            case "diff":
                return RunDiff(args, agentRoot);
            case "report":
                return RunReport(args, agentRoot);
            case "review":
                return RunReview(args, configDir, agentRoot);
            case "fix":
                return RunFix(args, configDir, agentRoot);
            case "create":
                return RunCreate(args, configDir, agentRoot);
            case "rollback":
                return RunRollback(args, configDir, agentRoot);
            case "knowledge":
                return RunKnowledge(args, configDir, agentRoot);
            case "plan":
                return RunPlan(args, configDir, agentRoot);
            case "export":
                return RunExport(args, agentRoot);
            case "production":
                return RunProduction(args, configDir, agentRoot);
            case "operations":
                return RunOperations(args, agentRoot);
            case "grf":
                return RunGrf(args, configDir, agentRoot);
            case "context":
                return RunContext(args, agentRoot);
            case "lessons":
                return RunLessons(args, agentRoot);
            case "golden":
                return RunGolden(args, agentRoot);
            case "field":
                return RunField(args, agentRoot);
            case "eval":
                return RunEval(args, agentRoot);
            case "observability":
                return RunObservability(args, agentRoot);
            case "openai":
                return RunOpenAi(args, agentRoot);

            case "apply":
                return RunApply(args, configDir, agentRoot);

            default:
                return Emit(agentRoot, JsonOutput.Error("unknown", $"Unknown command: {command}"), "unknown");
        }
    }

    private static int RunConfig(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "";
        var key = GetArg(args, 2);
        var value = GetArg(args, 3);
        return Emit(agentRoot, new ConfigCommand(configDir, agentRoot, sub, key, value).Execute(), "config");
    }

    private static int RunProfile(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "";
        var name = GetArg(args, 2);
        return Emit(agentRoot, new ProfileCommand(configDir, agentRoot, sub, name).Execute(), "profile");
    }

    private static int RunIndex(string[] args, string configDir, string agentRoot)
    {
        var scope = "entities";
        if (HasFlag(args, "--items")) scope = "items";
        else if (HasFlag(args, "--npcs")) scope = "npcs";
        else if (HasFlag(args, "--monsters")) scope = "monsters";
        else if (HasFlag(args, "--maps")) scope = "maps";
        else if (HasFlag(args, "--entities")) scope = "entities";

        return Emit(agentRoot, new IndexCommand(configDir, agentRoot, scope).Execute(), "index");
    }

    private static int RunFind(string[] args, string configDir, string agentRoot)
    {
        var entityType = GetArg(args, 1); // item, npc, monster, map
        if (string.IsNullOrWhiteSpace(entityType))
            return Emit(agentRoot, JsonOutput.Error("find",
                "Usage: ragnaforge find <item|npc|monster|map> --id <id> | --name <name>"), "find");

        int? id = null;
        string? name = null;

        var idVal = GetFlagValue(args, "--id");
        if (idVal != null && int.TryParse(idVal, out var parsedId)) id = parsedId;
        name = GetFlagValue(args, "--name");

        if (id is null && name is null)
            return Emit(agentRoot, JsonOutput.Error("find", "Specify --id or --name."), "find");

        return Emit(agentRoot, new FindCommand(configDir, agentRoot, entityType, id, name, ParseKnowledgeOptions(args)).Execute(), "find");
    }

    private static int RunValidate(string[] args, string configDir, string agentRoot)
    {
        if (HasFlag(args, "--canon"))
            return Emit(agentRoot, new CanonCommand(configDir, agentRoot).Execute(), "canon-check");

        var scope = "all";
        if (HasFlag(args, "--items")) scope = "items";
        else if (HasFlag(args, "--npcs")) scope = "npcs";
        else if (HasFlag(args, "--monsters")) scope = "monsters";
        else if (HasFlag(args, "--maps")) scope = "maps";
        else if (HasFlag(args, "--client")) scope = "client";
        else if (HasFlag(args, "--server")) scope = "server";

        return Emit(agentRoot, new ValidateCommand(configDir, agentRoot, scope).Execute(), "validate");
    }

    private static int RunCanon(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "check";
        if (!sub.Equals("check", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, JsonOutput.Error("canon", "Usage: ragnaforge canon check --json"), "canon");

        return Emit(agentRoot, new CanonCommand(configDir, agentRoot).Execute(), "canon-check");
    }

    private static int RunTriage(string[] args, string configDir, string agentRoot)
    {
        var externalOnly = HasFlag(args, "--external-data");
        var format = GetFlagValue(args, "--format") ?? "json";
        return Emit(agentRoot, new TriageCommand(configDir, agentRoot, externalOnly, format).Execute(), "triage");
    }

    private static int RunDryRun(string[] args, string configDir, string agentRoot)
    {
        var entityType = GetArg(args, 1);
        if (entityType != null && entityType.Equals("implement", StringComparison.OrdinalIgnoreCase))
        {
            var request = ParseImplementationRequest(args, workspaceDefault: "main");
            return Emit(agentRoot, new DryRunImplementCommand(configDir, agentRoot, request).Execute(), "dry-run-implement");
        }

        var inputPath = GetFlagValue(args, "--input");

        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(inputPath))
            return Emit(agentRoot, JsonOutput.Error("dry-run",
                "Usage: ragnaforge dry-run <item|npc|monster|map> --input <file.json>"), "dry-run");

        return Emit(agentRoot, new DryRunCommand(configDir, agentRoot, entityType, inputPath).Execute(), "dry-run");
    }

    private static int RunCleanup(string[] args, string agentRoot)
    {
        var safe = HasFlag(args, "--safe");
        var includeLogs = HasFlag(args, "--include-logs");
        var includeCache = HasFlag(args, "--include-cache");
        var includeInputs = HasFlag(args, "--include-inputs");

        if (!safe)
            return Emit(agentRoot, JsonOutput.Error("cleanup",
                "Usage: ragnaforge cleanup --safe [--include-logs] [--include-cache] [--include-inputs]"), "cleanup");

        return Emit(agentRoot, new CleanupCommand(agentRoot, includeLogs, includeCache, includeInputs).Execute(), "cleanup");
    }

    private static int RunDiff(string[] args, string agentRoot)
    {
        var last = HasFlag(args, "--last");
        var opId = GetFlagValue(args, "--operation");

        if (!last && opId is null)
            return Emit(agentRoot, JsonOutput.Error("diff",
                "Usage: ragnaforge diff --last | --operation <id>"), "diff");

        return Emit(agentRoot, new DiffCommand(agentRoot, opId, last).Execute(), "diff");
    }

    private static int RunReport(string[] args, string agentRoot)
    {
        var last = HasFlag(args, "--last");
        var opId = GetFlagValue(args, "--operation");
        var format = GetFlagValue(args, "--format") ?? "json";
        if (HasFlag(args, "--knowledge"))
            return Emit(agentRoot, new KnowledgeReportCommand(agentRoot, format).Execute(), "report-knowledge");
        if (HasFlag(args, "--external-data"))
            return Emit(agentRoot, new ExternalDataReportCommand(agentRoot, format).Execute(), "report-external-data");
        if (HasFlag(args, "--readiness-summary"))
            return Emit(agentRoot, new ReadinessSummaryReportCommand(agentRoot, format).Execute(), "report-readiness-summary");
        if (HasFlag(args, "--entity-plan"))
        {
            var entityType = GetFlagValue(args, "--entity-type") ?? "item";
            var idVal = GetFlagValue(args, "--id");
            int? id = int.TryParse(idVal, out var parsedId) ? parsedId : null;
            var name = GetFlagValue(args, "--name");
            var map = GetFlagValue(args, "--map");
            return Emit(agentRoot, new EntityPlanReportCommand(agentRoot, entityType, id, name, map, format, ParseKnowledgeOptions(args)).Execute(), "report-entity-plan");
        }

        if (!last && opId is null)
            return Emit(agentRoot, JsonOutput.Error("report",
                "Usage: ragnaforge report --last | --operation <id> --format json|md"), "report");

        return Emit(agentRoot, new ReportCommand(agentRoot, opId, last, format).Execute(), "report");
    }

    private static int RunRollback(string[] args, string configDir, string agentRoot)
    {
        var list = HasFlag(args, "--list");
        var id = GetFlagValue(args, "--id");
        var dryRun = HasFlag(args, "--dry-run");
        var confirm = HasFlag(args, "--confirm");

        return Emit(agentRoot, new RollbackCommand(configDir, agentRoot, id, list, dryRun, confirm).Execute(), "rollback");
    }

    private static int RunKnowledge(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "search";
        var dict = new Dictionary<string, string>();
        if (sub.Equals("pack", StringComparison.OrdinalIgnoreCase) ||
            sub.Equals("source", StringComparison.OrdinalIgnoreCase) ||
            sub.Equals("refresh", StringComparison.OrdinalIgnoreCase) ||
            sub.Equals("snapshot", StringComparison.OrdinalIgnoreCase) ||
            sub.Equals("learn", StringComparison.OrdinalIgnoreCase))
        {
            dict["action"] = GetArg(args, 2) ??
                (sub.Equals("refresh", StringComparison.OrdinalIgnoreCase) && (HasFlag(args, "--all") || GetFlagValue(args, "--source") is not null) ? "run" : null) ??
                "explain";
        }

        var query = GetFlagValue(args, "--query");
        if (query != null) dict["query"] = query;
        else if (sub.Equals("ask", StringComparison.OrdinalIgnoreCase) && GetArg(args, 2) is { } question)
            dict["query"] = question;

        var topic = GetFlagValue(args, "--topic");
        if (topic != null) dict["topic"] = topic;

        var id = GetFlagValue(args, "--id");
        if (id != null) dict["id"] = id;

        var summary = GetFlagValue(args, "--summary");
        if (summary != null) dict["summary"] = summary;

        var reason = GetFlagValue(args, "--reason");
        if (reason != null) dict["reason"] = reason;

        var entity = GetFlagValue(args, "--entity");
        if (entity != null) dict["entity"] = entity;
        var entityType = GetFlagValue(args, "--entity-type");
        if (entityType != null) dict["entityType"] = entityType;
        foreach (var kv in ParseKnowledgeOptionDictionary(args))
            dict[kv.Key] = kv.Value;
        var source = GetFlagValue(args, "--source");
        if (source != null) dict["source"] = source;
        var mode = GetFlagValue(args, "--mode");
        if (mode != null) dict["mode"] = mode;
        if (HasFlag(args, "--all")) dict["all"] = "true";
        if (HasFlag(args, "--dry-run")) dict["dryRun"] = "true";

        return Emit(agentRoot, new KnowledgeCommand(configDir, agentRoot, sub, dict).Execute(), "knowledge");
    }

    private static int RunPlan(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "";
        if (sub.Equals("implement", StringComparison.OrdinalIgnoreCase))
        {
            var request = ParseImplementationRequest(args, workspaceDefault: "main");
            return Emit(agentRoot, new PlanImplementCommand(configDir, agentRoot, request).Execute(), "plan-implement");
        }

        var entityType = GetArg(args, 2) ?? "";
        var idVal = GetFlagValue(args, "--id");
        int? id = int.TryParse(idVal, out var parsedId) ? parsedId : null;
        var name = GetFlagValue(args, "--name");
        var map = GetFlagValue(args, "--map");

        if (!sub.Equals("create", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(entityType))
            return Emit(agentRoot, JsonOutput.Error("plan", "Usage: ragnaforge plan create <item|monster|equipment|map|npc|skill|quest> --id <id> --name <name> --with-knowledge --json"), "plan");

        return Emit(agentRoot, new PlanCommand(configDir, agentRoot, entityType, id, name, map, ParseKnowledgeOptions(args)).Execute(), "plan");
    }

    private static int RunExport(string[] args, string agentRoot)
    {
        var target = GetArg(args, 1) ?? "";
        if (!target.Equals("api-readiness", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, JsonOutput.Error("export", "Usage: ragnaforge export api-readiness --json"), "export");

        return Emit(agentRoot, new ApiReadinessExportCommand(agentRoot).Execute(), "export-api-readiness");
    }

    private static int RunProduction(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "status";
        var operationId = GetFlagValue(args, "--operation");
        var environment = GetFlagValue(args, "--environment") ?? "local";
        var approver = GetFlagValue(args, "--approver");
        var reason = GetFlagValue(args, "--reason");
        var ttl = int.TryParse(GetFlagValue(args, "--ttl-minutes"), out var parsedTtl) ? parsedTtl : 1440;
        var confirm = HasFlag(args, "--confirm");

        return Emit(agentRoot,
            new ProductionCommand(configDir, agentRoot, sub, operationId, environment, approver, reason, ttl, confirm).Execute(),
            $"production-{sub}");
    }

    private static int RunOperations(string[] args, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "list";
        var operationId = GetFlagValue(args, "--operation") ?? GetFlagValue(args, "--id");
        var left = GetFlagValue(args, "--left");
        var right = GetFlagValue(args, "--right");

        return Emit(agentRoot, new OperationsCommand(agentRoot, sub, operationId, left, right).Execute(), $"operations-{sub}");
    }

    private static int RunGrf(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "list";
        var source = GetFlagValue(args, "--source") ?? GetFlagValue(args, "--container");
        var operationId = GetFlagValue(args, "--operation") ?? GetFlagValue(args, "--id");
        var confirm = HasFlag(args, "--confirm");

        return Emit(agentRoot, new GrfCommand(configDir, agentRoot, sub, source, operationId, confirm).Execute(), $"grf-{sub}");
    }

    private static int RunContext(string[] args, string agentRoot)
    {
        var noun = GetArg(args, 1) ?? string.Empty;
        if (!noun.Equals("pack", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, JsonOutput.Error("context", "Usage: context pack list|generate|show --area <area>"), "context");

        var sub = GetArg(args, 2) ?? "list";
        var area = GetFlagValue(args, "--area");
        var name = GetFlagValue(args, "--name");
        return Emit(agentRoot, new ContextPackCommand(agentRoot, sub, area, name).Execute(), $"context-pack-{sub}");
    }

    private static int RunLessons(string[] args, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "list";
        return Emit(agentRoot, new LessonsCommand(agentRoot, sub).Execute(), $"lessons-{sub}");
    }

    private static int RunGolden(string[] args, string agentRoot)
    {
        var noun = GetArg(args, 1) ?? string.Empty;
        if (!noun.Equals("scenarios", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, JsonOutput.Error("golden", "Usage: golden scenarios run --json"), "golden");

        var sub = GetArg(args, 2) ?? "run";
        return Emit(agentRoot, new GoldenScenariosCommand(agentRoot, sub).Execute(), $"golden-scenarios-{sub}");
    }

    private static int RunField(string[] args, string agentRoot)
    {
        var noun = GetArg(args, 1) ?? string.Empty;
        var verb = GetArg(args, 2) ?? string.Empty;
        var keepSandbox = HasFlag(args, "--keep-sandbox");
        return Emit(agentRoot, new FieldTestCommand(agentRoot, noun, verb, keepSandbox).Execute(), "field-test");
    }

    private static int RunEval(string[] args, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "run";
        return Emit(agentRoot, new AgentEvalCommand(agentRoot, sub).Execute(), $"eval-{sub}");
    }

    private static int RunObservability(string[] args, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "report";
        return Emit(agentRoot, new ObservabilityCommand(agentRoot, sub).Execute(), $"observability-{sub}");
    }

    private static int RunOpenAi(string[] args, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? "review";
        var operationId = GetFlagValue(args, "--operation") ?? GetFlagValue(args, "--id");
        return Emit(agentRoot, new OpenAiReviewCommand(agentRoot, sub, operationId).Execute(), $"openai-{sub}");
    }

    private static int RunApply(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? string.Empty;
        if (!sub.Equals("implement", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, OperationGovernanceResponses.CreateApplyUnavailable(), "apply");

        var operationId = GetFlagValue(args, "--operation");
        var confirm = HasFlag(args, "--confirm");
        if (string.IsNullOrWhiteSpace(operationId))
            return Emit(agentRoot, JsonOutput.Error("apply-implement",
                "Usage: ragnaforge apply implement --operation <id> --confirm"), "apply-implement");

        return Emit(agentRoot, new ApplyImplementCommand(configDir, agentRoot, operationId, confirm).Execute(), "apply-implement");
    }

    private static int RunReview(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? string.Empty;
        if (!sub.Equals("code", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, JsonOutput.Error("review", "Usage: ragnaforge review code --target <path> [--workspace main|agent] [--language <key>]"), "review");

        var target = GetFlagValue(args, "--target");
        if (string.IsNullOrWhiteSpace(target))
            return Emit(agentRoot, JsonOutput.Error("review-code", "Missing required argument: --target"), "review-code");

        var workspace = GetFlagValue(args, "--workspace") ?? "main";
        var language = GetFlagValue(args, "--language");
        return Emit(agentRoot, new ReviewCodeCommand(configDir, agentRoot, target, workspace, language).Execute(), "review-code");
    }

    private static int RunFix(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? string.Empty;
        if (!sub.Equals("code", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, JsonOutput.Error("fix", "Usage: ragnaforge fix code --target <path> [--workspace main|agent] [--language <key>]"), "fix");

        var target = GetFlagValue(args, "--target");
        if (string.IsNullOrWhiteSpace(target))
            return Emit(agentRoot, JsonOutput.Error("fix-code", "Missing required argument: --target"), "fix-code");

        var workspace = GetFlagValue(args, "--workspace") ?? "main";
        var language = GetFlagValue(args, "--language");
        return Emit(agentRoot, new FixCodeCommand(configDir, agentRoot, target, workspace, language).Execute(), "fix-code");
    }

    private static int RunCreate(string[] args, string configDir, string agentRoot)
    {
        var sub = GetArg(args, 1) ?? string.Empty;
        if (!sub.Equals("content", StringComparison.OrdinalIgnoreCase))
            return Emit(agentRoot, JsonOutput.Error("create", "Usage: ragnaforge create content --target <path> --language <key> [--template <name>] [--title <text>] [--name <name>] [--description <text>] [--workspace main|agent]"), "create");

        var request = ParseImplementationRequest(args, workspaceDefault: "main");
        if (string.IsNullOrWhiteSpace(request.TargetPath))
            return Emit(agentRoot, JsonOutput.Error("create-content", "Missing required argument: --target"), "create-content");
        if (string.IsNullOrWhiteSpace(request.LanguageHint))
            return Emit(agentRoot, JsonOutput.Error("create-content", "Missing required argument: --language"), "create-content");

        request.Intent = ImplementationIntent.CreateContent;
        return Emit(agentRoot, new CreateContentCommand(configDir, agentRoot, request).Execute(), "create-content");
    }

    // --- Helpers ---

    private static int Emit(string agentRoot, JsonOutput result, string operation)
    {
        LogResult(agentRoot, result, operation);
        Console.WriteLine(result.ToJson());
        return result.Ok ? 0 : 1;
    }

    private static int EmitWithoutLog(JsonOutput result)
    {
        Console.WriteLine(result.ToJson());
        return result.Ok ? 0 : 1;
    }

    private static void LogResult(string agentRoot, JsonOutput result, string operation)
    {
        try
        {
            var logger = new AgentLogger(agentRoot);
            logger.EnsureDirectories();
            logger.LogAgent(result.OperationId, result.ActiveProfile ?? "unknown",
                result.ConfigFingerprint ?? "unknown", operation,
                result.Ok ? "success" : "failure", result.Warnings, result.Errors);
        }
        catch { }
    }

    private static bool HasFlag(string[] args, string flag) =>
        args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetArg(string[] args, int index) =>
        index < args.Length && !args[index].StartsWith("--") ? args[index] : null;

    private static string? GetFlagValue(string[] args, string flag)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    private static KnowledgeLookupOptions ParseKnowledgeOptions(string[] args) => new()
    {
        WithKnowledge = HasFlag(args, "--with-knowledge"),
        KnowledgeLocalOnly = HasFlag(args, "--knowledge-local-only"),
        NoLiveReference = HasFlag(args, "--no-live-reference"),
        LiveSource = GetFlagValue(args, "--live-source") ?? "auto",
        LiveTimeoutMs = int.TryParse(GetFlagValue(args, "--live-timeout-ms"), out var timeout) ? timeout : 3000,
        MaxLiveRequestsPerSource = int.TryParse(GetFlagValue(args, "--max-live-requests"), out var maxSource) ? maxSource : 1,
        MaxTotalLiveRequests = int.TryParse(GetFlagValue(args, "--max-total-live-requests"), out var maxTotal) ? maxTotal : 2,
        AllowSanitizedMetadataCache = HasFlag(args, "--allow-sanitized-metadata-cache")
    };

    private static Dictionary<string, string> ParseKnowledgeOptionDictionary(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (HasFlag(args, "--knowledge-local-only")) dict["knowledgeLocalOnly"] = "true";
        if (HasFlag(args, "--no-live-reference")) dict["noLiveReference"] = "true";
        if (HasFlag(args, "--allow-sanitized-metadata-cache")) dict["allowSanitizedMetadataCache"] = "true";
        if (GetFlagValue(args, "--live-source") is { } liveSource) dict["liveSource"] = liveSource;
        if (GetFlagValue(args, "--live-timeout-ms") is { } timeout) dict["liveTimeoutMs"] = timeout;
        if (GetFlagValue(args, "--max-live-requests") is { } maxLive) dict["maxLiveRequests"] = maxLive;
        if (GetFlagValue(args, "--max-total-live-requests") is { } maxTotal) dict["maxTotalLiveRequests"] = maxTotal;
        return dict;
    }

    private static ImplementationRequest ParseImplementationRequest(string[] args, string workspaceDefault)
    {
        return new ImplementationRequest
        {
            Workspace = GetFlagValue(args, "--workspace") ?? workspaceDefault,
            TargetPath = GetFlagValue(args, "--target") ?? string.Empty,
            LanguageHint = GetFlagValue(args, "--language"),
            Template = GetFlagValue(args, "--template"),
            Title = GetFlagValue(args, "--title"),
            Name = GetFlagValue(args, "--name"),
            Description = GetFlagValue(args, "--description"),
            Instruction = GetFlagValue(args, "--instruction"),
            ContentFilePath = GetFlagValue(args, "--content-file"),
            Content = GetFlagValue(args, "--content")
        };
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
        Agente Setimmo CLI

        Usage: ragnaforge <command> [options]

        Diagnostics:
          status                  Show agent status (read-only)
          doctor                  Validate configuration and security (read-only)
          baseline                Run status + doctor + scan + index + validate
          health                  Return compact operational health summary
          scan --project          Scan and index project files (read-only)

        Config & Profiles:
          config get              Show current configuration
          config validate         Validate configuration safety
          config set <key> <val>  Set a configuration value
          profile list            List available profiles
          profile use <name>      Switch active profile
          profile validate        Validate active profile

        Entity Indexing:
          index --entities        Index all entities (items, NPCs, monsters, maps)
          index --items           Index items only
          index --npcs            Index NPCs only
          index --monsters        Index monsters only
          index --maps            Index maps only

        Search:
          find item --id <id>     Find item by ID
          find item --name <n>    Find item by name
          find npc --name <n>     Find NPC by name
          find monster --id <id>  Find monster by ID
          find monster --name <n> Find monster by name
          find map --name <n>     Find map by name

        Validation:
          validate                Validate all entities
          validate --canon        Run Global Canon policy checks
          validate --items        Validate items only
          validate --npcs         Validate NPCs only
          validate --monsters     Validate monsters only
          validate --maps         Validate maps only
          triage                  Triage external validation issues (read-only)
          canon check             Run Global Canon policy checks
          knowledge conflicts     Show knowledge conflict report
          knowledge coverage      Show knowledge coverage report
          knowledge ask <text>    Ask local Knowledge Library
          plan create <type>      Create dry-run plan with knowledge context

        Planning:
          dry-run <type> --input <file.json>  Plan changes (no apply)
          dry-run implement --target <path>   Persist a controlled implementation diff
          plan implement --target <path>      Draft an implementation plan without persisting
          cleanup --safe                      Remove regenerable local artifacts only
          diff --last                         View last operation diff
          diff --operation <id>               View operation diff
          report --last --format json|md      Generate report
          report --operation <id> --format json|md
          review code --target <path>         Review file content inside allowed roots
          fix code --target <path>            Generate safe auto-fix diff
          create content --target <path>      Generate scaffolded content diff
          apply implement --operation <id>    Apply a validated implementation diff

        Production governance:
          operations list                     List recorded agent operations
          operations show --operation <id>    Show one recorded operation
          operations compare --left <id> --right <id>
          context pack generate --area <area>
          context pack list
          lessons list
          golden scenarios run
          field test run                    Run safe stack fixtures in an agent sandbox
          eval run                          Run local behavior evals for Setimmo
          observability report              Summarize logs, operations and learning artifacts
          openai review --operation <id>    Prepare optional OpenAI reviewer contract
          production plan --operation <id>    Evaluate formal production readiness
          production approve --operation <id> --approver <name> --reason <text>
          production apply --operation <id>   Apply only after approval, diff hash and rollback checks
          production audit                    List production approvals

        GRF_Extractor integration:
          grf list                            Inventory GRF tooling and containers
          grf inspect --source <file.grf>     Metadata-only read-only inspect
          grf dry-run-extract --source <file> Plan controlled metadata output
          grf extract --operation <id>        Complete controlled metadata output only

        Rollback (informational):
          rollback --list                     List rollback plans
          rollback --id <id> --dry-run        Preview rollback
          rollback --id <id> --confirm        Revert an applied implementation operation

        Options:
          --json    Output in JSON format (default)
          version   Show version
          help      Show this help
        """);
    }
}
