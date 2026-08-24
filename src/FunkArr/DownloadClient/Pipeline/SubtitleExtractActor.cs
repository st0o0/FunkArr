using Akka.Actor;
using Akka.Event;
using FunkArr.DownloadClient.Ffmpeg;

namespace FunkArr.DownloadClient.Pipeline;

internal sealed class SubtitleExtractActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public SubtitleExtractActor(IFfmpegService ffmpegService)
    {
        ReceiveAsync<AcquireSubtitle>(async cmd =>
        {
            try
            {
                var found = await ffmpegService.ExtractSubtitleAsync(cmd.NzoId, cmd.HlsManifestUrl!);
                Context.Parent.Tell(new SubtitleAcquired(cmd.NzoId, found));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.Warning(ex, "SubtitleExtractActor failed for {NzoId}", cmd.NzoId);
                Context.Parent.Tell(new SubtitleAcquired(cmd.NzoId, false));
            }
            finally
            {
                Context.Stop(Self);
            }
        });
    }
}
