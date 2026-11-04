namespace FunkArr.Api.Models.RuleSets;

public enum FilterField
{
    Unknown = 0,
    Title = 1,
    Topic = 2,
    Channel = 3,
    Description = 4,
    Duration = 5,
    Timestamp = 6
}

public enum FilterOp
{
    Unknown = 0,
    Eq = 1,
    Contains = 2,
    NotContains = 3,
    GreaterThan = 4,
    LessThan = 5,
    Regex = 6
}

public sealed record FilterGroupOutput(
    FilterConditionOutput[]? All = null,
    FilterConditionOutput[]? Any = null,
    FilterConditionOutput[]? Not = null);

public sealed record FilterConditionOutput(FilterField Field, FilterOp Op, string Value);
