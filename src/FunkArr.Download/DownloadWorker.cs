using System.Diagnostics;
using Akka.Actor;
using Akka.Cluster.Sharding;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Servus.Akka;

namespace FunkArr.Download;

public sealed class DownloadWorker : ReceiveActor
{
    private sealed record FfmpegCompleted(int ExitCode, string? ErrorOutput, int ElapsedSeconds);
    private sealed record FfmpegProgressTick(FfmpegProgress Progress);
    private sealed record SubtitleRetry;

    private DownloadWorkerState _state = DownloadWorkerState.Empty;
    private FfmpegProcess? _ffmpeg;
    private readonly IActorRef _downloadManager = Context.GetActor<IDownloadManager>();

    public DownloadWorker()
    {
        Receive<StartDownload>(HandleStart);
        Receive<FfmpegProgressTick>(HandleProgress);
        Receive<FfmpegCompleted>(HandleCompleted);
        Receive<SubtitleRetry>(HandleSubtitleRetry);
    }

    private void HandleStart(StartDownload cmd)
    {
        _state = _state.Apply(cmd);
        StartFfmpeg(cmd.VideoUrl, cmd.SubtitleUrl, cmd.OutputPath);
    }

    private void HandleProgress(FfmpegProgressTick tick)
    {
        if (_state.Command is null)
        {
            return;
        }

        var cmd = _state.Command;
        var progress = new DownloadProgress(
            cmd.DownloadId,
            tick.Progress.OutTimeUs,
            cmd.Duration,
            tick.Progress.TotalSize,
            cmd.Size,
            tick.Progress.Speed);

        _downloadManager.Tell(progress);
    }

    private void HandleCompleted(FfmpegCompleted msg)
    {
        if (_state.Command is null)
        {
            return;
        }

        var cmd = _state.Command;

        if (msg.ExitCode == 0)
        {
            _state = _state.WithStatus(DownloadStatus.Completed);
            _downloadManager.Tell(new DownloadCompleted(cmd.DownloadId, cmd.OutputPath, msg.ElapsedSeconds));
            Passivate();
        }
        else if (cmd.SubtitleUrl is not null && IsSubtitleError(msg.ErrorOutput))
        {
            Self.Tell(new SubtitleRetry());
        }
        else
        {
            _state = _state.WithStatus(DownloadStatus.Failed);
            var reason = TruncateError(msg.ErrorOutput ?? "FFmpeg exited with code " + msg.ExitCode);
            _downloadManager.Tell(new DownloadFailed(cmd.DownloadId, reason));
            Passivate();
        }
    }

    private void HandleSubtitleRetry(SubtitleRetry _)
    {
        if (_state.Command is null)
        {
            return;
        }

        var cmd = _state.Command;
        _state = _state.Apply(cmd with { SubtitleUrl = null });
        StartFfmpeg(cmd.VideoUrl, null, cmd.OutputPath);
    }

    private void StartFfmpeg(string videoUrl, string? subtitleUrl, string outputPath)
    {
        var args = FfmpegArgumentBuilder.Build(videoUrl, subtitleUrl, outputPath);
        var self = Self;

        try
        {
            _ffmpeg = FfmpegProcess.Start(args);
            _state = _state.WithProcessId(_ffmpeg.ProcessId);
        }
        catch (Exception ex)
        {
            if (_state.Command is not { } cmd)
            {
                return;
            }

            _state = _state.WithStatus(DownloadStatus.Failed);
            _downloadManager.Tell(new DownloadFailed(cmd.DownloadId, "Failed to start FFmpeg: " + ex.Message));
            Passivate();
            return;
        }

        var process = _ffmpeg;
        var sw = Stopwatch.StartNew();

        Task.Run(async () =>
        {
            var block = new Dictionary<string, string>();
            var lastProgressAt = DateTimeOffset.MinValue;

            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                FfmpegProgressParser.AccumulateLine(block, line);

                if (FfmpegProgressParser.IsBlockComplete(block))
                {
                    var progress = FfmpegProgressParser.Parse(block);
                    if (progress is not null)
                    {
                        var now = DateTimeOffset.UtcNow;
                        if ((now - lastProgressAt).TotalSeconds >= 1.0 || progress.IsEnd)
                        {
                            self.Tell(new FfmpegProgressTick(progress));
                            lastProgressAt = now;
                        }
                    }
                    block = [];
                }
            }

            var exitCode = await process.WaitForExitAsync();
            sw.Stop();
            var stderr = exitCode != 0 ? process.GetStderrOutput() : null;
            return new FfmpegCompleted(exitCode, stderr, (int)sw.Elapsed.TotalSeconds);
        }).PipeTo(self);
    }

    private void Passivate()
    {
        _ffmpeg?.Dispose();
        _ffmpeg = null;
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }

    protected override void PostStop()
    {
        _ffmpeg?.Dispose();
        _ffmpeg = null;
        base.PostStop();
    }

    private static bool IsSubtitleError(string? stderr) =>
        stderr is not null && (
            stderr.Contains("subtitle", StringComparison.OrdinalIgnoreCase) ||
            stderr.Contains("Stream map", StringComparison.OrdinalIgnoreCase));

    private static string TruncateError(string error) =>
        error.Length > 500 ? error[^500..] : error;
}
