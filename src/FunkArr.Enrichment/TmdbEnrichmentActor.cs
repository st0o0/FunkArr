using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.Enrichment;

namespace FunkArr.Enrichment;

internal sealed class TmdbEnrichmentActor : ReceiveActor
{
    private readonly TmdbClient _tmdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TmdbEnrichmentActor(TmdbClient tmdbClient)
    {
        _tmdbClient = tmdbClient;

        ReceiveAsync<EnrichMovies>(Handle);
    }

    private async Task Handle(EnrichMovies msg)
    {
        try
        {
            TmdbMovieData? data = null;

            if (msg.TmdbId is not null)
            {
                data = await _tmdbClient.GetMovieDataAsync(msg.TmdbId.Value);
            }
            else if (msg.ImdbId is not null)
            {
                data = await _tmdbClient.FindByImdbIdAsync(msg.ImdbId);
            }

            if (data is null)
            {
                Sender.Tell(new EnrichMoviesFailed(new MovieNotFoundException()));
                return;
            }

            var enriched = MovieEnricher.Resolve(data.Movie, data.AltTitles, msg.Candidates, msg.Config);
            Telemetry.ItemsEnriched.Add(enriched.Length, new KeyValuePair<string, object?>("api", "tmdb"));
            Sender.Tell(new EnrichMoviesCompleted(enriched));
        }
        catch (Exception ex)
        {
            Telemetry.Failed.Add(1, new KeyValuePair<string, object?>("api", "tmdb"));
            _log.Warning(ex, "TMDB enrichment failed");
            Sender.Tell(new EnrichMoviesFailed(ex));
        }
    }
}
