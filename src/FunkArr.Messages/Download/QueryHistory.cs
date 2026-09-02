namespace FunkArr.Messages.Download;

public sealed record QueryHistory(int Start = 0, int Limit = 0, string? Category = null);
