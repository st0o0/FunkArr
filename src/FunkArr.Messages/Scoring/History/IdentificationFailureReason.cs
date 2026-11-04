namespace FunkArr.Messages.Scoring.History;

public enum IdentificationFailureReason
{
    Unknown = 0,
    UnknownStrategy = 1,
    SeasonPatternNotMatched = 2,
    NoEpisodePatternConfigured = 3,
    EpisodePatternNotMatched = 4,
    NoTitlePartsConfigured = 5,
    TitlePartRegexNotMatched = 6,
    TitleDoesNotMatch = 7,
    NoDateFoundInTitle = 8
}
