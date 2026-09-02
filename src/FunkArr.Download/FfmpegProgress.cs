namespace FunkArr.Download;

internal sealed record FfmpegProgress(
    long OutTimeUs,
    long TotalSize,
    double Speed,
    bool IsEnd);
