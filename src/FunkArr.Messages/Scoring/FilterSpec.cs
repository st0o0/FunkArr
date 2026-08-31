namespace FunkArr.Messages.Scoring;

public abstract record FilterNode
{
    public sealed record ConditionNode(FilterCondition Condition) : FilterNode;

    public sealed record GroupNode(FilterSpec Group) : FilterNode;
}

public sealed record FilterSpec(
    FilterNode[]? All = null,
    FilterNode[]? Any = null,
    FilterNode[]? Not = null);
