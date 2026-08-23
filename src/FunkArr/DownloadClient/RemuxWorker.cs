using Akka.Actor;
using Akka.Event;
using FunkArr.Muxing;

namespace FunkArr.DownloadClient;

internal sealed class RemuxWorker : ReceiveActor
{
    private sealed record DoWork;

    private readonly MuxingService _service;
    private readonly string _nzoId;
    private readonly string _videoPath;
    private readonly string? _subtitlePath;
    private readonly string _outputDir;
    private readonly string _title;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public RemuxWorker(MuxingService service, string nzoId, string videoPath, string? subtitlePath, string outputDir, string title)
    {
        _service = service;
        _nzoId = nzoId;
        _videoPath = videoPath;
        _subtitlePath = subtitlePath;
        _outputDir = outputDir;
        _title = title;

        Self.Tell(new DoWork());

        ReceiveAsync<DoWork>(async _ =>
        {
            try
            {
                var outcome = await _service.MuxAsync(videoPath, subtitlePath, outputDir, title, CancellationToken.None);

                switch (outcome)
                {
                    case MuxOutcome.Success success:
                        Context.Parent.Tell(new RemuxDone(nzoId, success.OutputPath));
                        break;
                    case MuxOutcome.Failure failure:
                        Context.Parent.Tell(new WorkerFailed(nzoId, FailureKind.Malformed, failure.Reason));
                        break;
                    case MuxOutcome.Skipped skipped:
                        Context.Parent.Tell(new WorkerFailed(nzoId, FailureKind.Malformed, skipped.Reason));
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "RemuxWorker failed for {NzoId}", nzoId);
                Context.Parent.Tell(new WorkerFailed(nzoId, FailureKind.Malformed, ex.Message));
            }
            finally
            {
                Context.Stop(Self);
            }
        });
    }
}
