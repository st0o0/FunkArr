namespace FunkArr.Messages.Shared;

public sealed record ExternalIds(
    int? TvdbId,
    string? ImdbId,
    int? TmdbId);
