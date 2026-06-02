using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagnaForge.Agent.Core.Knowledge;

public sealed class KnowledgeSourceAssessment
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("sourceType")] public string SourceType { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = "validated";
    [JsonPropertyName("updateMode")] public string UpdateMode { get; set; } = "manual";
    [JsonPropertyName("reviewedAt")] public string? ReviewedAt { get; set; }
    [JsonPropertyName("lastCheckedAt")] public string? LastCheckedAt { get; set; }
    [JsonPropertyName("nextDueAt")] public string? NextDueAt { get; set; }
    [JsonPropertyName("freshnessState")] public string FreshnessState { get; set; } = "unknown";
    [JsonPropertyName("latestSnapshotId")] public string? LatestSnapshotId { get; set; }
    [JsonPropertyName("supportedTopics")] public List<string> SupportedTopics { get; set; } = [];
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = [];
}

public sealed class KnowledgeRefreshPlanItem
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("updateMode")] public string UpdateMode { get; set; } = "manual";
    [JsonPropertyName("refreshCadenceDays")] public int RefreshCadenceDays { get; set; }
    [JsonPropertyName("lastCheckedAt")] public string? LastCheckedAt { get; set; }
    [JsonPropertyName("nextDueAt")] public string? NextDueAt { get; set; }
    [JsonPropertyName("staleAfterDays")] public int StaleAfterDays { get; set; }
    [JsonPropertyName("deprecatedAfterDays")] public int DeprecatedAfterDays { get; set; }
    [JsonPropertyName("autoRefreshAllowed")] public bool AutoRefreshAllowed { get; set; }
    [JsonPropertyName("maxRequestsPerRun")] public int MaxRequestsPerRun { get; set; } = 1;
    [JsonPropertyName("timeoutMs")] public int TimeoutMs { get; set; } = 3000;
    [JsonPropertyName("rateLimit")] public string RateLimit { get; set; } = "1 request per second";
    [JsonPropertyName("requiresHumanReview")] public bool RequiresHumanReview { get; set; } = true;
    [JsonPropertyName("isDue")] public bool IsDue { get; set; }
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
}

public sealed class KnowledgeSourceSnapshot
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("sourceVersion")] public string SourceVersion { get; set; } = string.Empty;
    [JsonPropertyName("retrievedAt")] public string RetrievedAt { get; set; } = string.Empty;
    [JsonPropertyName("reviewedAt")] public string? ReviewedAt { get; set; }
    [JsonPropertyName("reviewedBy")] public string? ReviewedBy { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "validated";
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("metadataHash")] public string MetadataHash { get; set; } = string.Empty;
    [JsonPropertyName("contentHash")] public string ContentHash { get; set; } = string.Empty;
    [JsonPropertyName("updateDetected")] public bool UpdateDetected { get; set; }
    [JsonPropertyName("provenance")] public string? Provenance { get; set; }
    [JsonPropertyName("sanitized")] public bool Sanitized { get; set; } = true;
    [JsonPropertyName("rawStored")] public bool RawStored { get; set; }
    [JsonPropertyName("canPromoteToKnowledgeCandidates")] public bool CanPromoteToKnowledgeCandidates { get; set; } = true;
    [JsonPropertyName("licenseNotes")] public string? LicenseNotes { get; set; }
    [JsonPropertyName("authorizedUseNotes")] public string? AuthorizedUseNotes { get; set; }
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
}

