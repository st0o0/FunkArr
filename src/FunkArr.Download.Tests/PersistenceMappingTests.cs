using FunkArr.Download;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence;

namespace FunkArr.Download.Tests;

public sealed class PersistenceMappingTests
{
    [Theory]
    [InlineData(MediaType.Show, PersistedMediaType.Show)]
    [InlineData(MediaType.Movie, PersistedMediaType.Movie)]
    public void MediaType_roundtrip(MediaType domain, PersistedMediaType expected)
    {
        var persisted = domain.ToPersistence();
        Assert.Equal(expected, persisted);

        var restored = persisted.ToDomain();
        Assert.Equal(domain, restored);
    }

    [Theory]
    [InlineData(DownloadPriority.Low, PersistedDownloadPriority.Low)]
    [InlineData(DownloadPriority.Normal, PersistedDownloadPriority.Normal)]
    [InlineData(DownloadPriority.High, PersistedDownloadPriority.High)]
    public void DownloadPriority_roundtrip(DownloadPriority domain, PersistedDownloadPriority expected)
    {
        var persisted = domain.ToPersistence();
        Assert.Equal(expected, persisted);

        var restored = persisted.ToDomain();
        Assert.Equal(domain, restored);
    }

    [Theory]
    [InlineData(DownloadStatus.Queued, PersistedDownloadStatus.Queued)]
    [InlineData(DownloadStatus.Processing, PersistedDownloadStatus.Processing)]
    [InlineData(DownloadStatus.Completed, PersistedDownloadStatus.Completed)]
    [InlineData(DownloadStatus.Failed, PersistedDownloadStatus.Failed)]
    [InlineData(DownloadStatus.Cancelled, PersistedDownloadStatus.Cancelled)]
    public void DownloadStatus_roundtrip(DownloadStatus domain, PersistedDownloadStatus expected)
    {
        var persisted = domain.ToPersistence();
        Assert.Equal(expected, persisted);

        var restored = persisted.ToDomain();
        Assert.Equal(domain, restored);
    }

    [Theory]
    [InlineData(FailureKind.Transient, PersistedFailureKind.Transient)]
    [InlineData(FailureKind.Permanent, PersistedFailureKind.Permanent)]
    public void FailureKind_roundtrip(FailureKind domain, PersistedFailureKind expected)
    {
        var persisted = domain.ToPersistence();
        Assert.Equal(expected, persisted);

        var restored = persisted.ToDomain();
        Assert.Equal(domain, restored);
    }

    [Fact]
    public void MediaType_enum_member_count_matches()
    {
        var domainCount = Enum.GetValues<MediaType>().Count(v => v.ToString() != "Unknown");
        var persistedCount = Enum.GetValues<PersistedMediaType>().Count(v => v.ToString() != "Unknown");
        Assert.Equal(domainCount, persistedCount);
    }

    [Fact]
    public void DownloadPriority_enum_member_count_matches()
    {
        var domainCount = Enum.GetValues<DownloadPriority>().Count(v => v.ToString() != "Unknown");
        var persistedCount = Enum.GetValues<PersistedDownloadPriority>().Count(v => v.ToString() != "Unknown");
        Assert.Equal(domainCount, persistedCount);
    }

    [Fact]
    public void DownloadStatus_enum_member_count_matches()
    {
        var domainCount = Enum.GetValues<DownloadStatus>().Count(v => v.ToString() != "Unknown");
        var persistedCount = Enum.GetValues<PersistedDownloadStatus>().Count(v => v.ToString() != "Unknown");
        Assert.Equal(domainCount, persistedCount);
    }

    [Fact]
    public void FailureKind_enum_member_count_matches()
    {
        var domainCount = Enum.GetValues<FailureKind>().Count(v => v.ToString() != "Unknown");
        var persistedCount = Enum.GetValues<PersistedFailureKind>().Count(v => v.ToString() != "Unknown");
        Assert.Equal(domainCount, persistedCount);
    }

    [Fact]
    public void DownloadMedia_roundtrip()
    {
        var original = new DownloadMedia("Tatort: Der Fall", "https://example.com/video.mp4",
            "https://example.com/sub.vtt", "ARD", 5400, 1073741824, MediaType.Show);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.VideoUrl, restored.VideoUrl);
        Assert.Equal(original.SubtitleUrl, restored.SubtitleUrl);
        Assert.Equal(original.Channel, restored.Channel);
        Assert.Equal(original.Duration, restored.Duration);
        Assert.Equal(original.Size, restored.Size);
        Assert.Equal(original.Category, restored.Category);
    }

    [Fact]
    public void DownloadCompletion_roundtrip()
    {
        var original = new DownloadCompletion("Tatort", MediaType.Show, 500_000_000,
            DownloadStatus.Completed, "/complete/tatort.mkv", null, 120, 1700000000);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.Category, restored.Category);
        Assert.Equal(original.Size, restored.Size);
        Assert.Equal(original.Status, restored.Status);
        Assert.Equal(original.RelativePath, restored.RelativePath);
        Assert.Null(restored.FailMessage);
        Assert.Equal(original.DownloadTimeSeconds, restored.DownloadTimeSeconds);
        Assert.Equal(original.CompletedAt, restored.CompletedAt);
    }

    [Fact]
    public void DownloadCompletion_failed_roundtrip()
    {
        var original = new DownloadCompletion("Tatort", MediaType.Movie, 0,
            DownloadStatus.Failed, null, "Connection timeout", 0, 1700000000);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(DownloadStatus.Failed, restored.Status);
        Assert.Equal("Connection timeout", restored.FailMessage);
        Assert.Null(restored.RelativePath);
    }

    [Fact]
    public void Undefined_MediaType_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((MediaType)99).ToPersistence());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PersistedMediaType)99).ToDomain());
    }

    [Fact]
    public void Undefined_DownloadStatus_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((DownloadStatus)99).ToPersistence());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PersistedDownloadStatus)99).ToDomain());
    }

    [Fact]
    public void Undefined_FailureKind_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((FailureKind)99).ToPersistence());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PersistedFailureKind)99).ToDomain());
    }
}
