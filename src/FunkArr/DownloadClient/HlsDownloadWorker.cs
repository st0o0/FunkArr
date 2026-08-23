using System.Net;
using Akka.Actor;
using Akka.Event;

namespace FunkArr.DownloadClient;

internal sealed class HlsDownloadWorker : ReceiveActor
{
    private sealed record DoWork;

    private readonly HlsDownloadService _service;
    private readonly string _nzoId;
    private readonly string _manifestUrl;
    private readonly string _tempPath;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public HlsDownloadWorker(HlsDownloadService service, string nzoId, string manifestUrl, string tempPath)
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
                var request = new DownloadRequest(nzoId, manifestUrl, null, tempPath, "", "", new Progress<DownloadProgress>());
                var result = await _service.DownloadAsync(request, 0, new Progress<DownloadProgress>(), CancellationToken.None);
                Context.Parent.Tell(new VideoFetchDone(nzoId, result.VideoPath));
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "HlsDownloadWorker failed for {NzoId}", nzoId);
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
