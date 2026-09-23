using System.ComponentModel.DataAnnotations;
using FunkArr.Api.Validation;

namespace FunkArr.Api.Models;

public sealed record CreateArrResourceRequest(
    [property: Required, SafeUrl] string Url,
    [property: Required] string ApiKey,
    string? FunkArrUrl = null);

public sealed record CreateArrResourceResponse(bool Success, string? Error = null);
