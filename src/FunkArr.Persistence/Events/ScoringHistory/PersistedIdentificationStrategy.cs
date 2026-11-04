namespace FunkArr.Persistence.Events.ScoringHistory;

public enum PersistedIdentificationStrategy
{
    SeasonAndEpisodeNumber = 0,
    AbsoluteEpisodeNumber = 1,
    TitleExact = 2,
    TitleIncludes = 3,
    AirdateExtraction = 4
}
