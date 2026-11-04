namespace FunkArr.Messages.Scoring;

public enum IdentificationStrategy
{
    Unknown = 0,
    SeasonAndEpisodeNumber = 1,
    AbsoluteEpisodeNumber = 2,
    TitleExact = 3,
    TitleIncludes = 4,
    AirdateExtraction = 5
}
