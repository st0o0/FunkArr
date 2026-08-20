using FunkArr.Indexer;
using FunkArr.Shared.Models;

namespace FunkArr.Tests.Indexer;

public class NewznabXmlBuilderTests
{
    [Fact]
    public void BuildCapsResponse_ContainsTvSearch()
    {
        var xml = NewznabXmlBuilder.BuildCapsResponse("http://localhost:8080");

        Assert.Contains("tv-search", xml);
        Assert.Contains("movie-search", xml);
        Assert.Contains("search", xml);
        Assert.Contains("FunkArr", xml);
    }

    [Fact]
    public void BuildSearchResponse_EmptyResults_ReturnsValidXml()
    {
        var xml = NewznabXmlBuilder.BuildSearchResponse([]);

        Assert.Contains("<rss", xml);
        Assert.Contains("total=\"0\"", xml);
    }

    [Fact]
    public void BuildSearchResponse_WithResults_ContainsItems()
    {
        var results = new[]
        {
            new NewznabResult
            {
                Title = "Tatort.S01E03.GERMAN.720p.WEB.h264-FA",
                DownloadUrl = "http://localhost/api/fake_nzb?url=test",
                SizeBytes = 1_500_000_000,
                PublishDate = DateTimeOffset.Parse("2024-01-15T20:15:00+01:00"),
                Category = "5040",
                Guid = "test-guid-1",
            },
        };

        var xml = NewznabXmlBuilder.BuildSearchResponse(results);

        Assert.Contains("Tatort.S01E03", xml);
        Assert.Contains("test-guid-1", xml);
        Assert.Contains("1500000000", xml);
    }

    [Fact]
    public void BuildSearchResponse_WithResults_ContainsLanguageAttribute()
    {
        var results = new[]
        {
            new NewznabResult
            {
                Title = "Test",
                DownloadUrl = "http://localhost/test",
                SizeBytes = 100,
                PublishDate = DateTimeOffset.UtcNow,
                Category = "5040",
                Guid = "test-1",
            },
        };

        var xml = NewznabXmlBuilder.BuildSearchResponse(results);

        Assert.Contains("name=\"language\"", xml);
        Assert.Contains("value=\"German\"", xml);
    }

    [Fact]
    public void BuildSearchResponse_WithQualityInfo_ContainsVideoAndResolutionAttributes()
    {
        var qi = new QualityInfo
        {
            Resolution = new Resolution(1920, 1080),
            Codec = "h265",
            FileSize = 2_000_000_000,
            ProbeSource = ProbeSource.UrlPattern,
        };

        var results = new[]
        {
            new NewznabResult
            {
                Title = "Test",
                DownloadUrl = "http://localhost/test",
                SizeBytes = 2_000_000_000,
                PublishDate = DateTimeOffset.UtcNow,
                Category = "5040",
                Guid = "test-1",
                QualityInfo = qi,
            },
        };

        var xml = NewznabXmlBuilder.BuildSearchResponse(results);

        Assert.Contains("name=\"video\" value=\"h265\"", xml);
        Assert.Contains("name=\"resolution\" value=\"1080p\"", xml);
    }

    [Fact]
    public void BuildSearchResponse_EstimatedQuality_OmitsResolutionAttribute()
    {
        var qi = new QualityInfo
        {
            Resolution = new Resolution(1280, 720),
            FileSize = 1_000_000_000,
            ProbeSource = ProbeSource.Estimated,
        };

        var results = new[]
        {
            new NewznabResult
            {
                Title = "Test",
                DownloadUrl = "http://localhost/test",
                SizeBytes = 1_000_000_000,
                PublishDate = DateTimeOffset.UtcNow,
                Category = "5040",
                Guid = "test-1",
                QualityInfo = qi,
            },
        };

        var xml = NewznabXmlBuilder.BuildSearchResponse(results);

        Assert.DoesNotContain("name=\"resolution\"", xml);
        Assert.Contains("name=\"video\" value=\"h264\"", xml);
    }

    [Fact]
    public void BuildSearchResponse_WithTvdbInfo_ContainsTvAttributes()
    {
        var results = new[]
        {
            new NewznabResult
            {
                Title = "Test",
                DownloadUrl = "http://localhost/test",
                SizeBytes = 100,
                PublishDate = DateTimeOffset.UtcNow,
                Category = "5040",
                Guid = "test-1",
                TvdbId = 83214,
                Season = 1,
                Episode = 5,
            },
        };

        var xml = NewznabXmlBuilder.BuildSearchResponse(results);

        Assert.Contains("name=\"tvdbid\" value=\"83214\"", xml);
        Assert.Contains("name=\"season\" value=\"1\"", xml);
        Assert.Contains("name=\"episode\" value=\"5\"", xml);
    }

    [Fact]
    public void BuildErrorResponse_ContainsCodeAndDescription()
    {
        var xml = NewznabXmlBuilder.BuildErrorResponse(100, "Incorrect user credentials");

        Assert.Contains("code=\"100\"", xml);
        Assert.Contains("Incorrect user credentials", xml);
    }

    [Theory]
    [InlineData("Tatort", 1, 3, QualityTier.HD1080, "Tatort.S01E03.GERMAN.1080p.WEB.h264-FA")]
    [InlineData("Tatort", 1, 3, QualityTier.HD720, "Tatort.S01E03.GERMAN.720p.WEB.h264-FA")]
    [InlineData("Tatort", 1, 3, QualityTier.SD, "Tatort.S01E03.GERMAN.480p.WEB.h264-FA")]
    [InlineData("Der Alte", 5, 12, QualityTier.HD1080, "Der.Alte.S05E12.GERMAN.1080p.WEB.h264-FA")]
    public void BuildReleaseTitle_FormatsCorrectly(
        string show, int season, int episode, QualityTier quality, string expected)
    {
        var result = NewznabXmlBuilder.BuildReleaseTitle(show, season, episode, quality);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildReleaseTitle_WithCustomCodec_UsesCodec()
    {
        var result = NewznabXmlBuilder.BuildReleaseTitle("Tatort", 1, 3, QualityTier.HD1080, "h265");
        Assert.Equal("Tatort.S01E03.GERMAN.1080p.WEB.h265-FA", result);
    }

    [Theory]
    [InlineData("Tatort", 2024, QualityTier.HD1080, "Tatort.2024.GERMAN.1080p.WEB.h264-FA")]
    [InlineData("Der Untergang", 2004, QualityTier.HD720, "Der.Untergang.2004.GERMAN.720p.WEB.h264-FA")]
    public void BuildMovieReleaseTitle_FormatsCorrectly(
        string movie, int year, QualityTier quality, string expected)
    {
        var result = NewznabXmlBuilder.BuildMovieReleaseTitle(movie, year, quality);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildMovieReleaseTitle_WithCustomCodec_UsesCodec()
    {
        var result = NewznabXmlBuilder.BuildMovieReleaseTitle("Tatort", 2024, QualityTier.HD720, "h265");
        Assert.Equal("Tatort.2024.GERMAN.720p.WEB.h265-FA", result);
    }
}
