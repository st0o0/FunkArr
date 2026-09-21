namespace FunkArr.Messages.Scoring.History;

public enum IdentificationFailureReason
{
    UnknownStrategy,
    SeasonPatternNotMatched,
    NoEpisodePatternConfigured,
    EpisodePatternNotMatched,
    NoTitlePartsConfigured,
    TitlePartRegexNotMatched,
    TitleDoesNotMatch,
    NoDateFoundInTitle,
}
