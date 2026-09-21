namespace FunkArr.Api.Models;

public sealed record EnrichmentConfigOutput(
    bool Enabled,
    EnrichmentMethod[] Methods,
    TitleMatchConfigOutput Title,
    AirdateMatchConfigOutput Airdate,
    RuntimeMatchConfigOutput Runtime,
    YearMatchConfigOutput Year);

public sealed record TitleMatchConfigOutput(float Threshold);

public sealed record AirdateMatchConfigOutput(int Tolerance);

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

public sealed record AirdateMatchConfigInput(int? Tolerance = null);

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
