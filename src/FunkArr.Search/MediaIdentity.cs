namespace FunkArr.Search;

public sealed record MediaIdentity(
    int? TvdbId,
    string? ImdbId,
    int? TmdbId,
    string? Season,
    string? Episode);
