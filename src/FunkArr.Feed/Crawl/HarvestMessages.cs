using FunkArr.Core.Configuration;

namespace FunkArr.Feed.Crawl;

internal sealed record StartHarvest(long Cursor, FeedOptions Options);

internal abstract record HarvestResponse;

internal sealed record HarvestCompleted(IngestSummary Summary) : HarvestResponse;

internal sealed record HarvestUnchanged : HarvestResponse;

internal sealed record HarvestFailed(string Reason) : HarvestResponse;
