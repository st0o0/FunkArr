using System.Diagnostics;
using System.Diagnostics.Metrics;
using Akka.Actor;
using Akka.Event;
using FunkArr.Diagnostics;
using FunkArr.DownloadClient.Ffmpeg;
using FunkArr.DownloadClient.Tracker;

namespace FunkArr.DownloadClient.Pipeline;

internal sealed class RemuxActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Histogram<double> _muxDuration = FunkArrMetrics.Instance.AddMuxDuration();

    public RemuxActor(IFfmpegService ffmpegService)
    {
        ReceiveAsync<RemuxVideo>(async cmd =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await ffmpegService.RemuxAsync(cmd.NzoId, cmd.Title, cmd.HasSubtitle, cmd.Category);
                _muxDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("outcome", "success"));
                Context.Parent.Tell(new VideoRemuxed(cmd.NzoId));
            }
            catch (Exception ex)
            {
                _muxDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("outcome", "error"));
                _log.Warning(ex, "RemuxActor failed for {NzoId}", cmd.NzoId);
                Context.Parent.Tell(new WorkerFailed(cmd.NzoId, FailureKind.Malformed, ex.Message));
            }
            finally
            {
                Context.Stop(Self);
            }
        });
    }
}
