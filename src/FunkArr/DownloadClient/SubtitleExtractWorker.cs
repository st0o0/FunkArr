using System.Net;
using Akka.Actor;
using Akka.Event;
using FunkArr.Subtitle;

namespace FunkArr.DownloadClient;

internal sealed class SubtitleExtractWorker : ReceiveActor
{
    private sealed record DoWork;

    private readonly SubtitleAcquisitionService _service;
    private readonly string _nzoId;
    private readonly string _manifestUrl;
    private readonly string _tempPath;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public SubtitleExtractWorker(SubtitleAcquisitionService service, string nzoId, string manifestUrl, string tempPath)
    {
        _service = service;
        _nzoId = nzoId;
        _manifestUrl = manifestUrl;
        _tempPath = tempPath;

        Self.Tell(new DoWork());

        ReceiveAsync<DoWork>(async _ =>
        {
            try
            {
                var path = await _service.AcquireAsync(null, manifestUrl, tempPath, nzoId, CancellationToken.None);

                if (path is not null)
                {
                    Context.Parent.Tell(new SubtitleAcquireDone(nzoId, path));
                }
                else
                {
                    Context.Parent.Tell(new NoSubtitleAvailable(nzoId));
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "SubtitleExtractWorker failed for {NzoId}", nzoId);
                Context.Parent.Tell(new WorkerFailed(nzoId, ClassifyException(ex), ex.Message));
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
