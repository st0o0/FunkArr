using System.Net;
using Akka.Actor;
using Akka.Event;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Shared;

namespace FunkArr.DownloadClient.Pipeline;

internal sealed class Mp4DownloadActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public Mp4DownloadActor(IHttpClientFactory httpClientFactory, IFileService fileService)
    {
        ReceiveAsync<FetchVideo>(async cmd =>
        {
            try
            {
                var client = httpClientFactory.CreateClient();

                using var response = await client.GetAsync(
                    cmd.Url, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);
                response.EnsureSuccessStatusCode();

                await using var contentStream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
                await fileService.SaveVideoAsync(cmd.NzoId, contentStream);

                Context.Parent.Tell(new VideoFetched(cmd.NzoId));
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Mp4DownloadActor failed for {NzoId}", cmd.NzoId);
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
