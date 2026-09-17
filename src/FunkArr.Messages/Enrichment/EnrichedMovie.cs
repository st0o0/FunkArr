namespace FunkArr.Messages.Enrichment;

public sealed record EnrichedMovie(
    int Index,
    string Title,
    int Year,
    string? ImdbId,
    int? TmdbId,
    float Confidence,
    MatchMethod Method) : IEnrichmentResult;
