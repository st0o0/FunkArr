namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedFilterNodeTrace(
    string? Field,
    string? Op,
    string? ExpectedValue,
    string? ActualValue,
    bool Passed,
    bool Skipped,
    PersistedFilterGroupTrace? Group);
