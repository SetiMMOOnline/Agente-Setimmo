using System.Text.Json.Serialization;

namespace RagnaForge.Agent.Core.Implementation;

public enum ImplementationIntent
{
    Review,
    Fix,
    CreateContent,
    Implement
}

public sealed class ImplementationRequest
{
    public ImplementationIntent Intent { get; set; } = ImplementationIntent.Implement;
    public string Workspace { get; set; } = "main";
    public string TargetPath { get; set; } = string.Empty;
    public string? LanguageHint { get; set; }
    public string? Template { get; set; }
    public string? Title { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Instruction { get; set; }
    public string? ContentFilePath { get; set; }
    public string? Content { get; set; }
}

public sealed class SemanticPatchPlan
{
    [JsonPropertyName("status")] public string Status { get; set; } = "planned";
    [JsonPropertyName("intent")] public string Intent { get; set; } = "unknown";
    [JsonPropertyName("targetKind")] public string TargetKind { get; set; } = "file";
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("targetContent")] public string? TargetContent { get; set; }
    [JsonPropertyName("requiresCodexReview")] public bool RequiresCodexReview { get; set; }
    [JsonPropertyName("needsCodexRepair")] public bool NeedsCodexRepair { get; set; }
    [JsonPropertyName("semanticConfidence")] public double SemanticConfidence { get; set; }
    [JsonPropertyName("patchQuality")] public PatchQualityReport PatchQuality { get; set; } = new();
    [JsonPropertyName("riskLevel")] public string RiskLevel { get; set; } = "medium";
    [JsonPropertyName("advice")] public List<string> Advice { get; set; } = [];
}

public sealed class PatchQualityReport
{
    [JsonPropertyName("valid")] public bool Valid { get; set; }
    [JsonPropertyName("classification")] public string Classification { get; set; } = "unknown";
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("blockers")] public List<string> Blockers { get; set; } = [];
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
}

public sealed class OperationSupervisionMetadata
{
    [JsonPropertyName("supervisionMode")] public string SupervisionMode { get; set; } = "codex-supervised";
    [JsonPropertyName("governanceProfile")] public string GovernanceProfile { get; set; } = "strict";
    [JsonPropertyName("generatedBy")] public string GeneratedBy { get; set; } = "setimmo";
    [JsonPropertyName("reviewedBy")] public string? ReviewedBy { get; set; }
    [JsonPropertyName("requiresCodexReview")] public bool RequiresCodexReview { get; set; } = true;
    [JsonPropertyName("codexReviewStatus")] public string CodexReviewStatus { get; set; } = "pending";
    [JsonPropertyName("semanticConfidence")] public double SemanticConfidence { get; set; }
    [JsonPropertyName("codexReviewThreshold")] public double CodexReviewThreshold { get; set; }
    [JsonPropertyName("autoApplyThreshold")] public double AutoApplyThreshold { get; set; }
    [JsonPropertyName("patchQuality")] public PatchQualityReport PatchQuality { get; set; } = new();
    [JsonPropertyName("riskLevel")] public string RiskLevel { get; set; } = "medium";
    [JsonPropertyName("canAutoApply")] public bool CanAutoApply { get; set; }
    [JsonPropertyName("needsCodexRepair")] public bool NeedsCodexRepair { get; set; }
    [JsonPropertyName("operationScopedAuthorization")] public object OperationScopedAuthorization { get; set; } = new();
    [JsonPropertyName("rollbackAvailable")] public bool RollbackAvailable { get; set; }
    [JsonPropertyName("contextPackPath")] public string? ContextPackPath { get; set; }
}

public sealed class ImplementationReviewIssue
{
    [JsonPropertyName("severity")] public string Severity { get; set; } = "info";
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("line")] public int? Line { get; set; }
    [JsonPropertyName("canAutoFix")] public bool CanAutoFix { get; set; }
    [JsonPropertyName("blocksApply")] public bool BlocksApply { get; set; }
}

public sealed class LanguageValidationMessage
{
    [JsonPropertyName("severity")] public string Severity { get; set; } = "info";
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}

public sealed class LanguageValidationResult
{
    [JsonPropertyName("valid")] public bool Valid { get; set; } = true;
    [JsonPropertyName("messages")] public List<LanguageValidationMessage> Messages { get; set; } = [];
    [JsonPropertyName("formattedContent")] public string FormattedContent { get; set; } = string.Empty;
}

public sealed class LanguageScaffoldRequest
{
    public string TargetPath { get; set; } = string.Empty;
    public string Template { get; set; } = "default";
    public string? Title { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class ProjectLanguageContext
{
    public string RootPath { get; set; } = string.Empty;
    public string? PackageJsonContent { get; set; }
    public string? ComposerJsonContent { get; set; }
    public bool HasPomXml { get; set; }
    public bool HasGradleBuild { get; set; }
    public bool HasMakefile { get; set; }
    public bool HasCMakeLists { get; set; }
    public bool HasPyProject { get; set; }
    public bool HasRequirementsTxt { get; set; }
    public bool HasShellScripts { get; set; }
    public bool HasDotnetSolution { get; set; }
    public bool HasLuaFiles { get; set; }
    public List<string> SampleFiles { get; set; } = [];
}

public sealed class RollbackFileEntry
{
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("action")] public string Action { get; set; } = string.Empty;
    [JsonPropertyName("existedBefore")] public bool ExistedBefore { get; set; }
    [JsonPropertyName("originalContent")] public string? OriginalContent { get; set; }
    [JsonPropertyName("targetContent")] public string? TargetContent { get; set; }
    [JsonPropertyName("language")] public string? Language { get; set; }
    [JsonPropertyName("semanticIntent")] public string? SemanticIntent { get; set; }
    [JsonPropertyName("patchQuality")] public PatchQualityReport? PatchQuality { get; set; }
}

public sealed class RollbackPlanDocument
{
    [JsonPropertyName("operationId")] public string OperationId { get; set; } = string.Empty;
    [JsonPropertyName("createdAtUtc")] public DateTimeOffset CreatedAtUtc { get; set; }
    [JsonPropertyName("applied")] public bool Applied { get; set; }
    [JsonPropertyName("note")] public string Note { get; set; } = string.Empty;
    [JsonPropertyName("files")] public List<RollbackFileEntry> Files { get; set; } = [];
}

public sealed class ImplementationPlanDocument
{
    [JsonPropertyName("operationId")] public string OperationId { get; set; } = string.Empty;
    [JsonPropertyName("intent")] public string Intent { get; set; } = string.Empty;
    [JsonPropertyName("workspace")] public string Workspace { get; set; } = string.Empty;
    [JsonPropertyName("language")] public string Language { get; set; } = string.Empty;
    [JsonPropertyName("instruction")] public string? Instruction { get; set; }
    [JsonPropertyName("supervision")] public OperationSupervisionMetadata Supervision { get; set; } = new();
    [JsonPropertyName("files")] public List<RollbackFileEntry> Files { get; set; } = [];
}
