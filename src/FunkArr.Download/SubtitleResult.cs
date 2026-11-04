namespace FunkArr.Download;

internal enum SubtitleFailureReason
{
    DownloadFailed = 0,
    EmptyContent = 1,
    UnrecognizedFormat = 2,
    ConversionFailed = 3
}

internal abstract record SubtitleResult
{
    public sealed record Succeeded(SubtitleTrack Track, string FilePath) : SubtitleResult;
    public sealed record Failed(SubtitleFailureReason Reason, string? Detail = null) : SubtitleResult;
    public sealed record Unavailable : SubtitleResult;
}
