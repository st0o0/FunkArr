using System.Xml.Linq;
using FunkArr.ArrApi.Newznab;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Newznab")]
public sealed class NewznabSearchTests(FunkArrFixture fixture)
{
    private static readonly XNamespace _newznab = "http://www.newznab.com/DTD/2010/feeds/attributes/";

    private readonly FunkArrFixture _fixture = WithEmptyFeedCache(fixture);

    [Fact]
    public async Task TvSearch_with_results_returns_rss_xml_with_items()
    {
        var searchProbe = _fixture.GetProbe<ISearchManager>();

        var task = _fixture.Client.GetAsync("/index/api?t=tvsearch&q=Tatort&apikey=test-key");

        var msg = searchProbe.ExpectMsg<SearchCommand>();
        Assert.Equal("Tatort", msg.Query);

        var items = new[]
        {
            new SearchResultItem(
                "Tatort.S01E01.720p.WEB-DL", "ARD", "Tatort",
                "https://example.com/video.mp4", 5400, 500_000_000, 720,
                DateTimeOffset.UtcNow, 95.0, "https://example.com/sub.xml")
        };
        searchProbe.Reply(new SearchCommandCompleted(Guid.NewGuid(), items, items.Length));

        var response = await task;

        var content = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(content);
        var ns = XNamespace.Get("http://www.newznab.com/DTD/2010/newnzab/");

        var channel = doc.Root?.Element("channel");
        Assert.NotNull(channel);

        var rssItems = channel.Elements("item").ToList();
        Assert.Single(rssItems);

        var title = rssItems[0].Element("title")?.Value;
        Assert.Equal("Tatort.S01E01.720p.WEB-DL", title);
    }

