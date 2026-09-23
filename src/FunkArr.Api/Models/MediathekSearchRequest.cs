using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Models;

public sealed record MediathekSearchRequest(
    [FromQuery] string? Q,
    [FromQuery] string? Channel,
    [FromQuery] string? Topic,
    [FromQuery][property: Range(0, int.MaxValue)] int? DurationMin,
    [FromQuery][property: Range(0, int.MaxValue)] int? DurationMax,
    [FromQuery][property: Range(0, int.MaxValue)] int? Offset,
    [FromQuery][property: Range(1, 100)] int? Limit,
    [FromQuery] string? SortBy,
    [FromQuery] string? SortOrder);
