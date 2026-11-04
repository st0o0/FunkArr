using System.ComponentModel.DataAnnotations;
using FunkArr.Api.Models.Shared;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Models.Downloads;

public sealed record DownloadHistoryRequest(
    [FromQuery][property: Range(0, int.MaxValue)] int? Start,
    [FromQuery][property: Range(1, 1000)] int? Limit,
    [FromQuery] MediaType? Category);
