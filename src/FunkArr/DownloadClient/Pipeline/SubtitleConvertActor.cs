using Akka.Actor;
using Akka.Event;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Shared;

namespace FunkArr.DownloadClient.Pipeline;

internal sealed class SubtitleConvertActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public SubtitleConvertActor(IFileService fileService)
    {
        ReceiveAsync<ConvertSubtitle>(async cmd =>
        {
            try
            {
                var normalizedPath = await fileService.NormalizeSubtitleAsync(cmd.NzoId);

                if (normalizedPath is not null)
                {
                    Context.Parent.Tell(new SubtitleConverted(cmd.NzoId));
                }
                else
                {
                    Context.Parent.Tell(new WorkerFailed(cmd.NzoId, FailureKind.Malformed, "Subtitle normalization returned null"));
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "SubtitleConvertActor failed for {NzoId}", cmd.NzoId);
                Context.Parent.Tell(new WorkerFailed(cmd.NzoId, FailureKind.Malformed, ex.Message));
            }
            finally
            {
                Context.Stop(Self);
            }
        });
    }
}
