using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Models;

public sealed record DownloadHistoryRequest(
    [FromQuery][property: Range(0, int.MaxValue)] int? Start,
    [FromQuery][property: Range(1, 1000)] int? Limit,
    [FromQuery] string? Category);
