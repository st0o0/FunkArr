namespace FunkArr.Api.Models;

public sealed record FilterGroupOutput(
    FilterConditionOutput[]? All = null,
    FilterConditionOutput[]? Any = null,
    FilterConditionOutput[]? Not = null);

public sealed record FilterConditionOutput(FilterField Field, FilterOp Op, string Value);
