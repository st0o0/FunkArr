namespace FunkArr.Search.Matching;

public sealed record MatchContext
{
    public string? ShowName { get; init; }
    public int? Season { get; init; }
    public int? Episode { get; init; }
    public DateTimeOffset? AirDate { get; init; }
    public int? ExpectedDurationSeconds { get; init; }
    public string? ImdbId { get; init; }
}
