using FunkArr.Api.Models.Scoring;

namespace FunkArr.Api.Models.RuleSets;

public enum EnrichmentMethod
{
    Unknown = 0,
    Title = 1,
    Airdate = 2
}

public enum RuntimeMode
{
    Unknown = 0,
    Tiebreaker = 1,
    Filter = 2
}

public enum IdentificationStrategy
{
    Unknown = 0,
    SeasonAndEpisodeNumber = 1,
    AbsoluteEpisodeNumber = 2,
    TitleExact = 3,
    TitleIncludes = 4,
    AirdateExtraction = 5
}

public sealed record EnrichmentConfigOutput(
    bool Enabled,
    EnrichmentMethod[] Methods,
    TitleMatchConfigOutput Title,
    AirdateMatchConfigOutput Airdate,
    RuntimeMatchConfigOutput Runtime,
    YearMatchConfigOutput Year);

public sealed record TitleMatchConfigOutput(float Threshold);

public sealed record AirdateMatchConfigOutput(int Tolerance, float MinTitleAffinity);

public sealed record RuntimeMatchConfigOutput(float Tolerance, RuntimeMode Mode);

public sealed record YearMatchConfigOutput(int Tolerance);

public sealed record EnrichmentConfigInput(
    bool? Enabled = null,
    EnrichmentMethod[]? Methods = null,
    TitleMatchConfigInput? Title = null,
    AirdateMatchConfigInput? Airdate = null,
    RuntimeMatchConfigInput? Runtime = null,
    YearMatchConfigInput? Year = null);

public sealed record TitleMatchConfigInput(float? Threshold = null);

public sealed record AirdateMatchConfigInput(int? Tolerance = null, float? MinTitleAffinity = null);

public sealed record RuntimeMatchConfigInput(float? Tolerance = null, RuntimeMode? Mode = null);

public sealed record YearMatchConfigInput(int? Tolerance = null);

public sealed record EnrichmentTraceOutput(
    MatchMethod Method,
    float Confidence,
    bool Enriched,
    string? ResolvedSeason = null,
    string? ResolvedEpisode = null,
    string? ResolvedTitle = null,
    int? ResolvedYear = null,
    int? DaysDiff = null,
    string? Detail = null);
