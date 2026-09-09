using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal sealed class TmdbResolverActor : ReceiveActor
{
    private readonly TmdbClient _tmdbClient;
    private readonly MovieResolver _movieResolver;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TmdbResolverActor(TmdbClient tmdbClient, MovieResolver movieResolver)
    {
        _tmdbClient = tmdbClient;
        _movieResolver = movieResolver;

        ReceiveAsync<FetchAndResolveMovie>(Handle);
    }

    private async Task Handle(FetchAndResolveMovie msg)
    {
        try
        {
            _log.Info("Fetching TMDB movie TmdbId={TmdbId} ImdbId={ImdbId}", msg.TmdbId, msg.ImdbId);
            TmdbMovie? movie = null;

            if (msg.TmdbId is not null)
            {
                movie = await _tmdbClient.GetMovieAsync(msg.TmdbId.Value);
            }
            else if (msg.ImdbId is not null)
            {
                movie = await _tmdbClient.FindByImdbIdAsync(msg.ImdbId);
            }

            if (movie is null)
            {
                Sender.Tell(new MovieResolutionFailed("Movie not found"));
                return;
            }

            var altTitles = await _tmdbClient.GetAlternativeTitlesAsync(movie.Id);
            var resolved = _movieResolver.Resolve(movie, altTitles, msg.Candidates);

            Context.Parent.Tell(new MovieCacheUpdate(movie.Id, resolved));

            Sender.Tell(new MoviesResolved(resolved));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "TMDB resolution failed");
            Sender.Tell(new MovieResolutionFailed(ex.Message));
        }
    }
}
