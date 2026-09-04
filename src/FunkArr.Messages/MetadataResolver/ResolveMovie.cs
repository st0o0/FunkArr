namespace FunkArr.Messages.MetadataResolver;

public sealed record ResolveMovie(
    string? ImdbId,
    int? TmdbId,
    MovieCandidate[] Candidates);
