namespace FunkArr.Api.Models.Shared;

public sealed record ErrorResponse(string Error);

public sealed record ValidationErrorEntry(string Field, string Message);

public sealed record ValidationErrorResponse(IReadOnlyList<ValidationErrorEntry> Errors);
