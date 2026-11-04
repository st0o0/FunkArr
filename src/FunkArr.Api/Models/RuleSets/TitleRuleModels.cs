namespace FunkArr.Api.Models.RuleSets;

public enum TitlePartType
{
    Unknown = 0,
    Static = 1,
    Regex = 2
}

public sealed record TitleRuleOutput(
    TitlePartType Type,
    FilterField? Field = null,
    string? Pattern = null,
    int? CaptureGroup = null,
    string? Value = null);
