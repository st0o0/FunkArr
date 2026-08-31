namespace FunkArr.Messages.Scoring;

public sealed record MatchingRule(
    string Id,
    int Priority,
    float? Confidence,
    FilterSpec? Filters,
    IdentificationSpec Identification);
