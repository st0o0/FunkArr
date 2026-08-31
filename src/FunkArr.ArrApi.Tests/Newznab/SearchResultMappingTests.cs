using System.Text;
using FunkArr.ArrApi.Newznab;
using FunkArr.Messages.Search;
using Xunit;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class SearchResultMappingTests
{
    [Fact]
    public void ToRss_maps_search_completed_to_rss()
    {
        var completed = new SearchCompleted(
            Guid.NewGuid(),
            [
                new SearchResultItem(
                    "Tatort: Die goldene Zeit", "ARD", "Tatort",
                    "https://example.com/hd.mp4", 5400, 1200000000, 720,
                    new DateTimeOffset(2024, 6, 25, 20, 15, 0, TimeSpan.Zero), 0.95),
            ],
            1);

        var rss = IndexerApiEndpoints.ToRss(completed, 0);

        Assert.Equal(1, rss.Channel.Response.Total);
        Assert.Single(rss.Channel.Items);
        var item = rss.Channel.Items[0];
        Assert.Equal("Tatort: Die goldene Zeit", item.Title);
        Assert.Equal("https://example.com/hd.mp4", item.Link);
        Assert.Equal("TV > HD", item.Category);
        Assert.Equal("ARD - Tatort", item.Description);
        Assert.Equal(1200000000, item.Enclosure.Length);
    }

    [Fact]
    public void ToRss_maps_sd_quality()
    {
        var completed = new SearchCompleted(
            Guid.NewGuid(),
            [new SearchResultItem("Test", "ZDF", "Topic", "url", 3600, 500000, 480, null, 0.5)],
            1);

        var rss = IndexerApiEndpoints.ToRss(completed, 0);

        Assert.Equal("TV > SD", rss.Channel.Items[0].Category);
        Assert.Equal("5030", rss.Channel.Items[0].Attributes[1].Value);
    }

    [Fact]
    public void ToRss_encodes_nzb_id_as_base64()
    {
        var completed = new SearchCompleted(
            Guid.NewGuid(),
            [new SearchResultItem("Title", "CH", "Topic", "https://example.com/v.mp4", 100, 0, 720, null, 1.0)],
            1);

        var rss = IndexerApiEndpoints.ToRss(completed, 0);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(rss.Channel.Items[0].Guid.Value));

        Assert.Equal("Title|https://example.com/v.mp4", decoded);
    }

    [Fact]
    public void ToRss_serializes_to_valid_xml()
    {
        var completed = new SearchCompleted(
            Guid.NewGuid(),
            [new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9)],
            1);

        var rss = IndexerApiEndpoints.ToRss(completed, 5);
        var xml = IndexerApiEndpoints.Serialize(rss);

        Assert.Contains("<title>Test</title>", xml);
        Assert.Contains("offset=\"5\"", xml);
        Assert.Contains("total=\"1\"", xml);
    }

    [Fact]
    public void ParseInt_parses_valid_integers() =>
        Assert.Equal(5040, IndexerApiEndpoints.ParseInt("5040"));

    [Fact]
    public void ParseInt_returns_null_for_invalid() =>
        Assert.Null(IndexerApiEndpoints.ParseInt("abc"));

    [Fact]
    public void ParseInt_returns_null_for_null() =>
        Assert.Null(IndexerApiEndpoints.ParseInt(null));
}
