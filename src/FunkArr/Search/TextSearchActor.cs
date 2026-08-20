using Akka.Actor;
using Akka.Event;

namespace FunkArr.Search;

internal sealed class TextSearchActor : ReceiveActor
{
    private readonly MediathekClient _mediathekClient;
    private readonly QualityProbeService _qualityProbeService;
    private readonly int _probeLimit;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TextSearchActor(
        MediathekClient mediathekClient,
        QualityProbeService qualityProbeService,
        int probeLimit)
    {
        _mediathekClient = mediathekClient;
        _qualityProbeService = qualityProbeService;
        _probeLimit = probeLimit;

        ReceiveAsync<ExecuteTextSearch>(HandleAsync);
    }

    private async Task HandleAsync(ExecuteTextSearch command)
    {
        var request = command.Request;
        var context = new MatchContext();

        var results = await SearchChildHelpers.SearchMediathekAsync(_mediathekClient, _log, request.Query);
        var filtered = await MatchingPipeline.ExecuteAsync(results, context, _qualityProbeService, _probeLimit);

        var matchRecord = SearchChildHelpers.BuildGenericPipelineRecord(
            request.Query, null, null, null, results.Length);

        _log.Debug(
            "Generic pipeline result for '{Topic}': {Matched}/{Total} results",
            request.Query, filtered.Count, results.Length);

        Sender.Tell(new SearchCompleted(command.CacheKey, filtered, matchRecord, command.ReplyTo));
    }
}
