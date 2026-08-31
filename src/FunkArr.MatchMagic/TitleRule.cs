namespace FunkArr.MatchMagic;

public sealed record TitleRule(
    string Type,
    string? Field = null,
    string? Pattern = null,
    int? CaptureGroup = null,
    string? Value = null);
