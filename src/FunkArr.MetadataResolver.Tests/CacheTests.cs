namespace FunkArr.MetadataResolver.Tests;

public sealed class CacheTests
{
    [Fact]
    public void Active_show_gets_2_day_ttl()
    {
        var futureDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
        var episodes = new[]
        {
            new TvdbEpisode(2026, 1, "Past Episode", "2026-01-01", 60),
            new TvdbEpisode(2026, 2, "Future Episode", futureDate, 60),
        };

        var ttl = CacheTtl.DetermineShowTtl(episodes);

        Assert.Equal(TimeSpan.FromDays(2), ttl);
    }

    [Fact]
    public void Inactive_show_gets_7_day_ttl()
    {
        var episodes = new[]
        {
            new TvdbEpisode(2025, 1, "Old Episode", "2025-01-01", 60),
            new TvdbEpisode(2025, 2, "Also Old", "2025-06-15", 60),
        };

        var ttl = CacheTtl.DetermineShowTtl(episodes);

        Assert.Equal(TimeSpan.FromDays(7), ttl);
    }

    [Fact]
    public void Empty_episodes_gets_inactive_ttl()
    {
        var ttl = CacheTtl.DetermineShowTtl([]);

        Assert.Equal(TimeSpan.FromDays(7), ttl);
    }

    [Fact]
    public void CacheEntry_not_expired_within_ttl()
    {
        var entry = new CacheEntry(
            new object(), DateTimeOffset.UtcNow, TimeSpan.FromHours(12), "tvdb", 1);

        Assert.False(entry.IsExpired);
    }

    [Fact]
    public void CacheEntry_expired_after_ttl()
    {
        var entry = new CacheEntry(
            new object(), DateTimeOffset.UtcNow.AddHours(-13), TimeSpan.FromHours(12), "tvdb", 1);

        Assert.True(entry.IsExpired);
    }

    [Fact]
    public void Movie_ttl_is_30_days()
    {
        Assert.Equal(TimeSpan.FromDays(30), CacheTtl.Movie);
    }
}
