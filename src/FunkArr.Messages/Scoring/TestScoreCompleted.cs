namespace FunkArr.Messages.Scoring;

public sealed record TestScoreCompleted(
    Guid RequestId,
    History.ItemTrace[] ItemTraces) : IScoringResponse;
