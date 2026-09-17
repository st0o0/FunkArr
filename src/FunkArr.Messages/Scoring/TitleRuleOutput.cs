namespace FunkArr.Messages.Scoring;

public sealed record TitleRuleOutput(
    TitlePartType Type,
    FilterField? Field = null,
    string? Pattern = null,
    int? CaptureGroup = null,
    string? Value = null);
