namespace FunkArr.Persistence.Events.ScoringHistory;

public enum PersistedIdentificationStrategy
{
    Unknown = 0,
    SeasonAndEpisodeNumber = 1,
    AbsoluteEpisodeNumber = 2,
    TitleExact = 3,
    TitleIncludes = 4,
    AirdateExtraction = 5
}
