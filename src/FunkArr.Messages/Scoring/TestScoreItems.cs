namespace FunkArr.Messages.Scoring;

public sealed record TestScoreItems(
    Guid TestScoringId,
    MatchingConfig Config,
    ScoreCandidate[] Candidates);
