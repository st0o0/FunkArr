using Akka.Actor;
using Akka.Event;

namespace FunkArr.Search;

internal sealed class MovieSearchActor : ReceiveActor
{
    private readonly MediathekClient _mediathekClient;
    private readonly QualityProbeService _qualityProbeService;
    private readonly int _probeLimit;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public MovieSearchActor(
        MediathekClient mediathekClient,
        QualityProbeService qualityProbeService,
        int probeLimit)
    {
        _mediathekClient = mediathekClient;
        _qualityProbeService = qualityProbeService;
        _probeLimit = probeLimit;

        ReceiveAsync<ExecuteMovieSearch>(HandleAsync);
    }

    private async Task HandleAsync(ExecuteMovieSearch command)
    {
        var request = command.Request;
        var context = new MatchContext
        {
            ShowName = request.Query,
            ImdbId = request.ImdbId,
        };

        var results = await SearchChildHelpers.SearchMediathekAsync(_mediathekClient, _log, command.SearchTerm);
        var filtered = await MatchingPipeline.ExecuteAsync(results, context, _qualityProbeService, _probeLimit);

        var matchRecord = SearchChildHelpers.BuildGenericPipelineRecord(
            command.SearchTerm, null, null, null, results.Length);

        _log.Debug(
            "Generic pipeline result for '{Topic}': {Matched}/{Total} results",
            command.SearchTerm, filtered.Count, results.Length);

        Sender.Tell(new SearchCompleted(command.CacheKey, filtered, matchRecord, command.ReplyTo));
    }
}
