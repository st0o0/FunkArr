using System.Diagnostics;
using Akka.Actor;

namespace FunkArr.Download;

internal sealed record ProgressUpdate(long TotalSize, long OutTimeUs, double Speed);
internal sealed record ProcessExited(int ExitCode, string? ErrorOutput, int ElapsedSeconds);

internal static class FfmpegRunner
{
    public static CancellationTokenSource Run(
        IActorRef self, string videoUrl, string? subtitleUrl, string outputPath)
    {
        var cts = new CancellationTokenSource();
        var args = FfmpegArgumentBuilder.Build(videoUrl, subtitleUrl, outputPath);

        FfmpegProcess process;
        try
        {
            process = FfmpegProcess.Start(args);
        }
        catch (Exception ex)
        {
            self.Tell(new ProcessExited(-1, "Failed to start FFmpeg: " + ex.Message, 0));
            return cts;
        }

        var ct = cts.Token;
        var sw = Stopwatch.StartNew();

        Task.Run(async () =>
        {
            var block = new Dictionary<string, string>();

            try
            {
                while (await process.StandardOutput.ReadLineAsync(ct) is { } line)
                {
                    ct.ThrowIfCancellationRequested();
                    FfmpegProgressParser.AccumulateLine(block, line);

                    if (FfmpegProgressParser.IsBlockComplete(block))
                    {
                        var progress = FfmpegProgressParser.Parse(block);
                        if (progress is not null)
                        {
                            self.Tell(progress);
                        }
                        block = [];
                    }
                }

                var exitCode = await process.WaitForExitAsync(ct);
                sw.Stop();
                var stderr = exitCode != 0 ? process.GetStderrOutput() : null;
                return new ProcessExited(exitCode, stderr, (int)sw.Elapsed.TotalSeconds);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                sw.Stop();
                return new ProcessExited(-1, "Cancelled", (int)sw.Elapsed.TotalSeconds);
            }
            finally
            {
                process.Dispose();
            }
        }, ct).PipeTo(self);

        return cts;
    }
}
