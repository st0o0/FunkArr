namespace FunkArr.Download.Subtitles;

internal enum SubtitleFailureReason
{
    Unknown = 0,
    DownloadFailed = 1,
    EmptyContent = 2,
    UnrecognizedFormat = 3,
    ConversionFailed = 4
}

internal abstract record SubtitleResult
{
    public sealed record Succeeded(SubtitleTrack Track, string FilePath) : SubtitleResult;
    public sealed record Failed(SubtitleFailureReason Reason, string? Detail = null) : SubtitleResult;
    public sealed record Unavailable : SubtitleResult;
}