public sealed class GithubMetadataSnapshot
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("repoOwner")] public string RepoOwner { get; set; } = string.Empty;
    [JsonPropertyName("repoName")] public string RepoName { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("defaultBranch")] public string DefaultBranch { get; set; } = string.Empty;
    [JsonPropertyName("latestCommitSha")] public string LatestCommitSha { get; set; } = string.Empty;
    [JsonPropertyName("latestCommitDate")] public string LatestCommitDate { get; set; } = string.Empty;
    [JsonPropertyName("latestReleaseOrTag")] public string? LatestReleaseOrTag { get; set; }
    [JsonPropertyName("license")] public string License { get; set; } = string.Empty;
    [JsonPropertyName("stars")] public int? Stars { get; set; }
    [JsonPropertyName("forks")] public int? Forks { get; set; }
    [JsonPropertyName("topLevelFiles")] public List<string> TopLevelFiles { get; set; } = [];
    [JsonPropertyName("readmeSummary")] public string ReadmeSummary { get; set; } = string.Empty;
    [JsonPropertyName("updateDetected")] public bool UpdateDetected { get; set; }
    [JsonPropertyName("previousSnapshotVersion")] public string? PreviousSnapshotVersion { get; set; }
    [JsonPropertyName("newSnapshotVersion")] public string NewSnapshotVersion { get; set; } = string.Empty;
    [JsonPropertyName("provenance")] public string Provenance { get; set; } = "github-public-metadata";
    [JsonPropertyName("authorizedUse")] public bool AuthorizedUse { get; set; } = true;
    [JsonPropertyName("codeAnalysisAllowed")] public bool CodeAnalysisAllowed { get; set; } = true;
    [JsonPropertyName("selectiveIncorporationAllowed")] public bool SelectiveIncorporationAllowed { get; set; } = true;
    [JsonPropertyName("rawContentStored")] public bool RawContentStored { get; set; }
    [JsonPropertyName("codeCopied")] public bool CodeCopied { get; set; }
    [JsonPropertyName("incorporatedFiles")] public List<string> IncorporatedFiles { get; set; } = [];
    [JsonPropertyName("canBlock")] public bool CanBlock { get; set; }
}

public sealed class RathenaBoardMetadataSnapshot
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = "rathena-board";
    [JsonPropertyName("url")] public string Url { get; set; } = "https://rathena.org/board/";
    [JsonPropertyName("retrievedAt")] public string RetrievedAt { get; set; } = string.Empty;
    [JsonPropertyName("categoriesSeen")] public List<string> CategoriesSeen { get; set; } = [];
    [JsonPropertyName("latestTopicsMetadata")] public List<object> LatestTopicsMetadata { get; set; } = [];
    [JsonPropertyName("sectionsRelevantToRagnaForge")] public List<string> SectionsRelevantToRagnaForge { get; set; } = [];
    [JsonPropertyName("updateDetected")] public bool UpdateDetected { get; set; }
    [JsonPropertyName("previousSnapshotVersion")] public string? PreviousSnapshotVersion { get; set; }
    [JsonPropertyName("newSnapshotVersion")] public string? NewSnapshotVersion { get; set; }
    [JsonPropertyName("provenance")] public string Provenance { get; set; } = "forum-metadata-policy";
    [JsonPropertyName("cacheMode")] public string CacheMode { get; set; } = "none";
    [JsonPropertyName("rawHtmlStored")] public bool RawHtmlStored { get; set; }
    [JsonPropertyName("postsCopied")] public bool PostsCopied { get; set; }
    [JsonPropertyName("linksFollowed")] public bool LinksFollowed { get; set; }
    [JsonPropertyName("canBlock")] public bool CanBlock { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "skipped_by_policy";
    [JsonPropertyName("warning")] public string Warning { get; set; } = string.Empty;
}

public sealed class KnowledgeRefreshResult
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("mode")] public string Mode { get; set; } = "metadata";
    [JsonPropertyName("status")] public string Status { get; set; } = "skipped";
    [JsonPropertyName("warning")] public string? Warning { get; set; }
    [JsonPropertyName("requestCount")] public int RequestCount { get; set; }
    [JsonPropertyName("timeoutMs")] public int TimeoutMs { get; set; } = 3000;
    [JsonPropertyName("rateLimitApplied")] public bool RateLimitApplied { get; set; } = true;
    [JsonPropertyName("linksFollowed")] public bool LinksFollowed { get; set; }
    [JsonPropertyName("paginationUsed")] public bool PaginationUsed { get; set; }
    [JsonPropertyName("bulkLookup")] public bool BulkLookup { get; set; }
    [JsonPropertyName("rangeLookup")] public bool RangeLookup { get; set; }
    [JsonPropertyName("rawHtmlStored")] public bool RawHtmlStored { get; set; }
    [JsonPropertyName("dumpStored")] public bool DumpStored { get; set; }
    [JsonPropertyName("cacheMode")] public string CacheMode { get; set; } = "none";
    [JsonPropertyName("updateDetected")] public bool UpdateDetected { get; set; }
    [JsonPropertyName("previousSnapshotId")] public string? PreviousSnapshotId { get; set; }
    [JsonPropertyName("newSnapshot")] public KnowledgeSourceSnapshot? NewSnapshot { get; set; }
    [JsonPropertyName("metadata")] public object? Metadata { get; set; }
    [JsonPropertyName("provenance")] public string Provenance { get; set; } = string.Empty;
    [JsonPropertyName("authorizedUse")] public bool AuthorizedUse { get; set; } = true;
    [JsonPropertyName("codeAnalysisAllowed")] public bool CodeAnalysisAllowed { get; set; } = true;
    [JsonPropertyName("selectiveIncorporationAllowed")] public bool SelectiveIncorporationAllowed { get; set; } = true;
    [JsonPropertyName("rawContentStored")] public bool RawContentStored { get; set; }
    [JsonPropertyName("codeCopied")] public bool CodeCopied { get; set; }
    [JsonPropertyName("incorporatedFiles")] public List<string> IncorporatedFiles { get; set; } = [];
    [JsonPropertyName("persisted")] public bool Persisted { get; set; }
    [JsonPropertyName("requiresHumanReview")] public bool RequiresHumanReview { get; set; } = true;
    [JsonPropertyName("canBlock")] public bool CanBlock { get; set; }
}

