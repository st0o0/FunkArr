using FunkArr.Api.Models.Feed;
using FunkArr.Messages.Feed;

namespace FunkArr.Api.Extensions;

internal static class FeedMappingExtensions
{
    internal static Models.Feed.CrawlOutcome ToApi(this Messages.Feed.CrawlOutcome value) =>
        value switch
        {
            Messages.Feed.CrawlOutcome.Completed => Models.Feed.CrawlOutcome.Completed,
            Messages.Feed.CrawlOutcome.CompletedWithGap => Models.Feed.CrawlOutcome.CompletedWithGap,
            Messages.Feed.CrawlOutcome.Unchanged => Models.Feed.CrawlOutcome.Unchanged,
            Messages.Feed.CrawlOutcome.Failed => Models.Feed.CrawlOutcome.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

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
                status.LastOutcome.ToApi(),
                status.LastError,
                status.LastNewItems,
                status.LastPublished,
                status.LastUnmatched,
                status.LastGap,
                status.ConsecutiveFailures));
}
