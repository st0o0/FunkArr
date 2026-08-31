namespace FunkArr.Messages.Scoring;

public sealed record IdentificationSpec(
    IdentificationStrategy Strategy,
    string? SeasonPattern = null,
    string? EpisodePattern = null,
    int? CaptureGroup = null,
    TitleMatchMode? MatchMode = null,
    TitlePart[]? TitleParts = null);
