namespace FunkArr.Messages.Scoring;

public sealed record ScoreItems(
    ScoreCandidate[] Items,
    string RuleSetId);
