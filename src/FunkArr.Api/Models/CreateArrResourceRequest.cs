using System.ComponentModel.DataAnnotations;

namespace FunkArr.Api.Models;

public sealed record CreateArrResourceRequest(
    [property: Required] string Url,
    [property: Required] string ApiKey,
    string? FunkArrUrl = null);

public sealed record CreateArrResourceResponse(bool Success, string? Error = null);
