namespace FunkArr.Persistence.Events.ScoringHistory;

public enum PersistedIdentificationFailureReason
{
    UnknownStrategy = 0,
    SeasonPatternNotMatched = 1,
    NoEpisodePatternConfigured = 2,
    EpisodePatternNotMatched = 3,
    NoTitlePartsConfigured = 4,
    TitlePartRegexNotMatched = 5,
    TitleDoesNotMatch = 6,
    NoDateFoundInTitle = 7
}
