namespace FunkArr.Api.Models;

public sealed record ErrorResponse(string Error);

public sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);
