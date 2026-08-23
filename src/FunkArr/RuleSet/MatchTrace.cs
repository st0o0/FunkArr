namespace FunkArr.RuleSet;

public abstract record MatchTrace
{
    public required string ItemTitle { get; init; }
    public required string ItemTopic { get; init; }
    public required int ItemDuration { get; init; }
    public required string ItemChannel { get; init; }
}

public sealed record MatchedTrace : MatchTrace
{
    public required int RuleIndex { get; init; }
    public required MatchingStrategy Strategy { get; init; }
    public required double Confidence { get; init; }
    public required int Season { get; init; }
    public required int Episode { get; init; }
    public required string EpisodeName { get; init; }
}

public sealed record FilteredTrace : MatchTrace
{
    public required string FilterField { get; init; }
    public required string FilterOp { get; init; }
    public required string FilterValue { get; init; }
    public required string ActualValue { get; init; }
    public required string Reason { get; init; }
}

public sealed record UnmatchedTrace : MatchTrace
{
    public required IReadOnlyList<RuleFailure> RuleFailures { get; init; }
}

public sealed record RuleFailure
{
    public required int RuleIndex { get; init; }
    public required string FailReason { get; init; }
    public string? Detail { get; init; }
}

public sealed record MatchRecord
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string SearchTopic { get; init; }
    public required int? TvdbId { get; init; }
    public required int? Season { get; init; }
    public required int? Episode { get; init; }
    public required string Source { get; init; }
    public required int TotalResults { get; init; }
    public required IReadOnlyList<MatchedTrace> Matched { get; init; }
    public required IReadOnlyList<FilteredTrace> Filtered { get; init; }
    public required IReadOnlyList<UnmatchedTrace> Unmatched { get; init; }
}

public sealed record TopicStats
{
    public required string Topic { get; init; }
    public required int SearchCount { get; init; }
    public required int TotalItemsEvaluated { get; init; }
    public required int MatchedCount { get; init; }
    public required int FilteredCount { get; init; }
    public required int UnmatchedCount { get; init; }
    public required double MatchRate { get; init; }
    public required Dictionary<string, int> PerRuleHitCounts { get; init; }
}
