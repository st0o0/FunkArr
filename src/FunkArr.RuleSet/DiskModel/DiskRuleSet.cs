using System.Text.Json;

namespace FunkArr.RuleSet.DiskModel;

public sealed class DiskRuleSet
{
    public string Topic { get; set; } = "";
    public List<string>? Aliases { get; set; }
    public DiskMedia? Media { get; set; }
    public float? Confidence { get; set; }
    public List<DiskRule>? Rules { get; set; }
    public bool Standalone { get; set; }
    public List<string>? Disable { get; set; }
    public DiskEnrichment? Enrichment { get; set; }
}

public sealed class DiskMedia
{
    public string? Name { get; set; }
    public Messages.MediaType? Type { get; set; }
    public int? TvdbId { get; set; }
    public string? ImdbId { get; set; }
    public int? TmdbId { get; set; }
}

public sealed class DiskRule
{
    public string Id { get; set; } = "";
    public int Priority { get; set; }
    public float? Confidence { get; set; }
    public string? Strategy { get; set; }
    public DiskFilterGroup? Filters { get; set; }
    public string? SeasonRegex { get; set; }
    public string? EpisodeRegex { get; set; }
    public int? CaptureGroup { get; set; }
    public List<DiskTitleRule>? TitleRules { get; set; }
}

public sealed class DiskFilterGroup
{
    public List<JsonElement>? All { get; set; }
    public List<JsonElement>? Any { get; set; }
    public List<JsonElement>? Not { get; set; }
}

public sealed class DiskFilter
{
    public Messages.Scoring.FilterField? Field { get; set; }
    public Messages.Scoring.FilterOp? Op { get; set; }
    public string? Value { get; set; }
}

public sealed class DiskTitleRule
{
    public Messages.Scoring.TitlePartType? Type { get; set; }
    public Messages.Scoring.FilterField? Field { get; set; }
    public string? Pattern { get; set; }
    public int? CaptureGroup { get; set; }
    public string? Value { get; set; }
}

public sealed class DiskEnrichment
{
    public bool? Enabled { get; set; }
    public List<Messages.Enrichment.EnrichmentMethod>? Methods { get; set; }
    public DiskTitleMatch? Title { get; set; }
    public DiskAirdateMatch? Airdate { get; set; }
    public DiskRuntimeMatch? Runtime { get; set; }
    public DiskYearMatch? Year { get; set; }
}

public sealed class DiskTitleMatch
{
    public float? Threshold { get; set; }
}

public sealed class DiskAirdateMatch
{
    public int? Tolerance { get; set; }
    public float? MinTitleAffinity { get; set; }
}

public sealed class DiskRuntimeMatch
{
    public float? Tolerance { get; set; }
    public Messages.Enrichment.RuntimeMode? Mode { get; set; }
}

public sealed class DiskYearMatch
{
    public int? Tolerance { get; set; }
}
