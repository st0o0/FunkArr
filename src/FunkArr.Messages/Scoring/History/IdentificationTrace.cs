namespace FunkArr.Messages.Scoring.History;

public sealed record IdentificationTrace(
    string? Strategy,
    bool Attempted,
    string? Detail);
