using System.Text;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Messages.Search;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class SearchResultMappingTests
{
    private const string _baseUrl = "http://localhost:6969";
    private const string _apiKey = "test-key";

    [Fact]
    public void ToRss_maps_search_items_to_rss()
    {
        var items = new[]
        {
            new SearchResultItem(
                "Tatort: Die goldene Zeit", "ARD", "Tatort",
                "https://example.com/hd.mp4", 5400, 1200000000, 720,
                new DateTimeOffset(2024, 6, 25, 20, 15, 0, TimeSpan.Zero), 0.95),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);

        Assert.Equal(1, rss.Channel.Response.Total);
        Assert.Single(rss.Channel.Items);
        var item = rss.Channel.Items[0];
        Assert.Equal("Tatort: Die goldene Zeit", item.Title);
        Assert.StartsWith("http://localhost:6969/index/api?t=get&id=", item.Link);
        Assert.Equal("TV > HD", item.Category);
        Assert.Equal("ARD - Tatort", item.Description);
        Assert.Equal(1200000000, item.Enclosure.Length);
        Assert.Equal(item.Link, item.Enclosure.Url);
    }

    [Fact]
    public void ToRss_maps_sd_quality()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ZDF", "Topic", "url", 3600, 500000, 480, null, 0.5),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);

        Assert.Equal("TV > SD", rss.Channel.Items[0].Category);
        Assert.Equal("5030", rss.Channel.Items[0].Attributes[1].Value);
    }

    [Fact]
    public void ToRss_encodes_nzb_id_as_base64()
    {
        var items = new[]
        {
            new SearchResultItem("Title", "CH", "Topic", "https://example.com/v.mp4", 100, 0, 720, null, 1.0),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(rss.Channel.Items[0].Guid.Value));

        Assert.Equal("Title\thttps://example.com/v.mp4\t\tCH\t100\t0\ttv", decoded);
    }

    [Fact]
    public void ToRss_serializes_to_valid_xml()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var xml = NewznabXmlResult.Serialize(rss);

        Assert.Contains("<title>Test</title>", xml);
        Assert.Contains("offset=\"0\"", xml);
        Assert.Contains("total=\"1\"", xml);
    }

    [Fact]
    public void ToRss_emits_tvdbid_attribute()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9,
                TvdbId: 83214),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var attrs = rss.Channel.Items[0].Attributes;

        Assert.Contains(attrs, a => a.Name == "tvdbid" && a.Value == "83214");
    }

    [Fact]
    public void ToRss_emits_imdb_attribute_not_imdbid()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9,
                ImdbId: "tt0806910"),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var attrs = rss.Channel.Items[0].Attributes;

        Assert.Contains(attrs, a => a.Name == "imdb" && a.Value == "tt0806910");
        Assert.DoesNotContain(attrs, a => a.Name == "imdbid");
    }

    [Fact]
    public void ToRss_emits_tmdbid_attribute()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 7200, 100, 720, null, 0.9,
                TmdbId: 2116),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var attrs = rss.Channel.Items[0].Attributes;

        Assert.Contains(attrs, a => a.Name == "tmdbid" && a.Value == "2116");
    }

    [Fact]
    public void ToRss_omits_id_attributes_when_null()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var attrs = rss.Channel.Items[0].Attributes;

        Assert.DoesNotContain(attrs, a => a.Name == "tvdbid");
        Assert.DoesNotContain(attrs, a => a.Name == "imdb");
        Assert.DoesNotContain(attrs, a => a.Name == "tmdbid");
    }

    [Fact]
    public void ToRss_movie_search_uses_movie_categories_hd()
    {
        var items = new[]
        {
            new SearchResultItem("Film", "ARD", "Film", "url", 5400, 1200000000, 720, null, 0.9),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Movie);

        Assert.Equal("Movies > HD", rss.Channel.Items[0].Category);
        Assert.Equal("2040", rss.Channel.Items[0].Attributes[1].Value);
    }

    [Fact]
    public void ToRss_movie_search_uses_movie_categories_sd()
    {
        var items = new[]
        {
            new SearchResultItem("Film", "ZDF", "Film", "url", 3600, 500000, 480, null, 0.5),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Movie);

        Assert.Equal("Movies > SD", rss.Channel.Items[0].Category);
        Assert.Equal("2030", rss.Channel.Items[0].Attributes[1].Value);
    }

    [Fact]
    public void BuildAttributes_tv_search_uses_tv_categories()
    {
        var item = new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9);
        var attrs = NewznabSearchService.BuildAttributes(item, NewznabCategory.Tv);

        Assert.Equal("5040", attrs[1].Value);
    }

    [Fact]
    public void BuildAttributes_movie_search_uses_movie_categories()
    {
        var item = new SearchResultItem("Test", "ARD", "Film", "url", 5400, 100, 720, null, 0.9);
        var attrs = NewznabSearchService.BuildAttributes(item, NewznabCategory.Movie);

        Assert.Equal("2040", attrs[1].Value);
    }

    [Fact]
    public void ToRss_strips_tabs_from_nzb_payload_fields()
    {
        var items = new[]
        {
            new SearchResultItem(
                "Title\twith\ttabs", "Chan\tnel", "Topic", "https://example.com/v.mp4",
                100, 0, 720, null, 1.0),
        };

        var rss = NewznabSearchService.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(rss.Channel.Items[0].Guid.Value));
        var parts = decoded.Split('\t');

        Assert.Equal(7, parts.Length);
        Assert.Equal("Title with tabs", parts[0]);
        Assert.Equal("Chan nel", parts[3]);
    }

    [Fact]
    public void ParseInt_parses_valid_integers() =>
        Assert.Equal(5040, NewznabSearchService.ParseInt("5040"));

    [Fact]
    public void ParseInt_returns_null_for_invalid() =>
        Assert.Null(NewznabSearchService.ParseInt("abc"));

    [Fact]
    public void ParseInt_returns_null_for_null() =>
        Assert.Null(NewznabSearchService.ParseInt(null));

    [Fact]
    public void ToRss_receives_pre_paged_items()
    {
        var allItems = Enumerable.Range(0, 10)
            .Select(i => new SearchResultItem(
                $"Item-{i}", "ARD", "Topic", $"url-{i}", 3600, 100, 720, null, 0.9 - i * 0.01))
            .ToArray();

        var paged = allItems.Skip(2).Take(3).ToArray();
        var rss = NewznabSearchService.ToRss(paged, allItems.Length, 2, _baseUrl, _apiKey, NewznabCategory.Tv);

        Assert.Equal(3, rss.Channel.Items.Count);
        Assert.Equal("Item-2", rss.Channel.Items[0].Title);
        Assert.Equal("Item-4", rss.Channel.Items[2].Title);
        Assert.Equal(10, rss.Channel.Response.Total);
        Assert.Equal(2, rss.Channel.Response.Offset);
    }

    [Fact]
    public void ToRss_empty_items_returns_empty_channel()
    {
        var rss = NewznabSearchService.ToRss([], 0, 0, _baseUrl, _apiKey, NewznabCategory.Tv);

        Assert.Empty(rss.Channel.Items);
        Assert.Equal(0, rss.Channel.Response.Total);
    }
}
