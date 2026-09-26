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
                ? [.. episodes.Where(e => e.SeasonNumber == msg.Season.Value)]
                : episodes;

            var enriched = EpisodeEnricher.Resolve(filtered, msg.Candidates, msg.Config);

            if (msg.Season is not null && enriched.Length < msg.Candidates.Length)
            {
                var matchedIndices = new HashSet<int>(enriched.Select(e => e.Index));
                var unmatched = msg.Candidates.Where(c => !matchedIndices.Contains(c.Index)).ToArray();

                if (unmatched.Length > 0)
                {
                    var fallback = EpisodeEnricher.Resolve(episodes, unmatched, msg.Config);
                    var penalized = fallback.Select(e => e with { Confidence = e.Confidence * 0.9f }).ToArray();
                    enriched = [.. enriched, .. penalized];
                }
            }

            Telemetry.ItemsEnriched.Add(enriched.Length, new KeyValuePair<string, object?>("api", "tvdb"));
            Sender.Tell(new EnrichEpisodesCompleted(enriched));
        }
        catch (Exception ex)
        {
            Telemetry.Failed.Add(1, new KeyValuePair<string, object?>("api", "tvdb"));
            _log.Warning(ex, "TVDB enrichment failed for series {TvdbId}", msg.TvdbId);
            Sender.Tell(new EnrichEpisodesFailed(ex));
        }
    }
}
