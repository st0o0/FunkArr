using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Models;

public sealed record DownloadHistoryRequest(
    [FromQuery] int? Start,
    [FromQuery] int? Limit,
    [FromQuery] string? Category);
