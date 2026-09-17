namespace FunkArr.Messages.Enrichment;

public sealed record EnrichedEpisode(
    int Index,
    string Season,
    string Episode,
    string EpisodeName,
    float Confidence,
    MatchMethod Method) : IEnrichmentResult;
