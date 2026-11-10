using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Messages.Search;

public sealed record EvaluateSeries(
    Guid SearchId,
    SearchSource Source,
    MediathekItem[] Items,
    int? TvdbId = null,
    string? ImdbId = null,
    int? Season = null,
    string? RuleSetId = null,
    string? MediaName = null,
    EnrichmentConfig? Enrichment = null) : IWithSearchId;

public abstract record EvaluateSeriesResponse;

public sealed record EvaluateSeriesCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : EvaluateSeriesResponse;

public sealed record EvaluateSeriesFailed(
    Guid SearchId,
    Exception Cause) : EvaluateSeriesResponse;
