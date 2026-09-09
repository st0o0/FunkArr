namespace FunkArr.Messages.Scoring;

public sealed record ScoreCompleted(
    Guid RequestId,
    ScoredItem[] Results,
    MetadataResolver.ResolutionConfig? Resolution = null) : IScoringResponse;
