namespace FunkArr.Messages.Enrichment;

public sealed record EnrichMovies(
    string? ImdbId,
    int? TmdbId,
    MovieCandidate[] Candidates);
