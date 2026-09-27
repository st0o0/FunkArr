namespace FunkArr.Download;

public sealed record RemuxOptions
{
    public string VideoUrl { get; init; }
    public string OutputPath { get; init; }
    public string? SubtitleUrl { get; init; }
    public string? Channel { get; init; }

    public bool IsHls => VideoUrl.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);

    private RemuxOptions(string videoUrl, string outputPath)
    {
        VideoUrl = videoUrl;
        OutputPath = outputPath;
    }

    public static RemuxOptions Create(string videoUrl, string outputPath) =>
        new(videoUrl, outputPath);

    public RemuxOptions WithSubtitle(string? url) =>
        this with { SubtitleUrl = url };

    public RemuxOptions WithChannel(string? channel) =>
        this with { Channel = channel };
}
