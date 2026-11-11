using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Crawl;

internal sealed record ItemGroup(
    string RuleSetId,
    MediaType MediaType,
    ExternalIds Ids,
    MediathekItem[] Items)
{
    public async Task<ScoredGroup> EvaluateAsync(IActorRef eval, TimeSpan timeout)
    {
        if (MediaType == MediaType.Movie)
        {
            var response = await eval.Ask<EvaluateMoviesResponse>(
                new EvaluateMovies(Guid.NewGuid(), SearchSource.Feed,
                    Items, Ids.ImdbId, Ids.TmdbId),
                timeout);

            return response switch
            {
                EvaluateMoviesCompleted c => new ScoredGroup(this, c.Items),
                EvaluateMoviesFailed f => throw f.Cause,
                _ => throw new InvalidOperationException("Unexpected movie evaluation response")
            };
        }
        else
        {
            var response = await eval.Ask<EvaluateSeriesResponse>(
                new EvaluateSeries(Guid.NewGuid(), SearchSource.Feed,
                    Items, Ids.TvdbId, Ids.ImdbId),
                timeout);

            return response switch
            {
                EvaluateSeriesCompleted c => new ScoredGroup(this, c.Items),
                EvaluateSeriesFailed f => throw f.Cause,
                _ => throw new InvalidOperationException("Unexpected series evaluation response")
            };
        }
    }
}
