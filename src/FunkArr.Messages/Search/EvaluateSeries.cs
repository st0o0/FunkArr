using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;

namespace FunkArr.Messages.Search;

public sealed record EvaluateSeries(
    Guid SearchId,
    SearchSource Source,
    MediathekItem[] Items,
    int? TvdbId = null,
    string? ImdbId = null,
    int? Season = null,
    RuleSetResolved? RuleSet = null) : IWithSearchId;

public abstract record EvaluateSeriesResponse;

public sealed record EvaluateSeriesCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : EvaluateSeriesResponse;

public sealed record EvaluateSeriesFailed(
    Guid SearchId,
    Exception Cause) : EvaluateSeriesResponse;
