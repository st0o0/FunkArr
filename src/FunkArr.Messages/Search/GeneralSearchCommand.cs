namespace FunkArr.Messages.Search;

public sealed record GeneralSearchCommand(string? Query, int? Cat, int? Limit, int? Offset);
