using System.Text.Json.Serialization;

namespace RagnaForge.Agent.Core.Configuration;

/// <summary>
/// Safety configuration from safety.json.
/// Enforces mandatory gates before any destructive operation.
/// </summary>
public sealed class SafetyConfig
{
    [JsonPropertyName("operationProfile")]
    public string OperationProfile { get; set; } = "strict";

    [JsonPropertyName("requireDryRunBeforeApply")]
    public bool RequireDryRunBeforeApply { get; set; } = true;

    [JsonPropertyName("requireDiffBeforeApply")]
    public bool RequireDiffBeforeApply { get; set; } = true;

    [JsonPropertyName("requireValidationBeforeApply")]
    public bool RequireValidationBeforeApply { get; set; } = true;

    [JsonPropertyName("requireExplicitConfirmation")]
    public bool RequireExplicitConfirmation { get; set; } = true;

    [JsonPropertyName("applyConfirmationText")]
    public string ApplyConfirmationText { get; set; } = "APLICAR";

    [JsonPropertyName("rollbackConfirmationText")]
    public string RollbackConfirmationText { get; set; } = "REVERTER";

    [JsonPropertyName("backupBeforeApply")]
    public bool BackupBeforeApply { get; set; } = true;

    [JsonPropertyName("blockOriginalGrfWrite")]
    public bool BlockOriginalGrfWrite { get; set; } = true;

    [JsonPropertyName("blockLubEditing")]
    public bool BlockLubEditing { get; set; } = true;

    [JsonPropertyName("maxFilesPerOperationWithoutWarning")]
    public int MaxFilesPerOperationWithoutWarning { get; set; } = 10;

    [JsonPropertyName("stopOnCriticalWarning")]
    public bool StopOnCriticalWarning { get; set; } = true;

    [JsonPropertyName("aiOperatorsMustUseReviewMode")]
    public bool AiOperatorsMustUseReviewMode { get; set; } = true;

    [JsonPropertyName("blockDangerousShellCommandsByDefault")]
    public bool BlockDangerousShellCommandsByDefault { get; set; } = true;

    [JsonPropertyName("invalidateCacheOnPathChange")]
    public bool InvalidateCacheOnPathChange { get; set; } = true;

    [JsonPropertyName("cacheMustMatchActiveProfile")]
    public bool CacheMustMatchActiveProfile { get; set; } = true;

    public string GetNormalizedOperationProfile() => NormalizeOperationProfile(OperationProfile);

    public static string NormalizeOperationProfile(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "standalone" or "standalone-relaxed" or "standalone_relaxed" => "standalone-relaxed",
            "api" or "api-restricted" or "api_restricted" => "api-restricted",
            "production" or "production-strict" or "production_strict" => "production-strict",
            "local" or "local-dev" or "local_dev" => "standalone-relaxed",
            "sandbox" => "sandbox",
            _ => "api-restricted"
        };
    }

    public double GetCodexReviewThreshold() => GetNormalizedOperationProfile() switch
    {
        "sandbox" => 0.68,
        "standalone-relaxed" => 0.72,
        "api-restricted" => 0.86,
        "production-strict" => 1.0,
        _ => 0.90
    };

    public double GetAutoApplyThreshold() => GetNormalizedOperationProfile() switch
    {
        "sandbox" => 0.68,
        "standalone-relaxed" => 0.72,
        "api-restricted" => 0.95,
        "production-strict" => 1.0,
        _ => 0.90
    };

    public bool AllowsRiskWithoutCodexReview(string? riskLevel)
    {
        var risk = string.IsNullOrWhiteSpace(riskLevel)
            ? "unknown"
            : riskLevel.Trim().ToLowerInvariant();

        return GetNormalizedOperationProfile() switch
        {
            "sandbox" => risk is "low" or "medium",
            "standalone-relaxed" => risk is "low" or "medium",
            "api-restricted" => risk == "low",
            "production-strict" => false,
            _ => risk == "low"
        };
    }

    public string DescribeOperationProfile() => GetNormalizedOperationProfile() switch
    {
        "sandbox" => "Sandbox profile: low and medium risk semantic patches may flow with less Codex friction inside writable roots.",
        "standalone-relaxed" => "Standalone-relaxed profile: trusted local operator mode for low and medium risk semantic patches inside approved writable roots.",
        "api-restricted" => "API-restricted profile: public API/UI surfaces stay conservative and require review for medium or higher risk.",
        "production-strict" => "Production-strict profile: no auto-apply; human approval, rollback, audit, diff and Codex review are required.",
        _ => "Strict profile: medium and high risk patches stay Codex-supervised."
    };
}
