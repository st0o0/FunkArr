namespace FunkArr.Messages.Scoring;

public sealed record TestScoreItems(
    Guid RequestId,
    MatchingConfig Config,
    ScoreCandidate[] Candidates);
