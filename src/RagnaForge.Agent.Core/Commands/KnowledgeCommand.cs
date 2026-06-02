using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Knowledge;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class KnowledgeCommand
{
    private static readonly HashSet<string> AllowedSchemaEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "item",
        "equipment",
        "mob",
        "npc",
        "map",
        "asset"
    };

    private readonly string _configDir;
    private readonly string _agentRoot;
    private readonly string _subCommand;
    private readonly Dictionary<string, string> _params;

    public KnowledgeCommand(string configDir, string agentRoot, string subCommand, Dictionary<string, string> parameters)
    {
        _configDir = configDir;
        _agentRoot = agentRoot;
        _subCommand = subCommand.ToLowerInvariant();
        _params = parameters;
    }

    public JsonOutput Execute()
    {
        try
        {
            var service = new KnowledgeService(_agentRoot);

            switch (_subCommand)
            {
                case "sources":
                    return ExecuteSources(service);
                case "source":
                    return ExecuteSource(service);

                case "build":
                    return ExecuteBuild(service);

                case "search":
                    return ExecuteSearch(service);

                case "explain":
                    return ExecuteExplain(service);
                case "conflicts":
                    return ExecuteConflicts();
                case "coverage":
                    return ExecuteCoverage();
                case "packs":
                    return ExecutePacks(service);
                case "pack":
                    return ExecutePack(service);
                case "freshness":
                    return ExecuteFreshness(service);
                case "refresh":
                    return ExecuteRefresh(service);
                case "snapshots":
                    return ExecuteSnapshots(service);
                case "snapshot":
                    return ExecuteSnapshot(service);
                case "learn":
                    return ExecuteLearn(service);
                case "ask":
                    return ExecuteAsk(service);

                case "entry":
                    return ExecuteEntry(service);

                case "validate":
                    return ExecuteValidate(service);

                case "schema":
                    return ExecuteSchema(service);

                default:
                    return JsonOutput.Error("knowledge", $"Unknown knowledge subcommand: '{_subCommand}'");
            }
        }
        catch (Exception)
        {
            return JsonOutput.Error("knowledge", "Knowledge command failed safely. Review local agent logs for technical details.");
        }
    }

    private JsonOutput ExecuteSources(KnowledgeService service)
    {
        var sources = service.LoadSources();
        var output = JsonOutput.Success("knowledge-sources", $"Loaded {sources.Count} knowledge sources successfully.");
        output.Data = new
        {
            sources,
            assessments = service.BuildSourceAssessments(),
            snapshots = service.LoadSnapshots(),
            learningCandidates = service.LoadLearningCandidates().Count,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteSource(KnowledgeService service)
    {
        var action = _params.GetValueOrDefault("action") ?? "explain";
        if (!action.Equals("explain", StringComparison.OrdinalIgnoreCase))
            return JsonOutput.Error("knowledge-source", $"Unknown knowledge source action: '{action}'");

        var sourceId = _params.GetValueOrDefault("id");
        var validationError = ValidateInput(sourceId, "id", 128);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-source", validationError);

        var source = service.GetSource(sourceId!);
        if (source is null)
            return JsonOutput.Error("knowledge-source", "Knowledge source not found.");

        var assessment = service.BuildSourceAssessments()
            .FirstOrDefault(item => item.SourceId.Equals(source.Id, StringComparison.OrdinalIgnoreCase));
        var latestSnapshot = service.GetLatestSnapshotForSource(source.Id);
        var output = JsonOutput.Success("knowledge-source-explain", $"Knowledge source '{source.Id}' loaded.");
        output.Data = new
        {
            source,
            assessment,
            latestSnapshot,
            authorization = new
            {
                authorizedUse = source.AuthorizedUsePolicy,
                licensePolicy = source.LicensePolicy,
                codeAnalysisAllowed = source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase),
                selectiveIncorporationAllowed = source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase),
                canBlock = false
            },
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteBuild(KnowledgeService service)
    {
        var index = service.BuildIndex();
        var output = JsonOutput.Success("knowledge-build", $"Built index successfully with {index.Entries.Count} entries.");
        output.Data = new { entriesCount = index.Entries.Count, index };
        return output;
    }

    private JsonOutput ExecuteSearch(KnowledgeService service)
    {
        _params.TryGetValue("query", out var queryStr);
        var validationError = ValidateInput(queryStr, "query", 512);
        if (validationError is not null)
        {
            return JsonOutput.Error("knowledge-search", validationError);
        }

        var queryValue = queryStr!;
        var query = new KnowledgeQuery
        {
            Query = queryValue,
            Limit = 10,
            IncludeDetails = true,
            IncludeSources = true
        };

        var results = service.Search(query);
        var output = JsonOutput.Success("knowledge-search", $"Found {results.Count} matching knowledge entries.");
        AddIndexWarning(service, output);
        var context = new KnowledgeContextService(_agentRoot);
        output.Data = new
        {
            matches = results,
            sourceRanking = BuildSourceRanking(service),
            provenance = BuildResultProvenance(results, service.LoadSources()),
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply,
            liveLookupDecision = context.DecideLiveLookup(
                _params.GetValueOrDefault("entityType") ?? "knowledge",
                TryParseInt(_params.GetValueOrDefault("id")),
                queryValue,
                results,
                results.Where(r => r.SourceIds.Any(s => s is "divine-pride" or "ratemyserver")).ToList(),
                ParseKnowledgeOptions())
        };
        return output;
    }

    private JsonOutput ExecuteExplain(KnowledgeService service)
    {
        if (_params.TryGetValue("source", out var sourceId) && !string.IsNullOrWhiteSpace(sourceId))
        {
            var source = service.LoadSources().FirstOrDefault(s => s.Id.Equals(sourceId, StringComparison.OrdinalIgnoreCase));
            if (source is null)
                return JsonOutput.Error("knowledge-explain", "Knowledge source not found.");

            var outputBySource = JsonOutput.Success("knowledge-explain", $"Source explanation: '{source.Id}'");
            outputBySource.Data = new
            {
                source,
                provenance = BuildSourceProvenance(source),
                priority = KnowledgeTrustPolicy.PriorityForSource(source.Id, source.SourceType),
                canBlock = KnowledgeTrustPolicy.CanBlockAlone(source.Id, source.SourceType),
                reasonNotBlocking = source.Id is "divine-pride" or "ratemyserver"
                    ? "Reference source only; local data has priority."
                    : "Internal source supports local validation and explanation.",
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            };
            return outputBySource;
        }

        if (_params.TryGetValue("id", out var entryId) && !string.IsNullOrWhiteSpace(entryId))
        {
            var entry = service.GetEntry(entryId);
            if (entry is null)
                return JsonOutput.Error("knowledge-explain", "Knowledge entry not found.");

            var outputById = JsonOutput.Success("knowledge-explain", $"Entry explanation: '{entry.Id}'");
            outputById.Data = new
            {
                entry,
                provenance = BuildEntryProvenance(entry, service.LoadSources()),
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            };
            return outputById;
        }

        _params.TryGetValue("topic", out var topicStr);
        var validationError = ValidateInput(topicStr, "topic", 512);
        if (validationError is not null)
        {
            return JsonOutput.Error("knowledge-explain", validationError);
        }

        var topicValue = topicStr!;
        var results = service.Explain(topicValue);
        var output = JsonOutput.Success("knowledge-explain", $"Explanation details for topic: '{topicValue}'");
        AddIndexWarning(service, output);
        output.Data = new
        {
            topic = topicValue,
            results,
            provenance = BuildResultProvenance(results, service.LoadSources()),
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteConflicts()
    {
        var entityType = _params.GetValueOrDefault("entityType");
        var conflicts = new KnowledgeContextService(_agentRoot).BuildConflictsReport(entityType);
        var output = JsonOutput.Success("knowledge-conflicts", $"Knowledge conflict report returned {conflicts.Count} finding(s).");
        output.Data = new
        {
            totalConflicts = conflicts.Count,
            conflicts,
            bySeverity = conflicts.GroupBy(c => c.Severity).ToDictionary(g => g.Key, g => g.Count()),
            byEntityType = conflicts.GroupBy(c => c.EntityType).ToDictionary(g => g.Key, g => g.Count()),
            safeForReadOnlyWork = true,
            safeForDryRun = !conflicts.Any(c => c.BlocksDryRun),
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteCoverage()
    {
        var coverage = new KnowledgeContextService(_agentRoot).BuildCoverage();
        var output = JsonOutput.Success("knowledge-coverage", "Knowledge coverage report is ready.");
        output.Data = new
        {
            coverage,
            liveLookupDecision = new { skipped = true, reason = "Coverage is broad analysis; live lookup is skipped by anti-bulk policy." },
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecutePacks(KnowledgeService service)
    {
        var packs = service.BuildPackAssessments();
        var output = JsonOutput.Success("knowledge-packs", $"Loaded {packs.Count} knowledge pack(s).");
        output.Data = new
        {
            packs,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecutePack(KnowledgeService service)
    {
        var action = _params.GetValueOrDefault("action") ?? "explain";
        var packId = _params.GetValueOrDefault("id");
        var validationError = ValidateInput(packId, "id", 128);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-pack", validationError);

        var pack = service.GetPack(packId!);
        if (pack is null)
            return JsonOutput.Error("knowledge-pack", "Knowledge pack not found.");

        var assessment = service.BuildPackAssessments()
            .FirstOrDefault(p => p.PackId.Equals(pack.Id, StringComparison.OrdinalIgnoreCase));

        if (action.Equals("validate", StringComparison.OrdinalIgnoreCase))
        {
            var ok = assessment is not null && assessment.Errors.Count == 0;
            var outputValidate = new JsonOutput
            {
                Ok = ok,
                Mode = "knowledge-pack-validate",
                Summary = ok ? $"Pack '{pack.Id}' validation passed." : $"Pack '{pack.Id}' validation found issues.",
                SafeForAutomation = ok,
                Data = new
                {
                    pack,
                    assessment,
                    safeForReadOnlyWork = true,
                    safeForDryRun = true,
                    safeForApply = BuildKnowledgeGovernance().SafeForApply
                }
            };
            if (!ok)
                outputValidate.Errors.AddRange(assessment?.Errors ?? []);
            outputValidate.Warnings.AddRange(assessment?.Warnings ?? []);
            return outputValidate;
        }

        var output = JsonOutput.Success("knowledge-pack-explain", $"Knowledge pack '{pack.Id}' loaded.");
        output.Data = new
        {
            pack,
            assessment,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteFreshness(KnowledgeService service)
    {
        var output = JsonOutput.Success("knowledge-freshness", "Knowledge freshness report is ready.");
        output.Data = service.BuildFreshnessReport();
        return output;
    }

    private JsonOutput ExecuteRefresh(KnowledgeService service)
    {
        var action = _params.GetValueOrDefault("action") ?? "plan";
        var refreshService = new OnlineKnowledgeRefreshService(_agentRoot, service);
        return action.ToLowerInvariant() switch
        {
            "plan" => SuccessWithData("knowledge-refresh-plan", "Knowledge refresh plan generated.", new
            {
                items = refreshService.BuildRefreshPlan(),
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            }),
            "due" => SuccessWithData("knowledge-refresh-due", "Knowledge refresh due list generated.", new
            {
                items = refreshService.BuildDuePlan(),
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            }),
            "report" => SuccessWithData("knowledge-refresh-report", "Knowledge refresh markdown report generated.", new
            {
                format = "md",
                markdown = refreshService.BuildMarkdownReport(),
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            }),
            "run" => ExecuteRefreshRun(refreshService),
            _ => JsonOutput.Error("knowledge-refresh", $"Unknown knowledge refresh action: '{action}'")
        };
    }

    private JsonOutput ExecuteRefreshRun(OnlineKnowledgeRefreshService refreshService)
    {
        var sourceId = _params.GetValueOrDefault("source");
        var runAll = _params.ContainsKey("all");
        if (!runAll)
        {
            var validationError = ValidateInput(sourceId, "source", 128);
            if (validationError is not null)
                return JsonOutput.Error("knowledge-refresh", validationError);
        }

        var mode = _params.GetValueOrDefault("mode") ?? "metadata";
        var results = refreshService.Run(sourceId, runAll, mode);
        var output = JsonOutput.Success("knowledge-refresh-run", $"Knowledge refresh processed {results.Count} source(s).");
        output.Data = new
        {
            mode,
            results,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteSnapshots(KnowledgeService service)
    {
        var snapshots = service.LoadSnapshots();
        var output = JsonOutput.Success("knowledge-snapshots", $"Loaded {snapshots.Count} knowledge snapshot(s).");
        output.Data = new
        {
            snapshots,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteSnapshot(KnowledgeService service)
    {
        var action = _params.GetValueOrDefault("action") ?? "explain";
        return action.ToLowerInvariant() switch
        {
            "explain" => ExecuteSnapshotExplain(service),
            "diff" => ExecuteSnapshotDiff(service),
            _ => JsonOutput.Error("knowledge-snapshot", $"Unknown knowledge snapshot action: '{action}'")
        };
    }

    private JsonOutput ExecuteSnapshotExplain(KnowledgeService service)
    {
        var id = _params.GetValueOrDefault("id");
        var validationError = ValidateInput(id, "id", 128);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-snapshot", validationError);

        var snapshot = service.GetSnapshot(id!);
        if (snapshot is null)
            return JsonOutput.Error("knowledge-snapshot", "Knowledge snapshot not found.");

        var output = JsonOutput.Success("knowledge-snapshot-explain", $"Knowledge snapshot '{snapshot.Id}' loaded.");
        output.Data = new
        {
            snapshot,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteSnapshotDiff(KnowledgeService service)
    {
        var sourceId = _params.GetValueOrDefault("source");
        var validationError = ValidateInput(sourceId, "source", 128);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-snapshot", validationError);

        var snapshots = service.LoadSnapshots()
            .Where(snapshot => snapshot.SourceId.Equals(sourceId!, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.RetrievedAt, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        var latest = snapshots.FirstOrDefault();
        var previous = snapshots.Skip(1).FirstOrDefault();
        var changes = new List<string>();
        if (latest is not null && previous is not null)
        {
            if (!string.Equals(latest.SourceVersion, previous.SourceVersion, StringComparison.OrdinalIgnoreCase))
                changes.Add($"sourceVersion: {previous.SourceVersion} -> {latest.SourceVersion}");
            if (!string.Equals(latest.ContentHash, previous.ContentHash, StringComparison.OrdinalIgnoreCase))
                changes.Add("contentHash changed");
            if (!string.Equals(latest.MetadataHash, previous.MetadataHash, StringComparison.OrdinalIgnoreCase))
                changes.Add("metadataHash changed");
        }

        var output = JsonOutput.Success("knowledge-snapshot-diff", $"Knowledge snapshot diff generated for '{sourceId}'.");
        output.Data = new
        {
            sourceId,
            latestSnapshot = latest,
            previousSnapshot = previous,
            changes,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteLearn(KnowledgeService service)
    {
        var action = _params.GetValueOrDefault("action") ?? "candidates";
        var learningService = new LearningService(service);
        return action.ToLowerInvariant() switch
        {
            "observe" => ExecuteLearnObserve(learningService),
            "candidates" => SuccessWithData("knowledge-learn-candidates", "Learning candidates loaded.", new
            {
                candidates = service.LoadLearningCandidates(),
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            }),
            "explain" => ExecuteLearnExplain(service),
            "approve" => ExecuteLearnApprove(learningService),
            "reject" => ExecuteLearnReject(learningService),
            "promote" => ExecuteLearnPromote(learningService),
            "report" => SuccessWithData("knowledge-learn-report", "Learning markdown report generated.", new
            {
                format = "md",
                markdown = learningService.BuildMarkdownReport(),
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            }),
            _ => JsonOutput.Error("knowledge-learn", $"Unknown learning action: '{action}'")
        };
    }

    private JsonOutput ExecuteLearnObserve(LearningService learningService)
    {
        var sourceId = _params.GetValueOrDefault("source");
        var topic = _params.GetValueOrDefault("topic");
        var summary = _params.GetValueOrDefault("summary") ?? _params.GetValueOrDefault("query");
        var sourceValidation = ValidateInput(sourceId, "source", 128);
        if (sourceValidation is not null)
            return JsonOutput.Error("knowledge-learn", sourceValidation);
        var topicValidation = ValidateInput(topic, "topic", 256);
        if (topicValidation is not null)
            return JsonOutput.Error("knowledge-learn", topicValidation);
        var summaryValidation = ValidateInput(summary, "summary", 512);
        if (summaryValidation is not null)
            return JsonOutput.Error("knowledge-learn", summaryValidation);

        var candidate = learningService.Observe(sourceId!, topic!, summary!);
        var output = JsonOutput.Success("knowledge-learn-observe", "Learning observation generated in dry-run mode.");
        output.Data = new
        {
            candidate,
            persisted = false,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteLearnExplain(KnowledgeService service)
    {
        var id = _params.GetValueOrDefault("id");
        var validationError = ValidateInput(id, "id", 128);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-learn", validationError);

        var candidate = service.GetLearningCandidate(id!);
        if (candidate is null)
            return JsonOutput.Error("knowledge-learn", "Learning candidate not found.");

        var output = JsonOutput.Success("knowledge-learn-explain", $"Learning candidate '{candidate.Id}' loaded.");
        output.Data = new
        {
            candidate,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteLearnApprove(LearningService learningService)
    {
        var id = _params.GetValueOrDefault("id");
        var validationError = ValidateInput(id, "id", 128);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-learn", validationError);

        var decision = learningService.Approve(id!, _params.ContainsKey("dryRun"));
        var output = JsonOutput.Success("knowledge-learn-approve", "Learning approve decision generated.");
        output.Data = new
        {
            decision,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteLearnReject(LearningService learningService)
    {
        var id = _params.GetValueOrDefault("id");
        var reason = _params.GetValueOrDefault("reason");
        var idValidation = ValidateInput(id, "id", 128);
        if (idValidation is not null)
            return JsonOutput.Error("knowledge-learn", idValidation);
        var reasonValidation = ValidateInput(reason, "reason", 256);
        if (reasonValidation is not null)
            return JsonOutput.Error("knowledge-learn", reasonValidation);

        var decision = learningService.Reject(id!, reason!);
        var output = JsonOutput.Success("knowledge-learn-reject", "Learning reject decision generated.");
        output.Data = new
        {
            decision,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteLearnPromote(LearningService learningService)
    {
        var id = _params.GetValueOrDefault("id");
        var validationError = ValidateInput(id, "id", 128);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-learn", validationError);

        var decision = learningService.Promote(id!, _params.ContainsKey("dryRun"));
        var output = JsonOutput.Success("knowledge-learn-promote", "Learning promote dry-run plan generated.");
        output.Data = new
        {
            decision,
            safeForReadOnlyWork = true,
            safeForDryRun = true,
            safeForApply = BuildKnowledgeGovernance().SafeForApply
        };
        return output;
    }

    private JsonOutput ExecuteAsk(KnowledgeService service)
    {
        _params.TryGetValue("query", out var question);
        var validationError = ValidateInput(question, "query", 512);
        if (validationError is not null)
            return JsonOutput.Error("knowledge-ask", validationError);

        var answer = new KnowledgeContextService(_agentRoot).BuildAskAnswer(question!, ParseKnowledgeOptions());
        var output = JsonOutput.Success("knowledge-ask", "Local knowledge answer generated.");
        AddIndexWarning(service, output);
        output.Data = answer;
        return output;
    }

    private JsonOutput ExecuteEntry(KnowledgeService service)
    {
        _params.TryGetValue("id", out var idStr);
        var validationError = ValidateInput(idStr, "id", 128);
        if (validationError is not null)
        {
            return JsonOutput.Error("knowledge-entry", validationError);
        }

        var idValue = idStr!;
        var entry = service.GetEntry(idValue);
        if (entry == null)
        {
            return JsonOutput.Error("knowledge-entry", "Knowledge entry not found.");
        }

        var output = JsonOutput.Success("knowledge-entry", $"Loaded entry: '{entry.Title}'");
        AddIndexWarning(service, output);
        output.Data = new { entry };
        return output;
    }

    private JsonOutput ExecuteValidate(KnowledgeService service)
    {
        var issues = service.ValidatePacks();
        issues.AddRange(service.ValidateSources());
        var assessments = service.BuildPackAssessments();
        var sourceAssessments = service.BuildSourceAssessments();
        var ok = issues.Count == 0;

        var output = new JsonOutput
        {
            Ok = ok,
            Mode = "knowledge-validate",
            Summary = ok ? "Knowledge validation passed. All packs are clean." : $"Knowledge validation failed with {issues.Count} issue(s).",
            SafeForAutomation = ok,
            Errors = issues,
            Data = new
            {
                totalIssues = issues.Count,
                bySeverity = new
                {
                    errors = assessments.Sum(a => a.Errors.Count) + sourceAssessments.Sum(a => a.Errors.Count),
                    warnings = assessments.Sum(a => a.Warnings.Count) + sourceAssessments.Sum(a => a.Warnings.Count)
                },
                packs = assessments,
                sources = sourceAssessments,
                snapshots = service.LoadSnapshots(),
                learningCandidates = service.LoadLearningCandidates(),
                safeForReadOnlyWork = true,
                safeForDryRun = true,
                safeForApply = BuildKnowledgeGovernance().SafeForApply
            }
        };

        return output;
    }

    private JsonOutput ExecuteSchema(KnowledgeService service)
    {
        _params.TryGetValue("entity", out var entityStr);
        var validationError = ValidateInput(entityStr, "entity", 32);
        if (validationError is not null)
        {
            return JsonOutput.Error("knowledge-schema", validationError);
        }

        var entityType = entityStr!.ToLowerInvariant();
        if (!AllowedSchemaEntities.Contains(entityType))
        {
            return JsonOutput.Error("knowledge-schema", "Unsupported entity type for schema. Supported: item, equipment, mob, npc, map, asset.");
        }

        object? schema = null;

        switch (entityType)
        {
            case "item":
                schema = new
                {
                    entityType = "item",
                    primaryFields = new[] { "Id", "AegisName" },
                    databaseFields = new[] { "Id (int)", "AegisName (string)", "Type (string)", "Weight (int)", "Buy (int)", "Sell (int)", "Slots (int)", "Script (string)" },
                    clientPairing = "System/itemInfo.lua (or .lub) mapped by Id. unidentifed/identified display and resource names.",
                    validationRules = new[] { "AegisName and Id must be globally unique.", "Duplicate Id within server is critical error, duplicates on client-side entries yield warnings." }
                };
                break;

            case "equipment":
                schema = new
                {
                    entityType = "equipment",
                    primaryFields = new[] { "Id", "AegisName", "View" },
                    databaseFields = new[] { "View (int)", "EquipLocations (string)", "JobRestrictions (string)", "WeaponType (string)" },
                    clientPairing = "Weapon/Headgear View maps directly to visual folder paths in data/sprite. Shields use hardcoded built-in View ranges.",
                    validationRules = new[] { "Shield views outside default built-in indices are blocked.", "Duplicate headgear or weapon View IDs are flagged." }
                };
                break;

            case "mob":
                schema = new
                {
                    entityType = "mob",
                    primaryFields = new[] { "Id", "AegisName" },
                    databaseFields = new[] { "Id (int)", "AegisName (string)", "Name (string)", "Hp (int)", "Drops (array)", "Skills (array)", "Spawns (array)" },
                    clientPairing = "Loads custom monster sprites based on AegisName from data/sprite/monster/AegisName.spr/.act.",
                    validationRules = new[] { "Duplicate Mob IDs on server are forbidden.", "Drops must reference existing server-side Item IDs." }
                };
                break;

            case "npc":
                schema = new
                {
                    entityType = "npc",
                    primaryFields = new[] { "DisplayName", "SpriteID" },
                    databaseFields = new[] { "MapName (string)", "X (int)", "Y (int)", "Direction (int)", "Type (script|shop|warp)", "DisplayName (string)", "SpriteID (int)" },
                    clientPairing = "SpriteID maps to client sprite identifiers in NPC identity lua tables. Hashtag suffixes hide duplicate names.",
                    validationRules = new[] { "Duplicate NPC names without hashtag suffixes (e.g. John#01) cause script override warning/errors." }
                };
                break;

            case "map":
                schema = new
                {
                    entityType = "map",
                    primaryFields = new[] { "MapName" },
                    databaseFields = new[] { "MapName (string, max 12 chars)", "NumericalIndex (int)" },
                    clientPairing = "Requires the core map trio (.rsw, .gnd, .gat) in client GRF containers or loose data/ directory.",
                    validationRules = new[] { "Server registration in maps_athena.conf and map_index.txt must match client trio.", "Missing .gat is a critical passability crash blocker." }
                };
                break;

            case "asset":
                schema = new
                {
                    entityType = "asset",
                    primaryFields = new[] { "FilePath" },
                    databaseFields = new[] { "FilePath (string)", "Encoding (EUC-KR)", "Container (GRF/loose)" },
                    clientPairing = "Sprites require paired .spr (pixels/palette) and .act (anchors/animations) under the same directory.",
                    validationRules = new[] { "Loose .spr/.act missing counterpart is flagged as a broken asset glitch.", "Path casing is normalized case-insensitive." }
                };
                break;

            default:
                return JsonOutput.Error("knowledge-schema", "Unsupported entity type for schema. Supported: item, equipment, mob, npc, map, asset.");
        }

        var output = JsonOutput.Success("knowledge-schema", $"Schema reference loaded for: '{entityType}'");
        output.Data = new { entityType, schema };
        return output;
    }

    private static void AddIndexWarning(KnowledgeService service, JsonOutput output)
    {
        if (!string.IsNullOrWhiteSpace(service.LastReadOnlyIndexWarning))
        {
            output.Warnings.Add(service.LastReadOnlyIndexWarning);
        }
    }

    private static string? ValidateInput(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return $"Missing required argument: --{name}";

        if (value.Length > maxLength)
            return $"Invalid {name}: maximum length is {maxLength} characters.";

        if (value.Any(char.IsControl))
            return $"Invalid {name}: control characters are blocked.";

        if (LooksPathLike(value))
            return $"Invalid {name}: path-like input is blocked for this command.";

        return null;
    }

    private static bool LooksPathLike(string value) =>
        value.Contains("..", StringComparison.Ordinal) ||
        value.Contains(':', StringComparison.Ordinal) ||
        value.Contains('\\', StringComparison.Ordinal) ||
        value.Contains('/', StringComparison.Ordinal);

    private KnowledgeLookupOptions ParseKnowledgeOptions() => new()
    {
        WithKnowledge = true,
        KnowledgeLocalOnly = _params.ContainsKey("knowledgeLocalOnly"),
        NoLiveReference = _params.ContainsKey("noLiveReference"),
        LiveSource = _params.GetValueOrDefault("liveSource") ?? "auto",
        LiveTimeoutMs = TryParseInt(_params.GetValueOrDefault("liveTimeoutMs")) ?? 3000,
        MaxLiveRequestsPerSource = TryParseInt(_params.GetValueOrDefault("maxLiveRequests")) ?? 1,
        MaxTotalLiveRequests = TryParseInt(_params.GetValueOrDefault("maxTotalLiveRequests")) ?? 2,
        AllowSanitizedMetadataCache = _params.ContainsKey("allowSanitizedMetadataCache")
    };

    private OperationGovernanceAssessment BuildKnowledgeGovernance() =>
        OperationGovernanceProfiles.EvaluateWithoutValidation(
            $"knowledge-{_subCommand}",
            applyEngineImplemented: true,
            rollbackEngineImplemented: true);

    private static int? TryParseInt(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static object BuildSourceRanking(KnowledgeService service) =>
        service.LoadSources()
            .Select(s => new
            {
                sourceId = s.Id,
                sourceName = s.Name,
                sourceKind = s.SourceType,
                trustLevel = s.TrustLevel,
                priority = KnowledgeTrustPolicy.PriorityForSource(s.Id, s.SourceType),
                localOrExternal = s.Id is "divine-pride" or "ratemyserver" ? "external-reference-registered-locally" : "internal",
                reason = s.AllowedUse,
                whetherItCanBlock = KnowledgeTrustPolicy.CanBlockAlone(s.Id, s.SourceType),
                updateMode = s.UpdateMode,
                supportedTopics = s.SupportedTopics
            })
            .OrderByDescending(s => s.priority)
            .ToList();

    private static List<object> BuildResultProvenance(IEnumerable<KnowledgeResult> results, List<KnowledgeSource> sources)
    {
        var byId = sources.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        return results.SelectMany(r => r.SourceIds).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id =>
            {
                byId.TryGetValue(id, out var source);
                return BuildSourceProvenance(source ?? new KnowledgeSource { Id = id, Name = id, SourceType = "unknown" });
            })
            .ToList();
    }

    private static List<object> BuildEntryProvenance(KnowledgeEntry entry, List<KnowledgeSource> sources)
    {
        var byId = sources.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        return entry.SourceIds
            .Select(id =>
            {
                byId.TryGetValue(id, out var source);
                return BuildSourceProvenance(source ?? new KnowledgeSource { Id = id, Name = id, SourceType = "unknown" });
            })
            .ToList();
    }

    private static object BuildSourceProvenance(KnowledgeSource source) => new
    {
        sourceId = source.Id,
        sourceName = source.Name,
        sourceKind = source.SourceType,
        origin = source.Provenance ?? (source.Id is "divine-pride" or "ratemyserver" ? "external-reference-registered-locally" : "internal"),
        externalReferenceUrl = source.ExternalReferenceUrl ?? source.Url,
        packVersion = source.SchemaVersion,
        reviewedAt = source.LastReviewedUtc,
        confidence = source.TrustLevel,
        priority = KnowledgeTrustPolicy.PriorityForSource(source.Id, source.SourceType),
        trustPolicy = string.IsNullOrWhiteSpace(source.TrustPolicy) ? source.AllowedUse : source.TrustPolicy,
        conflictPolicy = string.IsNullOrWhiteSpace(source.ConflictPolicy)
            ? (source.Id is "divine-pride" or "ratemyserver"
                ? "Reference source only; local data has priority."
                : "Internal source supports local validation and explanation.")
            : source.ConflictPolicy,
        canBlock = KnowledgeTrustPolicy.CanBlockAlone(source.Id, source.SourceType),
        reasonNotBlocking = source.Id is "divine-pride" or "ratemyserver"
            ? "Reference source only; local data has priority."
            : "Can support blocking only when local evidence confirms the issue."
    };

    private static JsonOutput SuccessWithData(string mode, string summary, object data)
    {
        var output = JsonOutput.Success(mode, summary);
        output.Data = data;
        return output;
    }
}
