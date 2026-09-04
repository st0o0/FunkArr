namespace FunkArr.Messages.MetadataResolver;

public sealed record ResolveEpisodes(
    int TvdbId,
    int? Season,
    ResolutionConfig Config,
    EpisodeCandidate[] Candidates);
