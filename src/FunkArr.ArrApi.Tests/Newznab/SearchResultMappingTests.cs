using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.ArrApi.Newznab.Services;
using FunkArr.Messages;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class SearchResultMappingTests
{
    private const string _baseUrl = "http://localhost:6969";
    private const string _apiKey = "test-key";
    private static readonly NewznabAttributeBuilder _attributeBuilder = new();
    private static readonly RssItemBuilder _rssItemBuilder = new(_attributeBuilder);

    [Fact]
    public void ToRss_maps_search_items_to_rss()
    {
        var items = new[]
        {
            new SearchResultItem(
                "Tatort: Die goldene Zeit", "ARD", "Tatort",
                "https://example.com/hd.mp4", 5400, 1200000000, 720,
                new DateTimeOffset(2024, 6, 25, 20, 15, 0, TimeSpan.Zero), 0.95)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);

        Assert.Equal(1, rss.Channel.Response.Total);
        var item = Assert.Single(rss.Channel.Items);
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
            new SearchResultItem("Test", "ZDF", "Topic", "url", 3600, 500000, 480, null, 0.5)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);

        var item = Assert.Single(rss.Channel.Items);
        Assert.Equal("TV > SD", item.Category);
        Assert.Equal("5030", item.Attributes[1].Value);
    }

    [Fact]
    public void ToRss_encodes_nzb_id_as_payload()
    {
        var items = new[]
        {
            new SearchResultItem("Title", "CH", "Topic", "https://example.com/v.mp4", 100, 0, 720, null, 1.0)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var item = Assert.Single(rss.Channel.Items);
        var decoded = NzbPayload.Decode(item.Guid.Value);

        Assert.Equal(new NzbPayload("Title", "https://example.com/v.mp4", null, "CH", 100, 0, MediaType.Show), decoded);
    }

    [Fact]
    public void ToRss_uses_movie_category_and_subtitle_in_nzb_payload()
    {
        var items = new[]
        {
            new SearchResultItem(
                "Film", "ZDF", "Topic", "https://example.com/f.mp4", 200, 5, 720, null, 1.0,
                "https://example.com/f.vtt")
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Movie);
        var item = Assert.Single(rss.Channel.Items);
        var decoded = NzbPayload.Decode(item.Guid.Value);

        Assert.Equal(
            new NzbPayload("Film", "https://example.com/f.mp4", "https://example.com/f.vtt", "ZDF", 200, 5, MediaType.Movie),
            decoded);
    }

    [Fact]
    public void ToRss_serializes_to_valid_xml()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
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
                Metadata: new MatchMetadata(new ExternalIds(83214, null, null), null, null, null, null))
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var item = Assert.Single(rss.Channel.Items);
        var attrs = item.Attributes;

        Assert.Contains(attrs, a => a is { Name: "tvdbid", Value: "83214" });
    }

    [Fact]
    public void ToRss_emits_imdb_attribute_not_imdbid()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9,
                Metadata: new MatchMetadata(new ExternalIds(null, "tt0806910", null), null, null, null, null))
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var item = Assert.Single(rss.Channel.Items);
        var attrs = item.Attributes;

        Assert.Contains(attrs, a => a is { Name: "imdb", Value: "tt0806910" });
        Assert.DoesNotContain(attrs, a => a.Name == "imdbid");
    }

    [Fact]
    public void ToRss_emits_tmdbid_attribute()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 7200, 100, 720, null, 0.9,
                Metadata: new MatchMetadata(new ExternalIds(null, null, 2116), null, null, null, null))
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var item = Assert.Single(rss.Channel.Items);
        var attrs = item.Attributes;

        Assert.Contains(attrs, a => a is { Name: "tmdbid", Value: "2116" });
    }

    [Fact]
    public void ToRss_omits_id_attributes_when_null()
    {
        var items = new[]
        {
            new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var item = Assert.Single(rss.Channel.Items);
        var attrs = item.Attributes;

        Assert.DoesNotContain(attrs, a => a.Name == "tvdbid");
        Assert.DoesNotContain(attrs, a => a.Name == "imdb");
        Assert.DoesNotContain(attrs, a => a.Name == "tmdbid");
    }

    [Fact]
    public void ToRss_movie_search_uses_movie_categories_hd()
    {
        var items = new[]
        {
            new SearchResultItem("Film", "ARD", "Film", "url", 5400, 1200000000, 720, null, 0.9)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Movie);

        var item = Assert.Single(rss.Channel.Items);
        Assert.Equal("Movies > HD", item.Category);
        Assert.Equal("2040", item.Attributes[1].Value);
    }

    [Fact]
    public void ToRss_movie_search_uses_movie_categories_sd()
    {
        var items = new[]
        {
            new SearchResultItem("Film", "ZDF", "Film", "url", 3600, 500000, 480, null, 0.5)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Movie);

        var item = Assert.Single(rss.Channel.Items);
        Assert.Equal("Movies > SD", item.Category);
        Assert.Equal("2030", item.Attributes[1].Value);
    }

    [Fact]
    public void BuildAttributes_tv_search_uses_tv_categories()
    {
        var item = new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9);
        var attrs = _attributeBuilder.BuildAttributes(item, NewznabCategory.Tv);

        Assert.Equal("5040", attrs[1].Value);
    }

    [Fact]
    public void BuildAttributes_movie_search_uses_movie_categories()
    {
        var item = new SearchResultItem("Test", "ARD", "Film", "url", 5400, 100, 720, null, 0.9);
        var attrs = _attributeBuilder.BuildAttributes(item, NewznabCategory.Movie);

        Assert.Equal("2040", attrs[1].Value);
    }

    [Fact]
    public void ToRss_preserves_tabs_in_nzb_payload_fields()
    {
        var items = new[]
        {
            new SearchResultItem(
                "Title\twith\ttabs", "Chan\tnel", "Topic", "https://example.com/v.mp4",
                100, 0, 720, null, 1.0)
        };

        var rss = _rssItemBuilder.ToRss(items, 1, 0, _baseUrl, _apiKey, NewznabCategory.Tv);
        var item = Assert.Single(rss.Channel.Items);
        var decoded = NzbPayload.Decode(item.Guid.Value);

        Assert.NotNull(decoded);
        Assert.Multiple(
            () => Assert.Equal("Title\twith\ttabs", decoded.Title),
            () => Assert.Equal("Chan\tnel", decoded.Channel));
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
        var rss = _rssItemBuilder.ToRss(paged, allItems.Length, 2, _baseUrl, _apiKey, NewznabCategory.Tv);

        Assert.Equal(3, rss.Channel.Items.Count);
        Assert.Equal("Item-2", rss.Channel.Items[0].Title);
        Assert.Equal("Item-4", rss.Channel.Items[2].Title);
        Assert.Equal(10, rss.Channel.Response.Total);
        Assert.Equal(2, rss.Channel.Response.Offset);
    }

    [Fact]
    public void ToRss_empty_items_returns_empty_channel()
    {
        var rss = _rssItemBuilder.ToRss([], 0, 0, _baseUrl, _apiKey, NewznabCategory.Tv);

        Assert.Empty(rss.Channel.Items);
        Assert.Equal(0, rss.Channel.Response.Total);
    }

    [Fact]
    public void BuildAttributes_always_emits_language_german()
    {
        var item = new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9);
        var attrs = _attributeBuilder.BuildAttributes(item, NewznabCategory.Tv);

        Assert.Contains(attrs, a => a is { Name: "language", Value: "German" });
    }

    [Fact]
    public void BuildAttributes_emits_subs_when_subtitle_present()
    {
        var item = new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9,
            "https://example.com/subs.vtt");
        var attrs = _attributeBuilder.BuildAttributes(item, NewznabCategory.Tv);

        Assert.Contains(attrs, a => a is { Name: "subs", Value: "German" });
    }

    [Fact]
    public void BuildAttributes_omits_subs_when_no_subtitle()
    {
        var item = new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9);
        var attrs = _attributeBuilder.BuildAttributes(item, NewznabCategory.Tv);

        Assert.DoesNotContain(attrs, a => a.Name == "subs");
    }

    [Fact]
    public void BuildAttributes_emits_usenetdate_when_firstSeenAt_provided()
    {
        var item = new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9);
        var firstSeen = new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var attrs = _attributeBuilder.BuildAttributes(item, NewznabCategory.Tv, firstSeen);

        var usenetdate = Assert.Single(attrs, a => a.Name == "usenetdate");
        Assert.Equal("Sun, 08 Nov 2026 10:00:00 GMT", usenetdate.Value);
    }

    [Fact]
    public void BuildAttributes_omits_usenetdate_when_null()
    {
        var item = new SearchResultItem("Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.9);
        var attrs = _attributeBuilder.BuildAttributes(item, NewznabCategory.Tv);

        Assert.DoesNotContain(attrs, a => a.Name == "usenetdate");
    }
}
