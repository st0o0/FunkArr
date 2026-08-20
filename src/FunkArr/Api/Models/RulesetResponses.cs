using FunkArr.RuleSet;

namespace FunkArr.Api.Models;

public sealed record RulesetSummaryResponse(
    string Topic,
    string Source,
    int RuleCount,
    MediaReference? Media,
    IReadOnlyList<string>? Aliases,
    double? MatchRate,
    int SearchCount);
