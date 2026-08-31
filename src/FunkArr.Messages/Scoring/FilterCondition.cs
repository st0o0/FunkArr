namespace FunkArr.Messages.Scoring;

public sealed record FilterCondition(
    FilterField Field,
    FilterOp Op,
    string Value);
