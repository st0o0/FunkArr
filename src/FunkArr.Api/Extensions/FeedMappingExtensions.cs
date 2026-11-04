using FunkArr.Api.Models.Feed;
using FunkArr.Messages.Feed;

namespace FunkArr.Api.Extensions;

internal static class FeedMappingExtensions
{
    internal static FeedOverviewResponse ToApi(this FeedResult entries, FeedCrawlStatusResult status) =>
        new(
            [
                .. entries.Entries.Select(e => new FeedEntryOutput(
                    e.StableId,
                    e.FirstSeenAt,
                    e.MediaType.ToApi(),
                    e.Title,
                    e.Channel,
                    e.Topic,
                    e.Quality,
                    e.Season,
                    e.Episode))
            ],
            entries.Total,
            entries.FilteredTotal,
            new FeedCrawlStatusOutput(
                status.Enabled,
                status.Running,
                status.LastRunAt,
                status.LastOutcome,
                status.LastNewItems,
                status.LastPublished,
                status.LastUnmatched,
                status.LastGap,
                status.ConsecutiveFailures));
}
