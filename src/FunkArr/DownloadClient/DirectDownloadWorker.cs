using System.Net;
using Akka.Actor;
using Akka.Event;

namespace FunkArr.DownloadClient;

internal sealed class DirectDownloadWorker : ReceiveActor
{
    private sealed record DoWork;

    private readonly Mp4DownloadService _service;
    private readonly string _nzoId;
    private readonly string _url;
    private readonly string _tempPath;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public DirectDownloadWorker(Mp4DownloadService service, string nzoId, string url, string tempPath)
    {
        _service = service;
        _nzoId = nzoId;
        _url = url;
        _tempPath = tempPath;

        Self.Tell(new DoWork());

        ReceiveAsync<DoWork>(async _ =>
        {
            try
            {
                var request = new DownloadRequest(nzoId, url, null, tempPath, "", "", new Progress<DownloadProgress>());
                var result = await _service.DownloadAsync(request, new Progress<DownloadProgress>(), CancellationToken.None);
                Context.Parent.Tell(new VideoFetchDone(nzoId, result.VideoPath));
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "DirectDownloadWorker failed for {NzoId}", nzoId);
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
