namespace FunkArr.Persistence.Events.ScoringHistory;

public enum PersistedIdentificationStrategy
{
    SeasonAndEpisodeNumber,
    AbsoluteEpisodeNumber,
    TitleExact,
    TitleIncludes,
    AirdateExtraction,
}
