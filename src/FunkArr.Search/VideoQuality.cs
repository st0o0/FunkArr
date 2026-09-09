using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

internal sealed record VideoVariant(string Url, int Quality, long EstimatedSize);

internal static class VideoQuality
{
    internal static VideoVariant[] GetVariants(MediathekItem item)
    {
        var variants = new List<VideoVariant>(3);

        if (item.UrlVideoHd is not null)
        {
            variants.Add(new VideoVariant(item.UrlVideoHd, 1080, EstimateSize(item.Duration, 833_000L)));
        }

        if (item.UrlVideo is not null)
        {
            variants.Add(new VideoVariant(item.UrlVideo, 720, EstimateSize(item.Duration, 420_000L)));
        }

        if (item.UrlVideoLow is not null)
        {
            variants.Add(new VideoVariant(item.UrlVideoLow, 480, EstimateSize(item.Duration, 100_000L)));
        }

        return variants.ToArray();
    }

    private static long EstimateSize(int durationSeconds, long bytesPerSecond) =>
        durationSeconds * bytesPerSecond;
}
