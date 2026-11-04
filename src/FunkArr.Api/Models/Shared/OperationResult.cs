namespace FunkArr.Api.Models.Shared;

public sealed record OperationResult(bool Success, string? Error = null);
