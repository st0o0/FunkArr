using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Models;

public sealed record MediathekSearchRequest(
    [FromQuery] string? Q,
    [FromQuery] string? Channel,
    [FromQuery] string? Topic,
    [FromQuery] int? DurationMin,
    [FromQuery] int? DurationMax,
    [FromQuery] int? Offset,
    [FromQuery] int? Limit,
    [FromQuery] string? SortBy,
    [FromQuery] string? SortOrder);
