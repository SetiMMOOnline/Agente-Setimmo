using RagnaForge.Agent.Core.Api;
using RagnaForge.Agent.Core.Canon;
using RagnaForge.Agent.Core.Governance;
using RagnaForge.Agent.Core.Output;

namespace RagnaForge.Agent.Core.Commands;

public sealed class ApiReadinessExportCommand
{
    private readonly string _agentRoot;

    public ApiReadinessExportCommand(string agentRoot)
    {
        _agentRoot = agentRoot;
    }

    public JsonOutput Execute()
    {
        var output = JsonOutput.Success("export-api-readiness", "Agent API readiness export generated.");
        output.Data = BuildContract();
        return output;
    }

    private AgentApiContract BuildContract()
    {
        var canon = new GlobalCanonValidator(_agentRoot).Check();
        var governance = OperationGovernanceProfiles.EvaluateValidated(
            "api-readiness-export",
            canon,
            new ValidationDecisionSummary
            {
                SafeForReadOnlyWork = true,
                SafeForDryRun = true,
                SafeForApply = true
            },
            applyEngineImplemented: true,
            rollbackEngineImplemented: true,
            productionApplyEnabled: false);
        return new AgentApiContract
        {
            Capabilities = new AgentCapabilityContract
            {
                SupportsApply = true,
                SupportsRollback = true,
                SupportsDryRun = true,
                SupportsProductionApply = true,
                SupportsCodexSupervised = true,
                SupportsSemanticPatch = true,
                SupportsContextPacks = true,
                SupportsOperationHistory = true,
                SupportsGrfOperations = true
            },
            SupportedCommands =
            [
                "review code --target <path>",
                "fix code --target <path>",
                "create content --target <path>",
                "plan implement --target <path>",
                "dry-run implement --target <path>",
                "apply implement --operation <id> --confirm",
                "rollback --id <id> --confirm",
                "operations list|show|compare",
                "production plan|review|approve|status|audit",
                "production apply --operation <id> --confirm",
                "production rollback --operation <id> --confirm",
                "grf list|inspect|dry-run-extract|extract|cleanup",
                "find item|equipment|monster|npc|map|skill|quest --with-knowledge",
                "knowledge search",
                "knowledge explain",
                "knowledge conflicts",
                "knowledge coverage",
                "knowledge packs",
                "knowledge pack explain",
                "knowledge pack validate",
                "knowledge freshness",
                "knowledge sources",
                "knowledge source explain",
                "knowledge refresh plan|due|run|report",
                "knowledge snapshots",
                "knowledge snapshot explain|diff",
                "knowledge learn observe|candidates|explain|approve|reject|promote|report",
                "triage --external-data",
                "plan create item|equipment|monster|npc|map|skill|quest",
                "report --knowledge",
                "report --external-data",
                "report --entity-plan",
                "report --readiness-summary",
                "export api-readiness",
                "canon check"
            ],
            SupportedEntityTypes = ["item", "equipment", "monster", "npc", "map", "skill", "quest"],
            SupportedKnowledgeFeatures =
            [
                "stable-json-contracts",
                "knowledge-hints",
                "conflicts",
                "coverage",
                "pack-freshness",
                "provenance",
                "source-ranking",
                "custom-project-policy",
                "controlled-live-reference-unavailable-by-policy",
                "knowledge-sources",
                "source-snapshots",
                "refresh-planning",
                "learning-candidates",
                "authorized-code-reference-notes"
            ],
            SupportedPlanFeatures =
            [
                "id-suggestions",
                "safe-id-ranges",
                "dependency-hints",
                "episode-gate",
                "risk classification",
                "human-review-required",
                "diff-preview-placeholder",
                "operation-governance-preview",
                "safe-for-production-apply",
                "implementation-review",
                "implementation-fix",
                "implementation-create-content",
                "implementation-rollback",
                "formal-production-approval",
                "diff-hash-bound-approval",
                "operation-history-observability",
                "grf-extractor-metadata-integration"
            ],
            SupportedReportFeatures =
            [
                "knowledge-markdown",
                "external-data-markdown",
                "entity-plan-markdown",
                "readiness-summary-markdown"
            ],
            SupportedLearningFeatures =
            [
                "observe-dry-run",
                "candidate-review",
                "approve-dry-run",
                "reject-read-only",
                "promote-dry-run",
                "provenance-required",
                "no-secrets",
                "no-raw-html"
            ],
            SupportedRefreshFeatures =
            [
                "refresh-plan",
                "refresh-due",
                "metadata-only-refresh",
                "forum-skipped-by-policy",
                "github-metadata",
                "authorized-code-reference",
                "sanitized-snapshots"
            ],
            SupportedOnlineSources =
            [
                "rathena-board",
                "robrowserlegacy",
                "robrowserlegacy-remoteclient-js",
                "divine-pride",
                "ratemyserver"
            ],
            SupportedInternalLibraries =
            [
                "global-canon",
                "rathena",
                "rathena-user-guides",
                "rathena-board",
                "robrowserlegacy",
                "robrowserlegacy-remoteclient-js",
                "divine-pride",
                "ratemyserver"
            ],
            SupportedAuthorizedCodeSources =
            [
                "robrowserlegacy",
                "robrowserlegacy-remoteclient-js"
            ],
            SupportedMcpTools =
            [
                "ragnaforge_knowledge_sources",
                "ragnaforge_knowledge_source_explain",
                "ragnaforge_knowledge_source_freshness",
                "ragnaforge_knowledge_refresh_plan",
                "ragnaforge_knowledge_snapshots",
                "ragnaforge_learning_candidates",
                "ragnaforge_learning_report",
                "ragnaforge_authorized_source_notes",
                "ragnaforge_knowledge_search",
                "ragnaforge_knowledge_explain",
                "ragnaforge_knowledge_entry",
                "ragnaforge_knowledge_schema",
                "ragnaforge_knowledge_validate",
                "ragnaforge_knowledge_conflicts",
                "ragnaforge_knowledge_coverage",
                "ragnaforge_external_data_triage",
                "ragnaforge_pack_freshness",
                "ragnaforge_plan_create_entity",
                "ragnaforge_review_code",
                "ragnaforge_fix_code",
                "ragnaforge_create_content",
                "ragnaforge_plan_implement",
                "ragnaforge_dry_run_implement",
                "ragnaforge_apply_implement",
                "ragnaforge_rollback_implement",
                "ragnaforge_cleanup_safe",
                "ragnaforge_operations_list",
                "ragnaforge_operations_show",
                "ragnaforge_operations_compare",
                "ragnaforge_production_status",
                "ragnaforge_production_audit",
                "ragnaforge_production_approve",
                "ragnaforge_production_apply",
                "ragnaforge_production_rollback",
                "ragnaforge_grf_list",
                "ragnaforge_grf_inspect",
                "ragnaforge_grf_dry_run_extract",
                "ragnaforge_grf_extract",
                "ragnaforge_generate_knowledge_report",
                "ragnaforge_api_readiness_export",
                "ragnaforge_canon_check"
            ],
            JsonSchemas = BuildSchemas(),
            SafetyFlags = new AgentValidationSummaryContract
            {
                CanApply = false,
                SafeForReadOnlyWork = governance.SafeForReadOnlyWork,
                SafeForDryRun = governance.SafeForDryRun,
                SafeForApply = false,
                SafeForProductionApply = governance.SafeForProductionApply,
                ReadOnlyMode = false,
                ApplyEnabled = false,
                RollbackEnabled = false
            },
            RecommendedApiEndpoints =
            [
                "GET /api/agent/capabilities",
                "GET /api/agent/api-readiness",
                "GET /api/agent/knowledge/summary",
                "POST /api/agent/entity/find",
                "POST /api/agent/entity/plan",
                "POST /api/agent/implementation/review",
                "POST /api/agent/implementation/fix",
                "POST /api/agent/implementation/create",
                "POST /api/agent/implementation/dry-run",
                "POST /api/agent/implementation/apply"
            ],
            RecommendedUiTabs =
            [
                "Dashboard",
                "Itens",
                "Equipamentos",
                "Monstros",
                "NPCs",
                "Mapas",
                "Assets / GRF Preview",
                "Progressao / Episodios",
                "Knowledge / Conflitos",
                "Pipeline / Dry-run / Diff-preview",
                "Relatorios",
                "Agent Health"
            ],
            RecommendedUiPanels =
            [
                "Knowledge Sources",
                "Source Freshness",
                "Learning Candidates",
                "Refresh Plan",
                "Online Lookup Policy",
                "Authorized Code References",
                "License / Permission Notes",
                "Source Snapshots",
                "Entity Hints",
                "Plan Create",
                "Reports"
            ]
        };
    }

