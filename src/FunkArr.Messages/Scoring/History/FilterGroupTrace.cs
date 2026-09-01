namespace FunkArr.Messages.Scoring.History;

public sealed record FilterGroupTrace(
    string Operator,
    bool Passed,
    FilterNodeTrace[] Nodes);
