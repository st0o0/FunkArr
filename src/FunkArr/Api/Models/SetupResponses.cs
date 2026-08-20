namespace FunkArr.Api.Models;

public sealed record StatusResponse(
    bool Configured,
    FfmpegStatus Ffmpeg,
    PathsStatus Paths,
    MediathekStatus Mediathek,
    RulesetsStatus Rulesets,
    ProwlarrStatus Prowlarr,
    IReadOnlyList<ArrInstanceStatus> ArrInstances);

public sealed record FfmpegStatus(bool Found, string? Version);

public sealed record PathsStatus(bool DownloadOk, bool TempOk);

public sealed record MediathekStatus(bool Reachable);

public sealed record RulesetsStatus(int TopicCount);

public sealed record ProwlarrStatus(bool Connected);

public sealed record ArrInstanceStatus(string Name, bool Connected);

public sealed record TestConnectionResponse(bool Success, int? StatusCode = null, string? Error = null);

public sealed record TestPathsResponse(bool DownloadOk, bool TempOk);

public sealed record FfmpegResponse(bool Found, string? Version);

public sealed record MediathekResponse(bool Reachable);

public sealed record ConfigResponse(
    string? ApiKey,
    string? DownloadPath,
    string? TempPath,
    string? PersistencePath,
    int ConcurrentDownloads,
    string? PathMapping,
    string? LogFormat,
    string? RuleSetRepository,
    string? RuleSetVersion,
    string? RuleSetPath,
    int RuleSetRefreshIntervalMinutes,
    int MatchLedgerCapacity,
    bool QualityProbing,
    int QualityCacheTtlMinutes,
    int QualityCacheCapacity,
    int QualityProbeLimit,
    ProwlarrConfig? Prowlarr,
    IReadOnlyList<ArrInstanceConfig> ArrInstances);

public sealed record ProwlarrConfig(string? Url, string? ApiKey);

public sealed record ArrInstanceConfig(string? Name, string? Type, string? Url, string? ApiKey);

public sealed record SuccessResponse(bool Success);

public sealed record TestConnectionRequest(string? Url, string? ApiKey);

public sealed record TestArrRequest(string? Url, string? ApiKey, string? Type);

public sealed record TestPathsRequest(string? DownloadPath, string? TempPath);
