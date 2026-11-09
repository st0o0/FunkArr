using System.Diagnostics;
using FFmpeg.Rx;
using FFmpeg.Rx.Builder;
using FFmpeg.Rx.Codecs;
using FFmpeg.Rx.Models;
using FunkArr.Messages.Download;
using R3;

namespace FunkArr.Download.Ffmpeg;

internal sealed class FfmpegProcess : IFfmpegProcess
{
    private const string UserAgent = "Mozilla/5.0";

    public async Task<FfmpegResult> ExecuteAsync(
        FfmpegInput input, Action<ProgressUpdate> onProgress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        await using var session = BuildSession(input);

        using var subscription = session.Progress.Subscribe(p =>
            onProgress(new ProgressUpdate(p.BytesWritten, p.TimeUs, p.Speed)));

        try
        {
            var result = await session.RunAsync(ct);
            sw.Stop();
            Telemetry.DownloadDuration.Record(sw.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("status", result.Success ? "completed" : "failed"));
            return MapResult(result, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new FfmpegResult(false, -1, "Cancelled", (int)sw.Elapsed.TotalSeconds);
        }
    }

    internal static IFfmpegSession BuildSession(FfmpegInput input)
    {
        var builder = FFmpeg.Rx.Ffmpeg
            .Input(new Uri(input.VideoUrl), opts =>
            {
                opts.UserAgent(UserAgent);
                if (input.ProxyUrl is not null)
                {
                    opts.HttpProxy(input.ProxyUrl);
                }
            })
            .WithProgressReporting();

        if (input.SubtitlePath is not null)
        {
            builder = builder.Input(input.SubtitlePath);
        }

        return builder.Output(input.OutputPath, output =>
        {
            output
                .Stream(0, 0, StreamType.Video, s => s.Copy())
                .Stream(0, 0, StreamType.Audio, s =>
                {
                    s.Copy();
                    if (input.IsHls)
                    {
                        s.BitstreamFilter("aac_adtstoasc=no_validation=1");
                    }
                });

            if (input.SubtitlePath is not null)
            {
                output.Stream(1, StreamType.Subtitle, s => s
                    .Encode(SubtitleCodec.Srt)
                    .Metadata("language", input.SubtitleLanguage ?? "deu")
                    .Disposition("0"));
            }
        });
    }

    private static FfmpegResult MapResult(FFmpeg.Rx.Models.FfmpegResult result, TimeSpan elapsed)
    {
        var failureKind = result.FailureKind switch
        {
            FFmpeg.Rx.FailureKind.Transient => Messages.Download.FailureKind.Transient,
            _ => Messages.Download.FailureKind.Permanent
        };

        return new FfmpegResult(
            result.Success,
            result.ExitCode,
            result.Error,
            (int)elapsed.TotalSeconds,
            failureKind);
    }
}
