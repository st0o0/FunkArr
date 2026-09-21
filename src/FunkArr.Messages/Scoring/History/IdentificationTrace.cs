namespace FunkArr.Messages.Scoring.History;

public sealed record IdentificationTrace(
    IdentificationStrategy? Strategy,
    bool Attempted,
    IdentificationFailureReason? Detail);
