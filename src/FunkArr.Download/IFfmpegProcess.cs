namespace FunkArr.Download;

internal interface IFfmpegProcess
{
    Task<FfmpegResult> ExecuteAsync(FfmpegInput input, Action<ProgressUpdate> onProgress, CancellationToken ct);
}
