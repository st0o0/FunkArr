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

        ReceiveAsync<ResolveMovie>(Handle);
    }

    private async Task Handle(ResolveMovie msg)
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
                Sender.Tell(new MovieResolutionFailed("Movie not found"));
                return;
            }

            var resolved = _movieResolver.Resolve(data.Movie, data.AltTitles, msg.Candidates);
            Sender.Tell(new MoviesResolved(resolved));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "TMDB resolution failed");
            Sender.Tell(new MovieResolutionFailed(ex));
        }
    }
}
