namespace FunkArr.Persistence.Events.ScoringHistory;

public enum PersistedIdentificationFailureReason
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
