namespace FunkArr.Messages.Scoring.History;

public sealed record ScoringDetailNotFound(Guid RequestId) : IScoringResponse;
