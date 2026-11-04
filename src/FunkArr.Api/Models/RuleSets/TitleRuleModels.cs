namespace FunkArr.Api.Models.RuleSets;

public enum TitlePartType
{
    Static,
    Regex
}

public sealed record TitleRuleOutput(
    TitlePartType Type,
    FilterField? Field = null,
    string? Pattern = null,
    int? CaptureGroup = null,
    string? Value = null);
