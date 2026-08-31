using Microsoft.AspNetCore.Mvc;

namespace FunkArr.ArrApi.Sabnzbd;

internal sealed record DownloadPostRequest(
    [FromQuery(Name = "mode")] string? Mode,
    [FromQuery(Name = "cat")] string? Cat,
    [FromQuery(Name = "priority")] string? Priority);
