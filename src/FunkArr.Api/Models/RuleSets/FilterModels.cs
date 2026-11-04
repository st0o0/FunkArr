namespace FunkArr.Api.Models;

public enum FilterField
{
    Title,
    Topic,
    Channel,
    Description,
    Duration,
    Timestamp
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

public sealed record FilterGroupOutput(
    FilterConditionOutput[]? All = null,
    FilterConditionOutput[]? Any = null,
    FilterConditionOutput[]? Not = null);

public sealed record FilterConditionOutput(FilterField Field, FilterOp Op, string Value);
