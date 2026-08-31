namespace FunkArr.MatchMagic;

public sealed record MediaRef(
    int? TvdbId = null,
    string? ImdbId = null,
    int? TmdbId = null,
    string Name = "",
    MediaType Type = MediaType.Show);
