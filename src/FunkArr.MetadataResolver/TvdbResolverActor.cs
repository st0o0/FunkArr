using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal sealed class TvdbResolverActor : ReceiveActor
{
    private readonly TvdbClient _tvdbClient;
    private readonly EpisodeResolver _episodeResolver;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public TvdbResolverActor(TvdbClient tvdbClient, EpisodeResolver episodeResolver)
    {
        _tvdbClient = tvdbClient;
        _episodeResolver = episodeResolver;

        ReceiveAsync<ResolveEpisodes>(Handle);
    }

    private async Task Handle(ResolveEpisodes msg)
    {
        if (msg.Config.Strategy == "none")
        {
            Sender.Tell(new EpisodesResolved([]));
            return;
        }

        try
        {
            var episodes = await _tvdbClient.GetEpisodesAsync(msg.TvdbId);

            var filtered = msg.Season is not null
                ? episodes.Where(e => e.SeasonNumber == msg.Season.Value).ToArray()
                : episodes;

            var resolved = _episodeResolver.Resolve(filtered, msg.Candidates, msg.Config);
            Sender.Tell(new EpisodesResolved(resolved));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "TVDB resolution failed for series {TvdbId}", msg.TvdbId);
            Sender.Tell(new EpisodeResolutionFailed(ex));
        }
    }
}
