namespace FunkArr.Api.Models;

public sealed record StatusResponse(
    bool Configured,
    string ApiKey,
    FfmpegStatus Ffmpeg,
    PathsStatus Paths,
    MediathekStatus Mediathek,
    RulesetsStatus Rulesets);

public sealed record FfmpegStatus(bool Found, string? Version);

public sealed record PathsStatus(bool DownloadOk, bool TempOk);

public sealed record MediathekStatus(bool Reachable);

public sealed record RulesetsStatus(int TopicCount);

public sealed record TestConnectionResponse(bool Success, int? StatusCode = null, string? Error = null);

public sealed record TestPathsResponse(bool DownloadOk, bool TempOk);

public sealed record FfmpegResponse(bool Found, string? Version);

public sealed record MediathekResponse(bool Reachable);

public sealed record SuccessResponse(bool Success);

public sealed record TestConnectionRequest(string? Url, string? ApiKey);

public sealed record TestArrRequest(string? Url, string? ApiKey, string? Type);

public sealed record TestPathsRequest(string? DownloadPath, string? TempPath);
