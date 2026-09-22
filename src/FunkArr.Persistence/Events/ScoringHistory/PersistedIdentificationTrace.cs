namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedIdentificationTrace(
    PersistedIdentificationStrategy? Strategy,
    bool Attempted,
    PersistedIdentificationFailureReason? Detail);
