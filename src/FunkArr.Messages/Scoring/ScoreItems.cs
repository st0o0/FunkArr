namespace FunkArr.Messages.Scoring;

public sealed record ScoreItems(
    Guid RequestId,
    string RuleSetId,
    ScoringOrigin Origin,
    ScoreCandidate[] Candidates);

public abstract record ScoreItemsResponse;

public sealed record ScoreCompleted(
    Guid RequestId,
    ScoredItem[] Results,
    History.ItemTrace[] ItemTraces) : ScoreItemsResponse;

public sealed record ScoringFailed(Exception Cause) : ScoreItemsResponse;