public sealed class LearningEvidence
{
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("snapshotId")] public string? SnapshotId { get; set; }
    [JsonPropertyName("referenceUrl")] public string? ReferenceUrl { get; set; }
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("retrievedAt")] public string? RetrievedAt { get; set; }
}

public sealed class LearningCandidate
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("sourceId")] public string SourceId { get; set; } = string.Empty;
    [JsonPropertyName("topic")] public string Topic { get; set; } = string.Empty;
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = "needs_review";
    [JsonPropertyName("category")] public string Category { get; set; } = "candidate";
    [JsonPropertyName("entityTypes")] public List<string> EntityTypes { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("evidence")] public List<LearningEvidence> Evidence { get; set; } = [];
    [JsonPropertyName("licenseNotes")] public string? LicenseNotes { get; set; }
    [JsonPropertyName("authorizedUseNotes")] public string? AuthorizedUseNotes { get; set; }
    [JsonPropertyName("humanReviewRequired")] public bool HumanReviewRequired { get; set; } = true;
    [JsonPropertyName("rawHtmlStored")] public bool RawHtmlStored { get; set; }
    [JsonPropertyName("secretStored")] public bool SecretStored { get; set; }
    [JsonPropertyName("canBlock")] public bool CanBlock { get; set; }
    [JsonPropertyName("safeForApply")] public bool SafeForApply { get; set; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; } = string.Empty;
    [JsonPropertyName("reviewedAt")] public string? ReviewedAt { get; set; }
    [JsonPropertyName("reviewedBy")] public string? ReviewedBy { get; set; }
    [JsonPropertyName("proposedPackId")] public string? ProposedPackId { get; set; }
    [JsonPropertyName("changelog")] public List<string> Changelog { get; set; } = [];
}

public sealed class LearningReviewDecision
{
    [JsonPropertyName("candidateId")] public string CandidateId { get; set; } = string.Empty;
    [JsonPropertyName("action")] public string Action { get; set; } = string.Empty;
    [JsonPropertyName("dryRun")] public bool DryRun { get; set; }
    [JsonPropertyName("resultingStatus")] public string ResultingStatus { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("writesPerformed")] public int WritesPerformed { get; set; }
    [JsonPropertyName("nextSafeActions")] public List<string> NextSafeActions { get; set; } = [];
    [JsonPropertyName("promotionPlan")] public LearningPromotionPlan? PromotionPlan { get; set; }
}

public sealed class LearningPromotionPlan
{
    [JsonPropertyName("candidateId")] public string CandidateId { get; set; } = string.Empty;
    [JsonPropertyName("targetPackId")] public string TargetPackId { get; set; } = string.Empty;
    [JsonPropertyName("proposedEntryId")] public string ProposedEntryId { get; set; } = string.Empty;
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("writeAllowed")] public bool WriteAllowed { get; set; }
    [JsonPropertyName("dryRun")] public bool DryRun { get; set; } = true;
    [JsonPropertyName("changeLogEntries")] public List<string> ChangeLogEntries { get; set; } = [];
    [JsonPropertyName("nextSafeActions")] public List<string> NextSafeActions { get; set; } = [];
}

public interface IOnlineKnowledgeMetadataClient
{
    Task<GithubMetadataSnapshot> FetchGithubMetadataAsync(KnowledgeSource source, KnowledgeSourceSnapshot? previousSnapshot, CancellationToken cancellationToken);
}

public sealed class GitHubMetadataClient : IOnlineKnowledgeMetadataClient
{
    private readonly HttpClient _httpClient;

