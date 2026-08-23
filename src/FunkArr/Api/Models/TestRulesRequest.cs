using FunkArr.RuleSet;

namespace FunkArr.Api.Models;

public sealed record TestRulesRequest
{
    public string Topic { get; init; } = string.Empty;
    public int? TvdbId { get; init; }
    public IReadOnlyList<Rule>? Rules { get; init; }
}
