namespace FunkArr.Download;

internal enum SubtitleFailureReason
{
    DownloadFailed,
    EmptyContent,
    UnrecognizedFormat,
    ConversionFailed,
}

internal abstract record SubtitleResult
{
    public sealed record Succeeded(SubtitleTrack Track, string FilePath) : SubtitleResult;
    public sealed record Failed(SubtitleFailureReason Reason, string? Detail = null) : SubtitleResult;
    public sealed record Unavailable : SubtitleResult;
}
