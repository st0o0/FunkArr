namespace FunkArr.Api.Models;

public sealed record ExternalIdsOutput(
    int? TvdbId,
    string? ImdbId,
    int? TmdbId);
