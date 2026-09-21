namespace FunkArr.Api.Models;

public sealed record MediaInput(
    string Name,
    string Type,
    int? TvdbId = null,
    string? ImdbId = null,
    int? TmdbId = null);

public sealed record RuleInput(
    string Id,
    int Priority = 0,
    float? Confidence = null,
    IdentificationStrategy? Strategy = null,
    string? SeasonRegex = null,
    string? EpisodeRegex = null,
    int? CaptureGroup = null,
    FilterGroupInput? Filters = null,
    TitleRuleInput[]? TitleRules = null);

public sealed record FilterGroupInput(
    FilterNodeInput[]? All = null,
    FilterNodeInput[]? Any = null,
    FilterNodeInput[]? Not = null);

public sealed record FilterNodeInput(
    FilterField? Field = null,
    FilterOp? Op = null,
    string? Value = null,
    FilterNodeInput[]? All = null,
    FilterNodeInput[]? Any = null,
    FilterNodeInput[]? Not = null);

public sealed record TitleRuleInput(
    TitlePartType Type,
    FilterField? Field = null,
    string? Pattern = null,
    int? CaptureGroup = null,
    string? Value = null);
