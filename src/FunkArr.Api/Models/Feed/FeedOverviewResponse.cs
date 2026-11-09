using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Models.Feed;

public sealed record FeedOverviewResponse(
    FeedEntryOutput[] Entries,
    int Total,
    int FilteredTotal,
    FeedCrawlStatusOutput Ingest);

public sealed record FeedEntryOutput(
    string StableId,
    DateTimeOffset FirstSeenAt,
    MediaType MediaType,
    string Title,
    string Channel,
    string Topic,
    int Quality,
    string? Season,
    string? Episode);

public sealed record FeedCrawlStatusOutput(
    bool Enabled,
    bool Running,
    DateTimeOffset? LastRunAt,
    CrawlOutcome LastOutcome,
    string? LastError,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures);
