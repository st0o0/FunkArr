namespace FunkArr.Messages.Scoring.History;

public sealed record ScoringDetailResult(
    Guid RequestId,
    string Source,
    string Query,
    DateTimeOffset Timestamp,
    ItemTrace[] ItemTraces) : IScoringResponse;
