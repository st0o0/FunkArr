using FunkArr.Messages.Scoring;

namespace FunkArr.Scoring;

internal sealed record ExecuteScoring(
    MatchingConfig Config,
    ScoreCandidate[] Items,
    Guid RequestId,
    ScoringOrigin Origin);
