namespace FunkArr.Api.Models;

public sealed record OperationResult(bool Success, string? Error = null);
