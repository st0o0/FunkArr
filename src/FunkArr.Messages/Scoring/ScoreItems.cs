namespace FunkArr.Messages.Scoring;

public sealed record ScoreItems(
    Guid RequestId,
    string RuleSetId,
    ScoringOrigin Origin,
    ScoreCandidate[] Candidates);
