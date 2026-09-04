using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal sealed class TvdbResolverActor : ReceiveActor
{
    private readonly TvdbClient _tvdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TvdbResolverActor(TvdbClient tvdbClient)
    {
        _tvdbClient = tvdbClient;

        ReceiveAsync<FetchAndResolveEpisodes>(Handle);
    }

    private async Task Handle(FetchAndResolveEpisodes msg)
    {
        try
        {
            _log.Info("Fetching TVDB episodes for series {TvdbId}", msg.TvdbId);
            var episodes = await _tvdbClient.GetEpisodesAsync(msg.TvdbId, null);

            Context.Parent.Tell(new CacheUpdate("tvdb", msg.TvdbId, episodes));

            var filtered = msg.Season is not null
                ? episodes.Where(e => e.SeasonNumber == msg.Season.Value).ToArray()
                : episodes;

            var resolved = EpisodeResolver.Resolve(filtered, msg.Candidates, msg.Config);
            Sender.Tell(new EpisodesResolved(resolved));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "TVDB fetch failed for series {TvdbId}", msg.TvdbId);
            Sender.Tell(new EpisodeResolutionFailed(ex.Message));
        }
    }
}
