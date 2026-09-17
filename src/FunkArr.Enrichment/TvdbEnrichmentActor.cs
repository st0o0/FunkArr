using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.Enrichment;

namespace FunkArr.Enrichment;

internal sealed class TvdbEnrichmentActor : ReceiveActor
{
    private readonly TvdbClient _tvdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TvdbEnrichmentActor(TvdbClient tvdbClient)
    {
        _tvdbClient = tvdbClient;

        ReceiveAsync<EnrichEpisodes>(Handle);
    }

    private async Task Handle(EnrichEpisodes msg)
    {
        try
        {
            var episodes = await _tvdbClient.GetEpisodesAsync(msg.TvdbId);

            var filtered = msg.Season is not null
                ? episodes.Where(e => e.SeasonNumber == msg.Season.Value).ToArray()
                : episodes;

            var enriched = EpisodeEnricher.Resolve(filtered, msg.Candidates);
            Sender.Tell(new EnrichEpisodesCompleted(enriched));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "TVDB enrichment failed for series {TvdbId}", msg.TvdbId);
            Sender.Tell(new EnrichEpisodesFailed(ex));
        }
    }
}
