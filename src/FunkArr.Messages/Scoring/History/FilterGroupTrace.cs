namespace FunkArr.Messages.Scoring.History;

public sealed record FilterGroupTrace(
    FilterGroupOp Operator,
    bool Passed,
    FilterNodeTrace[] Nodes);
