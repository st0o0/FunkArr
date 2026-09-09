using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal sealed record FetchAndResolveEpisodes(
    int TvdbId,
    int? Season,
    ResolutionConfig Config,
    EpisodeCandidate[] Candidates);

internal sealed record FetchAndResolveMovie(
    string? ImdbId,
    int? TmdbId,
    MovieCandidate[] Candidates);

internal sealed record EpisodeCacheUpdate(
    int TvdbId,
    TvdbEpisode[] Episodes,
    ResolvedEpisode[] Resolved);

internal sealed record MovieCacheUpdate(
    int TmdbId,
    MovieResolved[] Resolved);
