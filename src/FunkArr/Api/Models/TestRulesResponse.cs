using FunkArr.RuleSet;

namespace FunkArr.Api.Models;

public sealed record TestRulesResponse(
    IReadOnlyList<MatchedTrace> Matched,
    IReadOnlyList<FilteredTrace> Filtered,
    IReadOnlyList<UnmatchedTrace> Unmatched,
    int TotalItems);
