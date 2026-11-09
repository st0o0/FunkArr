using System.Diagnostics;
using FFmpeg.Rx;
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
        var spec = BuildPipeline(input);
        return FfmpegSession.Create(spec);
    }

    internal static PipelineConfig BuildPipeline(FfmpegInput input)
    {
        var inputs = BuildInputs(input);
        var output = BuildOutput(input);
        return new PipelineConfig(inputs, [output], ["-progress pipe:1"]);
    }

    internal static InputConfig[] BuildInputs(FfmpegInput input)
    {
        var customArgs = new List<string> { $"-user_agent \"{UserAgent}\"" };
        if (input.ProxyUrl is not null)
        {
            customArgs.Add($"-http_proxy {input.ProxyUrl}");
        }

        var urlInput = new UrlInputConfig(new Uri(input.VideoUrl))
        {
            Options = new InputOptionConfig(CustomArgs: customArgs)
        };

        if (input.SubtitlePath is not null)
        {
            return [urlInput, new FileInputConfig(input.SubtitlePath)];
        }

        return [urlInput];
    }

    internal static OutputConfig BuildOutput(FfmpegInput input)
    {
        var streams = new List<StreamConfig>
        {
            new(0, 0, StreamType.Video, new StreamCopy()),
            new(0, 0, StreamType.Audio, new StreamCopy())
        };

        var customArgs = new List<string>();

        if (input.IsHls)
        {
            customArgs.Add("-bsf:a aac_adtstoasc=no_validation=1");
        }

        if (input.SubtitlePath is not null)
        {
            streams.Add(new StreamConfig(
                1, null, StreamType.Subtitle,
                new SubtitleEncode(SubtitleCodec.Srt),
                [new("language", input.SubtitleLanguage ?? "deu")],
                "0"));
        }

        return new OutputConfig(
            new FileDestination(input.OutputPath),
            streams,
            CustomArgs: customArgs.Count > 0 ? customArgs : null);
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
