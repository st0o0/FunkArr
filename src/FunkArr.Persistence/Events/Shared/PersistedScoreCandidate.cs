namespace FunkArr.Persistence.Events.Shared;

public sealed record PersistedScoreCandidate(
    string Title,
    string Topic,
    string Channel,
    int Duration,
    int Quality,
    string? Description,
    long Timestamp);
