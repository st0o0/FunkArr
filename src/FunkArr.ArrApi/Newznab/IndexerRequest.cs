using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Newznab;

internal sealed record IndexerRequest(
    [FromQuery(Name = "t")] string? T,
    [FromQuery(Name = "q")] string? Q,
    [FromQuery(Name = "cat")] string? Cat,
    [FromQuery(Name = "offset")] int? Offset,
    [FromQuery(Name = "limit")] int? Limit,
    [FromQuery(Name = "id")] string? Id,
    [FromQuery(Name = "season")] string? Season,
    [FromQuery(Name = "ep")] string? Ep,
    [FromQuery(Name = "tvdbid")] string? TvdbId,
    [FromQuery(Name = "imdbid")] string? ImdbId,
    [FromQuery(Name = "tmdbid")] string? TmdbId);
