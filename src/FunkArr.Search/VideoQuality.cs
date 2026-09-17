namespace FunkArr.Search;

internal sealed record VideoVariant(string Url, int Quality, long EstimatedSize);

internal static class VideoQuality
{
    internal static VideoVariant[] GetVariants(SourceInfo source)
    {
        var variants = new List<VideoVariant>(3);

        if (source.UrlHd is not null)
        {
            variants.Add(new VideoVariant(source.UrlHd, 1080, EstimateSize(source.Duration, 833_000L)));
        }

        if (source.Url is not null)
        {
            variants.Add(new VideoVariant(source.Url, 720, EstimateSize(source.Duration, 420_000L)));
        }

        if (source.UrlLow is not null)
        {
            variants.Add(new VideoVariant(source.UrlLow, 480, EstimateSize(source.Duration, 100_000L)));
        }

        return variants.ToArray();
    }

    private static long EstimateSize(int durationSeconds, long bytesPerSecond)
        => durationSeconds * bytesPerSecond;
}
