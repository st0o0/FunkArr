using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Sabnzbd;

internal sealed record DownloadGetRequest(
    [FromQuery(Name = "mode")] string? Mode,
    [FromQuery(Name = "name")] string? Name,
    [FromQuery(Name = "value")] string? Value,
    [FromQuery(Name = "start")] int? Start,
    [FromQuery(Name = "limit")] int? Limit,
    [FromQuery(Name = "output")] string? Output);
