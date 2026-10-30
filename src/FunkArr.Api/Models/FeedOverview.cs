namespace FunkArr.Api.Models;

public sealed record FeedOverviewResponse(
    FeedEntryOutput[] Entries,
    int Total,
    FeedIngestStatusOutput Ingest);

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

public sealed record FeedIngestStatusOutput(
    bool Enabled,
    bool Running,
    DateTimeOffset? LastRunAt,
    string? LastOutcome,
    int LastNewItems,
    int LastPublished,
    int LastUnmatched,
    bool LastGap,
    int ConsecutiveFailures);
