using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal sealed class TmdbResolverActor : ReceiveActor
{
    private readonly TmdbClient _tmdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TmdbResolverActor(TmdbClient tmdbClient)
    {
        _tmdbClient = tmdbClient;

        ReceiveAsync<FetchAndResolveMovie>(Handle);
    }

    private async Task Handle(FetchAndResolveMovie msg)
    {
        try
        {
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

            Context.Parent.Tell(new CacheUpdate("tmdb", movie.Id, movie));

            var resolved = MovieResolver.Resolve(movie, altTitles, msg.Candidates);
            Sender.Tell(new MoviesResolved(resolved));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "TMDB resolution failed");
            Sender.Tell(new MovieResolutionFailed(ex.Message));
        }
    }
}
