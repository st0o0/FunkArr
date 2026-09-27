using System.ComponentModel.DataAnnotations;

namespace FunkArr.Api.Models;

public sealed record CreateArrResourceRequest(
    [property: Required] string Url,
    [property: Required] string ApiKey,
    string? FunkArrUrl = null);

public sealed record ArrResourceResponse(
    bool Success,
    int? Id = null,
    string? Name = null,
    string? Error = null,
    ArrProviderMessage? Message = null,
    IReadOnlyList<ArrValidationError>? ValidationErrors = null);

public sealed record ArrProviderMessage(string Message, string Type);

public sealed record ArrValidationError(string PropertyName, string ErrorMessage, bool IsWarning);
