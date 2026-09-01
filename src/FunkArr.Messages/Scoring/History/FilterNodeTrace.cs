namespace FunkArr.Messages.Scoring.History;

public sealed record FilterNodeTrace(
    string? Field,
    string? Op,
    string? ExpectedValue,
    string? ActualValue,
    bool Passed,
    bool Skipped,
    FilterGroupTrace? Group);
