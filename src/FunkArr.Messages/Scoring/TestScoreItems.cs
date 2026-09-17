namespace FunkArr.Messages.Scoring;

public sealed record TestScoreItems(
    Guid RequestId,
    MatchingConfig Config,
    ScoreCandidate[] Candidates);

public abstract record TestScoreItemsResponse;

public sealed record TestScoreCompleted(
    Guid RequestId,
    History.ItemTrace[] ItemTraces) : TestScoreItemsResponse;
