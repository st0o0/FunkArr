using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class ValidationRssTests
{
    private const string _baseUrl = "http://localhost:6969";
    private const string _apiKey = "test-key";

    private static readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_tv_returns_single_validation_item()
    {
        var rss = ValidationRss.Create(NewznabCategory.Tv, 0, _now, _baseUrl, _apiKey);

        var item = Assert.Single(rss.Channel.Items);
        Assert.Multiple(
            () => Assert.Equal("FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr", item.Title),
            () => Assert.Equal("funkarr-validation-tv", item.Guid.Value),
            () => Assert.False(item.Guid.IsPermaLink),
            () => Assert.Equal("http://localhost:6969/index/api?t=get&id=funkarr-validation-tv&apikey=test-key", item.Link),
            () => Assert.Equal(item.Link, item.Enclosure.Url),
            () => Assert.Equal("application/x-nzb", item.Enclosure.Type),
            () => Assert.Equal(1_000_000_000, item.Enclosure.Length),
            () => Assert.Equal("Fri, 30 Oct 2026 12:00:00 GMT", item.PubDate),
            () => Assert.Equal("TV > HD", item.Category),
            () => Assert.Equal("5040", Assert.Single(item.Attributes, a => a.Name == "category").Value),
            () => Assert.Equal("1000000000", Assert.Single(item.Attributes, a => a.Name == "size").Value),
            () => Assert.Equal(0, rss.Channel.Response.Offset),
            () => Assert.Equal(1, rss.Channel.Response.Total));
    }

    [Fact]
    public void Create_movie_returns_single_validation_item()
    {
        var rss = ValidationRss.Create(NewznabCategory.Movie, 0, _now, _baseUrl, _apiKey);

        var item = Assert.Single(rss.Channel.Items);
        Assert.Multiple(
            () => Assert.Equal("FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr", item.Title),
            () => Assert.Equal("funkarr-validation-movie", item.Guid.Value),
            () => Assert.Equal("http://localhost:6969/index/api?t=get&id=funkarr-validation-movie&apikey=test-key", item.Link),
            () => Assert.Equal("Movies > HD", item.Category),
            () => Assert.Equal("2040", Assert.Single(item.Attributes, a => a.Name == "category").Value));
    }

    [Fact]
    public void Create_guid_is_stable_across_requests()
    {
        var first = ValidationRss.Create(NewznabCategory.Tv, 0, _now, _baseUrl, _apiKey);
        var second = ValidationRss.Create(NewznabCategory.Tv, 0, _now.AddHours(1), _baseUrl, _apiKey);

        Assert.Equal(
            Assert.Single(first.Channel.Items).Guid.Value,
            Assert.Single(second.Channel.Items).Guid.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Create_with_offset_beyond_item_returns_no_items(int offset)
    {
        var rss = ValidationRss.Create(NewznabCategory.Tv, offset, _now, _baseUrl, _apiKey);

        Assert.Multiple(
            () => Assert.Empty(rss.Channel.Items),
            () => Assert.Equal(offset, rss.Channel.Response.Offset),
            () => Assert.Equal(1, rss.Channel.Response.Total));
    }

    [Fact]
    public void Create_with_negative_offset_is_treated_as_zero()
    {
        var rss = ValidationRss.Create(NewznabCategory.Tv, -5, _now, _baseUrl, _apiKey);

        Assert.Multiple(
            () => Assert.Single(rss.Channel.Items),
            () => Assert.Equal(0, rss.Channel.Response.Offset));
    }

    [Fact]
    public void Create_escapes_api_key_in_link()
    {
        var rss = ValidationRss.Create(NewznabCategory.Tv, 0, _now, _baseUrl, "a b&c");

        var item = Assert.Single(rss.Channel.Items);
        Assert.Equal("http://localhost:6969/index/api?t=get&id=funkarr-validation-tv&apikey=a%20b%26c", item.Link);
    }
}
