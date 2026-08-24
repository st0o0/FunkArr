using Akka.Actor;
using Akka.Event;
using FunkArr.Shared;

namespace FunkArr.DownloadClient.Pipeline;

internal sealed class SubtitleDownloadActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public SubtitleDownloadActor(IHttpClientFactory httpClientFactory, IFileService fileService)
    {
        ReceiveAsync<AcquireSubtitle>(async cmd =>
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                var response = await client.GetAsync(cmd.SubtitleUrl!, CancellationToken.None);

                if (!response.IsSuccessStatusCode)
                {
                    _log.Warning("Failed to download subtitle for {NzoId}: {Status}", cmd.NzoId, response.StatusCode);
                    Context.Parent.Tell(new SubtitleAcquired(cmd.NzoId, false));
                    return;
                }

                var extension = Path.GetExtension(new Uri(cmd.SubtitleUrl!).AbsolutePath);
                if (string.IsNullOrEmpty(extension))
                {
                    extension = ".sub";
                }

                var content = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
                await fileService.SaveSubtitleAsync(cmd.NzoId, content, extension);

                Context.Parent.Tell(new SubtitleAcquired(cmd.NzoId, true));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.Warning(ex, "Subtitle download failed for {NzoId}", cmd.NzoId);
                Context.Parent.Tell(new SubtitleAcquired(cmd.NzoId, false));
            }
            finally
            {
                Context.Stop(Self);
            }
        });
    }
}
