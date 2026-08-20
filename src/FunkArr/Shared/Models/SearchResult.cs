namespace FunkArr.Shared.Models;

public sealed record SearchResult
{
    public required string Title { get; init; }
    public required string Topic { get; init; }
    public required string Channel { get; init; }
    public required string Url { get; init; }
    public string? UrlSubtitle { get; init; }
    public required int DurationSeconds { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Description { get; init; }
    public required QualityTier Quality { get; init; }
    public QualityInfo? QualityInfo { get; init; }
    public double Score { get; init; }
}

public enum QualityTier
{
    SD,
    HD720,
    HD1080,
}
