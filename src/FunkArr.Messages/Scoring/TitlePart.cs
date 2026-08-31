namespace FunkArr.Messages.Scoring;

public sealed record TitlePart(
    TitlePartType Type,
    string? Value = null,
    string? Pattern = null,
    FilterField? Field = null,
    int? CaptureGroup = null);
