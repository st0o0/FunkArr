using FunkArr.Indexer;

namespace FunkArr.Tests.Indexer;

public class FakeNzbBuilderTests
{
    [Fact]
    public void BuildFakeNzbXml_ContainsEncodedUrl()
    {
        var xml = FakeNzbBuilder.BuildFakeNzbXml(
            "https://example.com/video.mp4",
            "Tatort.S01E03");

        Assert.Contains("FUNKARR_URL:", xml);
        Assert.Contains("FUNKARR_TITLE:", xml);
        Assert.Contains("<?xml", xml);
        Assert.Contains("<nzb", xml);
    }

    [Fact]
    public void BuildFakeNzbXml_WithSubtitle_ContainsSubtitleComment()
    {
        var xml = FakeNzbBuilder.BuildFakeNzbXml(
            "https://example.com/video.mp4",
            "Tatort.S01E03",
            "https://example.com/subtitle.srt");

        Assert.Contains("FUNKARR_SUBTITLE:", xml);
    }

    [Fact]
    public void ParseFakeNzb_RoundTrips()
    {
        var originalUrl = "https://cdn.mediathek.de/video/tatort-s01e03.mp4";
        var originalTitle = "Tatort.S01E03.GERMAN.1080p.WEB.h264-FA";
        var originalSubtitle = "https://cdn.mediathek.de/subtitle/tatort-s01e03.srt";

        var nzb = FakeNzbBuilder.BuildFakeNzbXml(originalUrl, originalTitle, originalSubtitle);
        var (url, title, subtitleUrl) = FakeNzbBuilder.ParseFakeNzb(nzb);

        Assert.Equal(originalUrl, url);
        Assert.Equal(originalTitle, title);
        Assert.Equal(originalSubtitle, subtitleUrl);
    }

    [Fact]
    public void ParseFakeNzb_WithoutSubtitle_ReturnsNull()
    {
        var nzb = FakeNzbBuilder.BuildFakeNzbXml(
            "https://example.com/video.mp4",
            "TestTitle");

        var (url, title, subtitleUrl) = FakeNzbBuilder.ParseFakeNzb(nzb);

        Assert.Equal("https://example.com/video.mp4", url);
        Assert.Equal("TestTitle", title);
        Assert.Null(subtitleUrl);
    }

    [Fact]
    public void BuildFakeNzbUrl_FormatsCorrectly()
    {
        var result = FakeNzbBuilder.BuildFakeNzbUrl(
            "http://localhost:8080",
            "https://example.com/video.mp4",
            "Test Title");

        Assert.StartsWith("http://localhost:8080/api/fake_nzb?url=", result);
        Assert.Contains("&title=", result);
    }
}
