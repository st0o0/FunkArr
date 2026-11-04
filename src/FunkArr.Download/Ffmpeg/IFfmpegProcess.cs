namespace FunkArr.Download.Ffmpeg;

internal interface IFfmpegProcess
{
    Task<FfmpegResult> ExecuteAsync(FfmpegInput input, Action<ProgressUpdate> onProgress, CancellationToken ct);
}
