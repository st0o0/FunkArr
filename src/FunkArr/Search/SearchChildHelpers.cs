using Akka.Event;
using FunkArr.RuleSet;

namespace FunkArr.Search;

internal static class SearchChildHelpers
{
    public static async Task<MediathekResultItem[]> SearchMediathekAsync(
        MediathekClient mediathekClient, ILoggingAdapter log, string searchTerm)
    {
        try
        {
            var query = new MediathekQuery
            {
                Queries =
                [
                    new MediathekQueryItem { Fields = ["topic", "title"], Query = searchTerm },
                ],
            };

            var response = await mediathekClient.QueryAsync(query);
            return response?.Result ?? [];
        }
        catch (Exception ex)
        {
            log.Warning(ex, "MediathekViewWeb query failed for '{SearchTerm}'", searchTerm);
            return [];
        }
    }

    public static MatchRecord BuildGenericPipelineRecord(
        string searchTopic, int? tvdbId, int? season, int? episode, int totalResults)
    {
        return new MatchRecord
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            Timestamp = DateTimeOffset.UtcNow,
            SearchTopic = searchTopic,
            TvdbId = tvdbId,
            Season = season,
            Episode = episode,
            Source = "generic-pipeline",
            TotalResults = totalResults,
            Matched = [],
            Filtered = [],
            Unmatched = [],
        };
    }
}
