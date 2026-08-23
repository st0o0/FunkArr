using Akka.Actor;
using Akka.Event;
using FunkArr.Subtitle;

namespace FunkArr.DownloadClient;

internal sealed class SubtitleConvertWorker : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private sealed record DoWork;

    public SubtitleConvertWorker(SubtitleNormalizerService normalizer, string nzoId, string subtitlePath, string tempPath)
    {
        ReceiveAsync<DoWork>(async _ =>
        {
            try
            {
                var outputPath = Path.Combine(tempPath, $"{nzoId}.srt");
                var normalizedPath = await normalizer.NormalizeAsync(subtitlePath, outputPath);

                if (normalizedPath is not null)
                {
                    Context.Parent.Tell(new SubtitleConvertDone(nzoId, normalizedPath));
                }
                else
                {
                    Context.Parent.Tell(new WorkerFailed(nzoId, FailureKind.Malformed, "Subtitle normalization returned null"));
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "SubtitleConvertWorker failed for {NzoId}", nzoId);
                Context.Parent.Tell(new WorkerFailed(nzoId, FailureKind.Malformed, ex.Message));
            }
            finally
            {
                Context.Stop(Self);
            }
        });

        Self.Tell(new DoWork());
    }
}
