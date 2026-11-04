namespace FunkArr.Api.Models;

public enum MediaType
{
    Show,
    Movie
}

public enum SearchSource
{
    Sonarr,
    Radarr,
    Prowlarr,
    Feed = 4
}

public enum FilterOp
{
    Eq,
    Contains,
    NotContains,
    GreaterThan,
    LessThan,
    Regex
}

public enum FilterField
{
    Title,
    Topic,
    Channel,
    Description,
    Duration,
    Timestamp
}

public enum IdentificationStrategy
{
    SeasonAndEpisodeNumber,
    AbsoluteEpisodeNumber,
    TitleExact,
    TitleIncludes,
    AirdateExtraction
}

public enum TitlePartType
{
    Static,
    Regex
}

public enum EnrichmentMethod
{
    Title,
    Airdate
}

public enum RuntimeMode
{
    Tiebreaker,
    Filter
}

public enum MediathekVariant
{
    Standard,
    AudioDescription,
    SignLanguage,
    PlainLanguage,
    OriginalVersion
}

public enum VariantPolicy
{
    Exclude,
    Allow
}

public enum RuleOutcome
{
    Matched,
    FilterFailed,
    IdentificationFailed
}

public enum MatchMethod
{
    RegexExtracted,
    TitleMatch,
    AirdateMatch,
    YearMatch
}

public enum QueuePhase
{
    VideoDownload,
    Remuxing,
    Moving
}

public enum QueuePriority
{
    Low = -1,
    Normal = 0,
    High = 1
}
