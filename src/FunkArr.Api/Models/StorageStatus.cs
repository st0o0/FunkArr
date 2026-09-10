namespace FunkArr.Api.Models;

public sealed record StorageStatusResponse(
    StorageDirectory CompleteDirectory,
    StorageDirectory IncompleteDirectory);

public sealed record StorageDirectory(
    string Path,
    long AvailableBytes,
    long TotalBytes);
