namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedIdentificationTrace(
    string? Strategy,
    bool Attempted,
    string? Detail);