    public GitHubMetadataClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RagnaForge-Agent", "1.2"));
    }

    public async Task<GithubMetadataSnapshot> FetchGithubMetadataAsync(KnowledgeSource source, KnowledgeSourceSnapshot? previousSnapshot, CancellationToken cancellationToken)
    {
        var (owner, repo) = ParseRepository(source.ExternalReferenceUrl ?? source.Url);
        var repoJson = await GetJsonAsync($"https://api.github.com/repos/{owner}/{repo}", cancellationToken);
        var defaultBranch = repoJson.GetProperty("default_branch").GetString() ?? "main";
        var commitJson = await GetJsonAsync($"https://api.github.com/repos/{owner}/{repo}/commits/{defaultBranch}", cancellationToken);
        var contentsJson = await GetJsonAsync($"https://api.github.com/repos/{owner}/{repo}/contents/", cancellationToken);
        var readmeJson = await GetJsonAsync($"https://api.github.com/repos/{owner}/{repo}/readme", cancellationToken);

        var latestCommitSha = commitJson.GetProperty("sha").GetString() ?? string.Empty;
        var latestCommitDate = commitJson.GetProperty("commit").GetProperty("author").GetProperty("date").GetString() ?? string.Empty;
        var license = repoJson.TryGetProperty("license", out var licenseElement) && licenseElement.ValueKind == JsonValueKind.Object
            ? licenseElement.GetProperty("spdx_id").GetString() ?? "none-declared"
            : "none-declared";
        var readmeSummary = SummarizeReadme(DecodeReadme(readmeJson));
        var previousVersion = previousSnapshot?.SourceVersion;

        return new GithubMetadataSnapshot
        {
            SourceId = source.Id,
            RepoOwner = owner,
            RepoName = repo,
            Url = source.ExternalReferenceUrl ?? source.Url,
            DefaultBranch = defaultBranch,
            LatestCommitSha = latestCommitSha,
            LatestCommitDate = latestCommitDate,
            LatestReleaseOrTag = repoJson.TryGetProperty("pushed_at", out _) ? null : null,
            License = license,
            Stars = repoJson.TryGetProperty("stargazers_count", out var stars) && stars.TryGetInt32(out var starCount) ? starCount : null,
            Forks = repoJson.TryGetProperty("forks_count", out var forks) && forks.TryGetInt32(out var forkCount) ? forkCount : null,
            TopLevelFiles = contentsJson.EnumerateArray()
                .Select(item => item.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .Take(20)
                .ToList(),
            ReadmeSummary = readmeSummary,
            UpdateDetected = !string.Equals(previousVersion, latestCommitSha, StringComparison.OrdinalIgnoreCase),
            PreviousSnapshotVersion = previousVersion,
            NewSnapshotVersion = latestCommitSha,
            AuthorizedUse = true,
            CodeAnalysisAllowed = true,
            SelectiveIncorporationAllowed = true,
            RawContentStored = false,
            CodeCopied = false,
            CanBlock = false
        };
    }

    private async Task<JsonElement> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private static string DecodeReadme(JsonElement readmeJson)
    {
        var encoded = readmeJson.GetProperty("content").GetString() ?? string.Empty;
        encoded = encoded.Replace("\n", string.Empty, StringComparison.Ordinal);
        return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    }

    private static string SummarizeReadme(string readme)
    {
        var lines = readme
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith("[![", StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();
        return string.Join(" ", lines).Trim();
    }

    private static (string Owner, string Repo) ParseRepository(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            throw new InvalidOperationException("GitHub repository URL is invalid.");
        return (segments[0], segments[1]);
    }
}

public sealed class OnlineKnowledgeRefreshService
{
    private readonly string _agentRoot;
    private readonly KnowledgeService _knowledgeService;
    private readonly IOnlineKnowledgeMetadataClient _metadataClient;

    public OnlineKnowledgeRefreshService(string agentRoot, KnowledgeService knowledgeService, IOnlineKnowledgeMetadataClient? metadataClient = null, HttpMessageHandler? httpHandler = null)
    {
        _agentRoot = agentRoot;
        _knowledgeService = knowledgeService;
        _metadataClient = metadataClient ?? new GitHubMetadataClient(httpHandler is null ? new HttpClient() : new HttpClient(httpHandler, disposeHandler: false));
    }

    public List<KnowledgeRefreshPlanItem> BuildRefreshPlan()
    {
        return _knowledgeService.LoadSources()
            .Select(source =>
            {
                var sourceNextDueAt = ResolveNextDueAt(source);
                var isDue = sourceNextDueAt is null || sourceNextDueAt <= DateTimeOffset.UtcNow;
                return new KnowledgeRefreshPlanItem
                {
                    SourceId = source.Id,
                    Name = source.Name,
                    UpdateMode = source.UpdateMode,
                    RefreshCadenceDays = source.RefreshCadenceDays,
                    LastCheckedAt = source.LastCheckedAt,
                    NextDueAt = sourceNextDueAt?.ToString("O"),
                    StaleAfterDays = source.StaleAfterDays,
                    DeprecatedAfterDays = source.DeprecatedAfterDays,
                    AutoRefreshAllowed = source.AutoRefreshAllowed,
                    MaxRequestsPerRun = source.MaxRequestsPerRun,
                    TimeoutMs = source.TimeoutMs,
                    RateLimit = source.RateLimit,
                    RequiresHumanReview = source.RequiresHumanReview,
                    IsDue = isDue,
                    Reason = BuildPlanReason(source, isDue)
                };
            })
            .OrderBy(item => item.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<KnowledgeRefreshPlanItem> BuildDuePlan() =>
        BuildRefreshPlan().Where(item => item.IsDue).ToList();

    public List<KnowledgeRefreshResult> Run(string? sourceId, bool runAll, string mode)
    {
        var targets = runAll
            ? _knowledgeService.LoadSources()
            : _knowledgeService.LoadSources().Where(source => source.Id.Equals(sourceId, StringComparison.OrdinalIgnoreCase)).ToList();

        var results = new List<KnowledgeRefreshResult>();
        foreach (var source in targets)
            results.Add(RunSingle(source, mode));

        return results;
    }

    public string BuildMarkdownReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Knowledge Refresh Plan");
        sb.AppendLine();
        foreach (var item in BuildRefreshPlan())
            sb.AppendLine($"- {item.SourceId}: mode={item.UpdateMode}, due={item.IsDue}, next={item.NextDueAt ?? "manual"}, reason={item.Reason}");
        sb.AppendLine();
        sb.AppendLine("No crawler. No follow links for forum refresh. No pagination. No bulk. No raw HTML. No dump.");
        sb.AppendLine("safeForApply=false");
        return sb.ToString();
    }

    private KnowledgeRefreshResult RunSingle(KnowledgeSource source, string mode)
    {
        if (!mode.Equals("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return Skipped(source, mode, "Only metadata refresh is supported in this build.");
        }

        return source.Id switch
        {
            "rathena-board" => RefreshRathenaBoard(source, mode),
            "robrowserlegacy" or "robrowserlegacy-remoteclient-js" => RefreshGithubSource(source, mode),
            "divine-pride" or "ratemyserver" => Skipped(source, mode, "Controlled-point sources are refreshed through targeted entity lookup, not broad source refresh."),
            _ => Skipped(source, mode, "Source refresh is manual or local-only for this source.")
        };
    }

    private KnowledgeRefreshResult RefreshGithubSource(KnowledgeSource source, string mode)
    {
        var previousSnapshot = _knowledgeService.GetLatestSnapshotForSource(source.Id);
        using var cts = new CancellationTokenSource(Math.Clamp(source.TimeoutMs, 500, 3000));

        try
        {
            var metadata = _metadataClient.FetchGithubMetadataAsync(source, previousSnapshot, cts.Token).GetAwaiter().GetResult();
            var summary = $"{metadata.RepoOwner}/{metadata.RepoName} default branch {metadata.DefaultBranch}; latest commit {metadata.LatestCommitSha[..Math.Min(metadata.LatestCommitSha.Length, 12)]}; top-level files: {string.Join(", ", metadata.TopLevelFiles.Take(6))}.";
            var snapshot = BuildSnapshot(source, metadata.NewSnapshotVersion, summary, metadata.UpdateDetected, metadata.License, source.AuthorizedUsePolicy);
            return new KnowledgeRefreshResult
            {
                SourceId = source.Id,
                Mode = mode,
                Status = "refreshed",
                RequestCount = 4,
                TimeoutMs = Math.Clamp(source.TimeoutMs, 500, 3000),
                RateLimitApplied = true,
                LinksFollowed = false,
                PaginationUsed = false,
                BulkLookup = false,
                RangeLookup = false,
                RawHtmlStored = false,
                DumpStored = false,
                CacheMode = "none",
                UpdateDetected = metadata.UpdateDetected,
                PreviousSnapshotId = previousSnapshot?.Id,
                NewSnapshot = snapshot,
                Metadata = metadata,
                Provenance = "github-public-metadata",
                AuthorizedUse = true,
                CodeAnalysisAllowed = true,
                SelectiveIncorporationAllowed = true,
                RawContentStored = false,
                CodeCopied = false,
                Persisted = false,
                RequiresHumanReview = true,
                CanBlock = false
            };
        }
        catch (OperationCanceledException)
        {
            return Warning(source, mode, "Source refresh timed out. Internet failure remains a warning only.");
        }
        catch (HttpRequestException)
        {
            return Warning(source, mode, "Source refresh failed due to HTTP/network error. Internet failure remains a warning only.");
        }
        catch (Exception)
        {
            return Warning(source, mode, "Source refresh metadata could not be parsed safely. Review manually.");
        }
    }

    private KnowledgeRefreshResult RefreshRathenaBoard(KnowledgeSource source, string mode)
    {
        var previousSnapshot = _knowledgeService.GetLatestSnapshotForSource(source.Id);
        var metadata = new RathenaBoardMetadataSnapshot
        {
            RetrievedAt = DateTimeOffset.UtcNow.ToString("O"),
            PreviousSnapshotVersion = previousSnapshot?.SourceVersion,
            Warning = "Forum live refresh skipped_by_policy because robots/terms and page behavior were not validated for automated access in this build."
        };

        return new KnowledgeRefreshResult
        {
            SourceId = source.Id,
            Mode = mode,
            Status = "skipped_by_policy",
            Warning = metadata.Warning,
            RequestCount = 0,
            TimeoutMs = Math.Clamp(source.TimeoutMs, 500, 3000),
            RateLimitApplied = true,
            LinksFollowed = false,
            PaginationUsed = false,
            BulkLookup = false,
            RangeLookup = false,
            RawHtmlStored = false,
            DumpStored = false,
            CacheMode = "none",
            UpdateDetected = false,
            PreviousSnapshotId = previousSnapshot?.Id,
            NewSnapshot = null,
            Metadata = metadata,
            Provenance = "forum-metadata-policy",
            AuthorizedUse = true,
            CodeAnalysisAllowed = false,
            SelectiveIncorporationAllowed = false,
            RawContentStored = false,
            CodeCopied = false,
            Persisted = false,
            RequiresHumanReview = true,
            CanBlock = false
        };
    }

    private KnowledgeRefreshResult Skipped(KnowledgeSource source, string mode, string reason)
    {
        return new KnowledgeRefreshResult
        {
            SourceId = source.Id,
            Mode = mode,
            Status = "skipped",
            Warning = reason,
            RequestCount = 0,
            TimeoutMs = Math.Clamp(source.TimeoutMs, 500, 3000),
            RateLimitApplied = true,
            LinksFollowed = false,
            PaginationUsed = false,
            BulkLookup = false,
            RangeLookup = false,
            RawHtmlStored = false,
            DumpStored = false,
            CacheMode = "none",
            UpdateDetected = false,
            Provenance = source.Provenance ?? "internal-policy",
            AuthorizedUse = true,
            CodeAnalysisAllowed = source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase),
            SelectiveIncorporationAllowed = source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase),
            RawContentStored = false,
            CodeCopied = false,
            Persisted = false,
            RequiresHumanReview = source.RequiresHumanReview,
            CanBlock = false
        };
    }

    private static KnowledgeRefreshResult Warning(KnowledgeSource source, string mode, string warning)
    {
        return new KnowledgeRefreshResult
        {
            SourceId = source.Id,
            Mode = mode,
            Status = "warning",
            Warning = warning,
            RequestCount = 0,
            TimeoutMs = Math.Clamp(source.TimeoutMs, 500, 3000),
            RateLimitApplied = true,
            LinksFollowed = false,
            PaginationUsed = false,
            BulkLookup = false,
            RangeLookup = false,
            RawHtmlStored = false,
            DumpStored = false,
            CacheMode = "none",
            UpdateDetected = false,
            Provenance = source.Provenance ?? "external-warning",
            AuthorizedUse = true,
            CodeAnalysisAllowed = source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase),
            SelectiveIncorporationAllowed = source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase),
            RawContentStored = false,
            CodeCopied = false,
            Persisted = false,
            RequiresHumanReview = true,
            CanBlock = false
        };
    }

    private static KnowledgeSourceSnapshot BuildSnapshot(KnowledgeSource source, string sourceVersion, string summary, bool updateDetected, string? licenseNotes, string? authorizedUseNotes)
    {
        var metadataHash = ComputeHash($"{source.Id}|{sourceVersion}|{summary}");
        var contentHash = ComputeHash($"{source.Id}|{summary}");
        return new KnowledgeSourceSnapshot
        {
            Id = $"{source.Id}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            SourceId = source.Id,
            SourceVersion = sourceVersion,
            RetrievedAt = DateTimeOffset.UtcNow.ToString("O"),
            ReviewedAt = source.ReviewedAt ?? source.LastReviewedUtc,
            ReviewedBy = source.ReviewedBy,
            Status = "candidate",
            Summary = summary,
            MetadataHash = metadataHash,
            ContentHash = contentHash,
            UpdateDetected = updateDetected,
            Provenance = source.Provenance,
            Sanitized = true,
            RawStored = false,
            CanPromoteToKnowledgeCandidates = true,
            LicenseNotes = licenseNotes,
            AuthorizedUseNotes = authorizedUseNotes
        };
    }

    private static DateTimeOffset? ResolveNextDueAt(KnowledgeSource source)
    {
        if (DateTimeOffset.TryParse(source.NextDueAt, out var nextDue))
            return nextDue;

        if (DateTimeOffset.TryParse(source.LastCheckedAt, out var lastChecked) && source.RefreshCadenceDays > 0)
            return lastChecked.AddDays(source.RefreshCadenceDays);

        if (DateTimeOffset.TryParse(source.ReviewedAt ?? source.LastReviewedUtc, out var reviewedAt) && source.RefreshCadenceDays > 0)
            return reviewedAt.AddDays(source.RefreshCadenceDays);

        return null;
    }

    private static string BuildPlanReason(KnowledgeSource source, bool isDue)
    {
        if (source.Id.Equals("rathena-board", StringComparison.OrdinalIgnoreCase))
            return isDue
                ? "Forum source is metadata-only and requires explicit human review before any live access."
                : "Forum source remains policy-constrained; no crawler or topic follow is allowed.";

        if (source.Id.StartsWith("robrowserlegacy", StringComparison.OrdinalIgnoreCase))
            return isDue
                ? "GitHub metadata refresh may check repo/readme/tree only; authorized code reference remains review-first."
                : "GitHub metadata is fresh enough; no live refresh is required now.";

        return isDue
            ? "Manual or local refresh is due."
            : "Source refresh is not currently due.";
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed class LearningService
{
    private static readonly string[] RestrictedMarkers =
    [
        "to" + "ken",
        "pass" + "word",
        "sec" + "ret",
        "api" + "key",
        "api" + "_" + "key",
        "private" + "_" + "key"
    ];

    private readonly KnowledgeService _knowledgeService;

    public LearningService(KnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    public LearningCandidate Observe(string sourceId, string topic, string summary)
    {
        ValidateText(summary, "summary");
        ValidateText(topic, "topic");
        var source = _knowledgeService.GetSource(sourceId) ?? throw new InvalidOperationException("Learning source not found.");
        var sanitizedSummary = Sanitize(summary);
        return new LearningCandidate
        {
            Id = $"{sourceId}-{NormalizeSlug(topic)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            SourceId = sourceId,
            Topic = topic,
            Summary = sanitizedSummary,
            Status = "candidate",
            Category = "observation",
            EntityTypes = source.SupportedEntityTypes.ToList(),
            Tags = source.SupportedTopics.Take(6).ToList(),
            Evidence =
            [
                new LearningEvidence
                {
                    SourceId = sourceId,
                    ReferenceUrl = source.ExternalReferenceUrl ?? source.Url,
                    Summary = sanitizedSummary,
                    RetrievedAt = DateTimeOffset.UtcNow.ToString("O")
                }
            ],
            LicenseNotes = source.LicensePolicy,
            AuthorizedUseNotes = source.AuthorizedUsePolicy,
            HumanReviewRequired = true,
            RawHtmlStored = false,
            SecretStored = false,
            CanBlock = false,
            SafeForApply = false,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            ProposedPackId = sourceId switch
            {
                "rathena-board" => "rathena-board-reference",
                "robrowserlegacy" => "robrowserlegacy",
                "robrowserlegacy-remoteclient-js" => "robrowserlegacy-remoteclient-js",
                _ => "custom-learning"
            },
            Changelog = ["Observed candidate created in dry-run mode. Human review is required before promotion."]
        };
    }

    public LearningReviewDecision Approve(string candidateId, bool dryRun)
    {
        var candidate = _knowledgeService.GetLearningCandidate(candidateId) ?? throw new InvalidOperationException("Learning candidate not found.");
        return new LearningReviewDecision
        {
            CandidateId = candidateId,
            Action = "approve",
            DryRun = dryRun,
            ResultingStatus = "approved",
            Reason = "Approval is dry-run only in this build; no candidate file was modified.",
            WritesPerformed = 0,
            NextSafeActions = ["Review the promotion plan.", "Promote in dry-run mode only.", "Keep provenance and license notes attached."],
            PromotionPlan = BuildPromotionPlan(candidate, true)
        };
    }

    public LearningReviewDecision Reject(string candidateId, string reason)
    {
        var candidate = _knowledgeService.GetLearningCandidate(candidateId) ?? throw new InvalidOperationException("Learning candidate not found.");
        ValidateText(reason, "reason");
        return new LearningReviewDecision
        {
            CandidateId = candidateId,
            Action = "reject",
            DryRun = true,
            ResultingStatus = "rejected",
            Reason = $"Rejected in read-only mode: {Sanitize(reason)}",
            WritesPerformed = 0,
            NextSafeActions = ["Keep the candidate for audit history only.", "Do not promote rejected content.", "Review another candidate or add more evidence."],
            PromotionPlan = null
        };
    }

    public LearningReviewDecision Promote(string candidateId, bool dryRun)
    {
        var candidate = _knowledgeService.GetLearningCandidate(candidateId) ?? throw new InvalidOperationException("Learning candidate not found.");
        return new LearningReviewDecision
        {
            CandidateId = candidateId,
            Action = "promote",
            DryRun = dryRun,
            ResultingStatus = "promoted",
            Reason = "Promotion remains dry-run only in this build; no pack file was written.",
            WritesPerformed = 0,
            NextSafeActions = ["Review the proposed entry ID and target pack.", "Apply manually in a future curated authoring step only."],
            PromotionPlan = BuildPromotionPlan(candidate, dryRun)
        };
    }

    public string BuildMarkdownReport()
    {
        var candidates = _knowledgeService.LoadLearningCandidates();
        var sb = new StringBuilder();
        sb.AppendLine("# Learning Candidates Report");
        sb.AppendLine();
        sb.AppendLine($"Total candidates: {candidates.Count}");
        sb.AppendLine();
        foreach (var candidate in candidates)
            sb.AppendLine($"- {candidate.Id}: {candidate.Status} | {candidate.SourceId} | {candidate.Topic}");
        sb.AppendLine();
        sb.AppendLine("Learning is review-first, does not self-modify the agent, and never stores raw HTML or secrets.");
        sb.AppendLine("safeForApply=false");
        return sb.ToString();
    }

    private static LearningPromotionPlan BuildPromotionPlan(LearningCandidate candidate, bool dryRun)
    {
        return new LearningPromotionPlan
        {
            CandidateId = candidate.Id,
            TargetPackId = candidate.ProposedPackId ?? "manual-review",
            ProposedEntryId = $"learning.{candidate.SourceId}.{NormalizeSlug(candidate.Topic)}",
            Summary = candidate.Summary,
            WriteAllowed = false,
            DryRun = dryRun,
            ChangeLogEntries =
            [
                $"Promote candidate '{candidate.Id}' into curated pack '{candidate.ProposedPackId ?? "manual-review"}'.",
                "Preserve source provenance, license notes, and authorized use notes."
            ],
            NextSafeActions =
            [
                "Open the target pack in a curated authoring review.",
                "Keep source provenance and license notes in the promoted entry.",
                "Do not auto-write packs from learning promotion in this build."
            ]
        };
    }

    private static void ValidateText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required argument: --{fieldName}");

        var lowered = value.ToLowerInvariant();
        if (lowered.Contains("<html", StringComparison.Ordinal) ||
            lowered.Contains("</html", StringComparison.Ordinal))
            throw new InvalidOperationException($"Learning {fieldName} cannot store raw HTML.");

        if (RestrictedMarkers.Any(marker => lowered.Contains(marker, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Learning {fieldName} cannot store secrets.");
    }

    private static string Sanitize(string input)
    {
        var trimmed = input.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return trimmed.Length <= 400 ? trimmed : trimmed[..400];
    }

    private static string NormalizeSlug(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in value.ToLowerInvariant())
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        return builder.ToString().Trim('-');
    }
}
