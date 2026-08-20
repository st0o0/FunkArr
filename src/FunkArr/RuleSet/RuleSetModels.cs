using System.Text.Json.Serialization;

namespace FunkArr.RuleSet;

public sealed record RuleSetFile
{
    public required string Topic { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public required MediaReference Media { get; init; }
    public required string Source { get; init; }
    public double Confidence { get; init; } = 1.0;
    public required IReadOnlyList<Rule> Rules { get; init; }
    public OverrideConfig? Overrides { get; init; }
}

public sealed record MediaReference
{
    public int? TvdbId { get; init; }
    public string? ImdbId { get; init; }
    public int? TmdbId { get; init; }
    public required string Name { get; init; }
    public string Type { get; init; } = "show";
}

public sealed record Rule
{
    public int Priority { get; init; }
    public FilterGroup Filters { get; init; } = FilterGroup.Empty;
    public required MatchingStrategy Strategy { get; init; }
    public double? Confidence { get; init; }
    public string? SeasonRegex { get; init; }
    public string? EpisodeRegex { get; init; }
    public int? CaptureGroup { get; init; }
    public IReadOnlyList<TitleRule> TitleRules { get; init; } = [];
}

[JsonDerivedType(typeof(Filter), "filter")]
[JsonDerivedType(typeof(FilterGroup), "group")]
public abstract record FilterNode;

public sealed record Filter : FilterNode
{
    public required string Field { get; init; }
    public required FilterOp Op { get; init; }
    public required string Value { get; init; }
}

public sealed record FilterGroup : FilterNode
{
    public IReadOnlyList<FilterNode> All { get; init; } = [];
    public IReadOnlyList<FilterNode> Any { get; init; } = [];
    public IReadOnlyList<FilterNode> Not { get; init; } = [];

    [JsonIgnore]
    public bool IsEmpty => All.Count == 0 && Any.Count == 0 && Not.Count == 0;

    public static readonly FilterGroup Empty = new();
}

public sealed record TitleRule
{
    public required TitleRuleType Type { get; init; }
    public string? Field { get; init; }
    public string? Pattern { get; init; }
    public int? CaptureGroup { get; init; }
    public string? Value { get; init; }
}

public sealed record OverrideConfig
{
    public OverrideMode Mode { get; init; } = OverrideMode.Replace;
    public string? Base { get; init; }
    public IReadOnlyList<Rule> Add { get; init; } = [];
    public IReadOnlyList<int> Remove { get; init; } = [];
}

public enum MatchingStrategy
{
    SeasonAndEpisodeNumber,
    ItemTitleExact,
    ItemTitleIncludes,
    ItemTitleEqualsAirdate,
    ByAbsoluteEpisodeNumber,
}

public enum FilterOp
{
    GreaterThan,
    LessThan,
    ExactMatch,
    Contains,
    Regex,
    Eq,
    NotContains,
}

public enum TitleRuleType
{
    Regex,
    Static,
}

public enum OverrideMode
{
    Replace,
    Merge,
}
