namespace FunkArr.Messages.Search;

public sealed record SearchSeries(
    Guid SearchId,
    string Source,
    string? Query,
    int? Season,
    int? Episode,
    int? TvdbId,
    string? ImdbId,
    int? Limit,
    int? Offset) : SearchRequest(SearchId, Source, Query, Limit, Offset);

public abstract record SearchSeriesResponse;

public sealed record SearchSeriesCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : SearchSeriesResponse;

public sealed record SearchSeriesFailed(
    Guid SearchId,
    Exception Cause) : SearchSeriesResponse;
