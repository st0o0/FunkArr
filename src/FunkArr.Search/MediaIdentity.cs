namespace FunkArr.Search;

public abstract record MediaIdentity(string? ImdbId);

public sealed record ShowIdentity(
    string? ImdbId,
    int? TvdbId,
    string? Season,
    string? Episode) : MediaIdentity(ImdbId);

public sealed record MovieIdentity(
    string? ImdbId,
    int? TmdbId,
    int? Year) : MediaIdentity(ImdbId);
