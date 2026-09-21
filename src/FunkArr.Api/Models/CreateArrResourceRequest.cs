namespace FunkArr.Api.Models;

public sealed record CreateArrResourceRequest(string Url, string ApiKey, string? FunkArrUrl = null);

public sealed record CreateArrResourceResponse(bool Success, string? Error = null);
