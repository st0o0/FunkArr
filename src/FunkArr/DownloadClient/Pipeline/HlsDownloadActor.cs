using System.Net;
using Akka.Actor;
using Akka.Event;
using FunkArr.DownloadClient.Ffmpeg;
using FunkArr.DownloadClient.Tracker;

namespace FunkArr.DownloadClient.Pipeline;

internal sealed class HlsDownloadActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public HlsDownloadActor(IFfmpegService ffmpegService)
    {
        ReceiveAsync<FetchVideo>(async cmd =>
        {
            try
            {
                await ffmpegService.DownloadHlsAsync(cmd.NzoId, cmd.Url);
                Context.Parent.Tell(new VideoFetched(cmd.NzoId));
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "HlsDownloadActor failed for {NzoId}", cmd.NzoId);
                Context.Parent.Tell(new WorkerFailed(cmd.NzoId, ClassifyException(ex), ex.Message));
            }
            finally
            {
                Context.Stop(Self);
            }
        });
    }

    private static FailureKind ClassifyException(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.NotFound or HttpStatusCode.Gone } => FailureKind.Gone,
        HttpRequestException => FailureKind.Transient,
        IOException => FailureKind.LocalIo,
        _ => FailureKind.Transient,
    };
}