    private static Dictionary<string, object> BuildSchemas()
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["AgentApiContract"] = Schema(["capabilities", "supportedCommands", "supportedEntityTypes", "jsonSchemas", "safetyFlags", "recommendedApiEndpoints", "recommendedUiTabs"]),
            ["AgentEntityLookupContract"] = Schema(["entityType", "query", "localEntity", "hints", "conflicts", "risk", "provenance", "externalReference", "episodeGate", "nextSafeActions", "safeForReadOnlyWork", "safeForDryRun", "safeForApply", "safeForProductionApply"]),
            ["AgentEntityPlanContract"] = Schema(["requestedEntity", "normalizedEntity", "requiredFields", "missingRequiredFields", "idConflictCheck", "idSuggestions", "safeIdRanges", "localMatches", "knowledgeMatches", "referenceContext", "controlledLiveReference", "assetHints", "dependencyHints", "episodeGate", "riskLevel", "conflicts", "warnings", "nextSafeActions", "dryRunPlan", "diffPreviewPlaceholder", "humanReviewRequired", "canApply", "safeForReadOnlyWork", "safeForDryRun", "safeForApply", "safeForProductionApply"]),
            ["AgentKnowledgeHintContract"] = Schema(["id", "severity", "category", "message", "explanation", "provenance", "humanReviewRecommended", "blocksReadOnly", "blocksDryRun", "blocksApply", "reasonNotBlocking"]),
            ["AgentConflictContract"] = Schema(["entityType", "entityId", "severity", "riskLevel", "explanation", "humanReviewRecommended", "blocksReadOnly", "blocksDryRun", "blocksApply", "reasonNotBlocking", "nextSafeAction"]),
            ["AgentCoverageContract"] = Schema(["entityType", "localCount", "internalReferenceCount", "withHints", "conflicts", "coverageRatio"]),
            ["AgentRiskContract"] = Schema(["level", "reason"]),
            ["AgentProvenanceContract"] = Schema(["sourceId", "sourceName", "sourceKind", "origin", "externalReferenceUrl", "reviewedAt", "retrievedAt", "confidence", "priority", "trustPolicy", "conflictPolicy", "canBlock", "reasonNotBlocking"]),
            ["AgentExternalReferenceContract"] = Schema(["liveLookupDecision", "decisionReason", "source", "requestCount", "timeoutMs", "rateLimitApplied", "linksFollowed", "bulkLookup", "rangeLookup", "rawHtmlStored", "dumpStored", "cacheMode", "warning", "reasonNotBlocking"]),
            ["AgentEpisodeGateContract"] = Schema(["status", "reason"]),
            ["AgentDependencyContract"] = Schema(["name", "status", "reason"]),
            ["AgentReportContract"] = Schema(["reportType", "format", "markdown", "warnings", "safeForReadOnlyWork", "safeForDryRun", "safeForApply", "safeForProductionApply"]),
            ["AgentValidationSummaryContract"] = Schema(["canApply", "safeForReadOnlyWork", "safeForDryRun", "safeForApply", "safeForProductionApply", "readOnlyMode", "applyEnabled", "rollbackEnabled"]),
            ["AgentCapabilityContract"] = Schema(["supportsApply", "supportsRollback", "supportsDryRun", "supportsProductionApply", "supportsCodexSupervised", "supportsSemanticPatch", "supportsContextPacks", "supportsOperationHistory", "supportsGrfOperations"]),
            ["AgentKnowledgeSourceContract"] = Schema(["sourceId", "name", "sourceType", "supportedTopics", "supportedEntityTypes", "updateMode", "refreshPolicy", "licenseNotes", "authorizedUse", "canBlock"]),
            ["AgentKnowledgeRefreshContract"] = Schema(["sourceId", "mode", "status", "requestCount", "timeoutMs", "rateLimitApplied", "linksFollowed", "paginationUsed", "bulkLookup", "rangeLookup", "rawHtmlStored", "dumpStored", "cacheMode", "updateDetected"]),
            ["AgentLearningCandidateContract"] = Schema(["id", "sourceId", "topic", "summary", "status", "humanReviewRequired", "licenseNotes", "authorizedUse", "safeForApply"]),
            ["AgentSourceSnapshotContract"] = Schema(["id", "sourceId", "sourceVersion", "retrievedAt", "sanitized", "rawStored", "updateDetected", "summary", "warnings"]),
            ["AgentLicenseNoteContract"] = Schema(["sourceId", "license", "note"]),
            ["AgentAuthorizedUseContract"] = Schema(["authorized", "authorizationScope", "codeAnalysisAllowed", "selectiveIncorporationAllowed", "requiresHumanReview"])
        };
    }

    private static object Schema(IEnumerable<string> requiredFields) => new
    {
        type = "object",
        required = requiredFields.ToArray(),
        additionalProperties = true
    };
}
