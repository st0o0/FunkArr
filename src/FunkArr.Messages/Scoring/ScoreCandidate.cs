namespace FunkArr.Messages.Scoring;

public sealed record ScoreCandidate(
    string Title,
    string Topic,
    string Channel,
    int Duration,
    int Quality,
    string? Description,
    long Timestamp);