    [Fact]
    public async Task TvSearch_without_query_returns_validation_item_without_searching()
    {
        var searchProbe = _fixture.GetProbe<ISearchManager>();

        var response = await WithEmptyFeed(
            "/index/api?t=tvsearch&cat=5030,5040&extended=1&offset=0&limit=100&apikey=test-key", MediaType.Show);

        var item = await SingleItem(response);
        Assert.Multiple(
            () => Assert.Equal("FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr", item.Element("title")?.Value),
            () => Assert.Equal("5040", CategoryAttribute(item)));
        searchProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task MovieSearch_without_query_returns_validation_item_instead_of_error()
    {
        var searchProbe = _fixture.GetProbe<ISearchManager>();

        var response = await WithEmptyFeed(
            "/index/api?t=movie&cat=2000,2040&offset=0&limit=100&apikey=test-key", MediaType.Movie);

        var item = await SingleItem(response);
        Assert.Multiple(
            () => Assert.Equal("FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr", item.Element("title")?.Value),
            () => Assert.Equal("2040", CategoryAttribute(item)));
        searchProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task Validation_item_get_link_returns_incorrect_parameter_error()
    {
        var response = await WithEmptyFeed("/index/api?t=tvsearch&apikey=test-key", MediaType.Show);
        var item = await SingleItem(response);
        var link = item.Element("link")?.Value;
        Assert.NotNull(link);

        var getResponse = await _fixture.Client.GetAsync(new Uri(link).PathAndQuery);

        var doc = XDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Multiple(
            () => Assert.Equal("error", doc.Root?.Name.LocalName),
            () => Assert.Equal("201", doc.Root?.Attribute("code")?.Value));
    }

    [Fact]
    public async Task TvSearch_without_query_returns_feed_entries()
    {
        var feedProbe = _fixture.GetProbe<IFeedManager>();
        var searchProbe = _fixture.GetProbe<ISearchManager>();
        var firstSeen = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var entry = new FeedEntry("0123456789abcdef0123456789abcdef", firstSeen, MediaType.Show,
            new SearchResultItem("Tatort.S2026E18.Herz.aus.Eis.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Tatort",
                "https://example.com/video.mp4", 5400, 500_000_000, 720, null, 0.9,
                Metadata: new MatchMetadata(null, "2026", "18", null, null)));

        var task = _fixture.Client.GetAsync("/index/api?t=tvsearch&offset=0&limit=100&apikey=test-key");

        var query = feedProbe.ExpectMsg<QueryFeed>();
        Assert.Multiple(
            () => Assert.Equal(MediaType.Show, query.MediaType),
            () => Assert.Equal(0, query.Offset),
            () => Assert.Equal(100, query.Limit));
        feedProbe.Reply(new FeedResult([entry], 1));

        var item = await SingleItem(await task);
        Assert.Multiple(
            () => Assert.Equal("Tatort.S2026E18.Herz.aus.Eis.GERMAN.720p.WEB.h264-FunkArr", item.Element("title")?.Value),
            () => Assert.Equal("0123456789abcdef0123456789abcdef", item.Element("guid")?.Value),
            () => Assert.Equal("Fri, 30 Oct 2026 12:00:00 GMT", item.Element("pubDate")?.Value),
            () => Assert.Equal("5040", CategoryAttribute(item)));
        searchProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task Repeated_feed_page_is_served_from_cache_until_the_feed_changes()
    {
        var feedProbe = _fixture.GetProbe<IFeedManager>();
        var entry = new FeedEntry("fedcba9876543210fedcba9876543210", DateTimeOffset.UnixEpoch.AddDays(20000), MediaType.Show,
            new SearchResultItem("Tatort.S2026E19.Borowski.GERMAN.720p.WEB.h264-FunkArr", "NDR", "Tatort",
                "https://example.com/borowski.mp4", 5400, 500_000_000, 720, null, 0.9));
        const string url = "/index/api?t=tvsearch&offset=0&limit=50&apikey=test-key";

        var first = _fixture.Client.GetAsync(url);
        feedProbe.ExpectMsg<QueryFeed>();
        feedProbe.Reply(new FeedResult([entry], 1));
        var firstItem = await SingleItem(await first);

        var secondItem = await SingleItem(await _fixture.Client.GetAsync(url));
        feedProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));

        _fixture.Services.GetRequiredService<FeedResultCache>().Clear();
        var third = _fixture.Client.GetAsync(url);
        var query = feedProbe.ExpectMsg<QueryFeed>();
        feedProbe.Reply(new FeedResult([entry], 1));
        await third;

        Assert.Multiple(
            () => Assert.Equal(firstItem.ToString(), secondItem.ToString()),
            () => Assert.Equal(new QueryFeed(MediaType.Show, 0, 50), query));
    }

    [Fact]
    public async Task Cached_feed_page_still_requires_a_valid_api_key()
    {
        var feedProbe = _fixture.GetProbe<IFeedManager>();
        var entry = new FeedEntry("00112233445566778899aabbccddeeff", DateTimeOffset.UnixEpoch.AddDays(20000), MediaType.Show,
            new SearchResultItem("Tatort.S2026E20.Test.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Tatort",
                "https://example.com/test.mp4", 5400, 500_000_000, 720, null, 0.9));

        var first = _fixture.Client.GetAsync("/index/api?t=tvsearch&offset=0&limit=25&apikey=test-key");
        feedProbe.ExpectMsg<QueryFeed>();
        feedProbe.Reply(new FeedResult([entry], 1));
        await first;

        var response = await _fixture.Client.GetAsync("/index/api?t=tvsearch&offset=0&limit=25&apikey=wrong-key");

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Tatort.S2026E20", body);
        feedProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task Empty_feed_is_not_cached()
    {
        await WithEmptyFeed("/index/api?t=tvsearch&offset=0&limit=10&apikey=test-key", MediaType.Show);

        var response = await WithEmptyFeed("/index/api?t=tvsearch&offset=0&limit=10&apikey=test-key", MediaType.Show);

        var item = await SingleItem(response);
        Assert.Equal("FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr", item.Element("title")?.Value);
    }

    [Fact]
    public async Task Get_nzb_with_valid_base64_id_returns_nzb_content()
    {
        var payload = string.Join('\t', "Test Title", "https://example.com/video.mp4", "", "ARD", "3600", "500000000", "tv");
        var id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));

        var response = await _fixture.Client.GetAsync($"/index/api?t=get&id={Uri.EscapeDataString(id)}&apikey=test-key");

        var contentType = response.Content.Headers.ContentType;
        Assert.NotNull(contentType);
        Assert.Equal("application/x-nzb", contentType.MediaType);
    }

    private static FunkArrFixture WithEmptyFeedCache(FunkArrFixture fixture)
    {
        fixture.Services.GetRequiredService<FeedResultCache>().Clear();
        return fixture;
    }

    private async Task<HttpResponseMessage> WithEmptyFeed(string url, MediaType expectedMediaType)
    {
        var feedProbe = _fixture.GetProbe<IFeedManager>();
        var task = _fixture.Client.GetAsync(url);

        var query = feedProbe.ExpectMsg<QueryFeed>();
        Assert.Equal(expectedMediaType, query.MediaType);
        feedProbe.Reply(new FeedResult([], 0));

        return await task;
    }

    private static async Task<XElement> SingleItem(HttpResponseMessage response)
    {
        var doc = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var channel = doc.Root?.Element("channel");
        Assert.NotNull(channel);
        Assert.Equal("1", channel.Element(_newznab + "response")?.Attribute("total")?.Value);
        return Assert.Single(channel.Elements("item"));
    }

    private static string? CategoryAttribute(XElement item) =>
        item.Elements(_newznab + "attr")
            .SingleOrDefault(a => a.Attribute("name")?.Value == "category")
            ?.Attribute("value")?.Value;
}
