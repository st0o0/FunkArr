namespace FunkArr.Shared.Models;

public sealed record NewznabResult
{
    public required string Title { get; init; }
    public required string DownloadUrl { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset PublishDate { get; init; }
    public required string Category { get; init; }
    public required string Guid { get; init; }
    public QualityInfo? QualityInfo { get; init; }
    public int? TvdbId { get; init; }
    public int? Season { get; init; }
    public int? Episode { get; init; }
}
