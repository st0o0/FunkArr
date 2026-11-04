using FunkArr.Messages.Feed;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api.Extensions;

internal static class FeedMappingExtensions
{
    internal static ApiModels.FeedOverviewResponse ToApi(this FeedResult entries, FeedCrawlStatusResult status) =>
        new(
            [
                .. entries.Entries.Select(e => new ApiModels.FeedEntryOutput(
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
            new ApiModels.FeedCrawlStatusOutput(
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
