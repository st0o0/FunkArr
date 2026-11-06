using System.Xml.Linq;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core.Actors;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Search;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Newznab")]
public sealed class NewznabSearchTests(FunkArrFixture fixture)
{
    private static readonly XNamespace _newznab = "http://www.newznab.com/DTD/2010/feeds/attributes/";

    [Fact]
    public async Task TvSearch_with_results_returns_rss_xml_with_items()
    {
        var searchProbe = fixture.GetProbe<ISearchManager>();

        var task = fixture.Client.GetAsync("/index/api?t=tvsearch&q=Tatort&apikey=test-key", TestContext.Current.CancellationToken);

        var msg = searchProbe.ExpectMsg<SearchCommand>(cancellationToken: TestContext.Current.CancellationToken);
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

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = XDocument.Parse(content);

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
        var searchProbe = fixture.GetProbe<ISearchManager>();

        var response = await WithEmptyFeed(
            "/index/api?t=tvsearch&cat=5030,5040&extended=1&offset=0&limit=100&apikey=test-key", MediaType.Show);

        var item = await SingleItem(response);
        Assert.Multiple(
            () => Assert.Equal("FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr", item.Element("title")?.Value),
            () => Assert.Equal("5040", CategoryAttribute(item)));
        searchProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MovieSearch_without_query_returns_validation_item_instead_of_error()
    {
        var searchProbe = fixture.GetProbe<ISearchManager>();

        var response = await WithEmptyFeed(
            "/index/api?t=movie&cat=2000,2040&offset=0&limit=100&apikey=test-key", MediaType.Movie);

        var item = await SingleItem(response);
        Assert.Multiple(
            () => Assert.Equal("FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr", item.Element("title")?.Value),
            () => Assert.Equal("2040", CategoryAttribute(item)));
        searchProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Validation_item_get_link_returns_incorrect_parameter_error()
    {
        var response = await WithEmptyFeed("/index/api?t=tvsearch&apikey=test-key", MediaType.Show);
        var item = await SingleItem(response);
        var link = item.Element("link")?.Value;
        Assert.NotNull(link);

        var getResponse = await fixture.Client.GetAsync(new Uri(link).PathAndQuery, TestContext.Current.CancellationToken);

        var doc = XDocument.Parse(await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Multiple(
            () => Assert.Equal("error", doc.Root?.Name.LocalName),
            () => Assert.Equal("201", doc.Root?.Attribute("code")?.Value));
    }

    [Fact]
    public async Task TvSearch_without_query_returns_feed_entries()
    {
        var feedProbe = fixture.GetProbe<IFeedStoreManager>();
        var searchProbe = fixture.GetProbe<ISearchManager>();
        var firstSeen = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var entry = new FeedEntry("0123456789abcdef0123456789abcdef", firstSeen, MediaType.Show,
            "Tatort.S2026E18.Herz.aus.Eis.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Tatort",
            "https://example.com/video.mp4", 5400, 500_000_000, 720, null, 0.9, null,
            null, "2026", "18");

        var task = fixture.Client.GetAsync("/index/api?t=tvsearch&offset=0&limit=100&apikey=test-key", TestContext.Current.CancellationToken);

        var query = feedProbe.ExpectMsg<QueryFeed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Multiple(
            () => Assert.Equal(MediaType.Show, query.MediaType),
            () => Assert.Equal(0, query.Offset),
            () => Assert.Equal(100, query.Limit));
        feedProbe.Reply(new FeedResult([entry], 1, 1));

        var item = await SingleItem(await task);
        Assert.Multiple(
            () => Assert.Equal("Tatort.S2026E18.Herz.aus.Eis.GERMAN.720p.WEB.h264-FunkArr", item.Element("title")?.Value),
            () => Assert.Equal("0123456789abcdef0123456789abcdef", item.Element("guid")?.Value),
            () => Assert.Equal("Fri, 30 Oct 2026 12:00:00 GMT", item.Element("pubDate")?.Value),
            () => Assert.Equal("5040", CategoryAttribute(item)));
        searchProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Get_nzb_with_valid_payload_id_returns_nzb_content()
    {
        var id = new NzbPayload("Test Title", "https://example.com/video.mp4", null, "ARD", 3600, 500_000_000, MediaType.Show).Encode();

        var response = await fixture.Client.GetAsync($"/index/api?t=get&id={Uri.EscapeDataString(id)}&apikey=test-key", TestContext.Current.CancellationToken);

        var contentType = response.Content.Headers.ContentType;
        Assert.NotNull(contentType);
        Assert.Equal("application/x-nzb", contentType.MediaType);
    }

    [Theory]
    [MemberData(nameof(FeedProbeMatrix))]
    public async Task Feed_probe_answers_with_entries_of_requested_media_type_or_validation_item(
        FeedState state, string function, string? cat, MediaType? expectedMediaType, int limit)
    {
        var feedProbe = fixture.GetProbe<IFeedStoreManager>();
        var catQuery = cat is null ? "" : $"&cat={cat}";

        var task = fixture.Client.GetAsync(
            $"/index/api?t={function}{catQuery}&extended=1&offset=0&limit={limit}&apikey=test-key",
            TestContext.Current.CancellationToken);

        var query = feedProbe.ExpectMsg<QueryFeed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(expectedMediaType, query.MediaType);

        var store = StoreFor(state);
        var matching = store.Where(e => query.MediaType is null || e.MediaType == query.MediaType).ToArray();
        feedProbe.Reply(new FeedResult(matching, store.Length, matching.Length));

        var response = await task;
        var doc = XDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var channel = doc.Root?.Element("channel");
        Assert.NotNull(channel);
        var items = channel.Elements("item").ToList();
        var total = channel.Element(_newznab + "response")?.Attribute("total")?.Value;

        if (matching.Length == 0)
        {
            var validation = Assert.Single(items);
            var isMovie = expectedMediaType == MediaType.Movie;
            Assert.Multiple(
                () => Assert.Equal(
                    isMovie ? "FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr" : "FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr",
                    validation.Element("title")?.Value),
                () => Assert.Equal(isMovie ? "2040" : "5040", CategoryAttribute(validation)),
                () => Assert.Equal("1", total));
            return;
        }

        Assert.Multiple(
            () => Assert.Equal(matching.Length.ToString(), total),
            () => Assert.Equal(
                matching.Select(e => e.Title).ToArray(),
                items.Select(i => i.Element("title")?.Value ?? "").ToArray()));
    }

    public enum FeedState
    {
        Empty,
        TvOnly,
        MoviesOnly,
        Both
    }

    public static TheoryData<FeedState, string, string?, MediaType?, int> FeedProbeMatrix()
    {
        var probes = new (string Function, string? Cat, MediaType? Expected)[]
        {
            ("tvsearch", "5030,5040", MediaType.Show),
            ("movie", "2030,2040", MediaType.Movie),
            ("search", "5030,5040", MediaType.Show),
            ("search", "2030,2040", MediaType.Movie),
            ("search", null, null)
        };

        var data = new TheoryData<FeedState, string, string?, MediaType?, int>();
        var limit = 200;
        foreach (var state in Enum.GetValues<FeedState>())
        {
            foreach (var (function, cat, expected) in probes)
            {
                data.Add(state, function, cat, expected, limit++);
            }
        }

        return data;
    }

    private static FeedEntry[] StoreFor(FeedState state) => state switch
    {
        FeedState.Empty => [],
        FeedState.TvOnly => Entries(MediaType.Show, 3),
        FeedState.MoviesOnly => Entries(MediaType.Movie, 2),
        FeedState.Both => [.. Entries(MediaType.Show, 3), .. Entries(MediaType.Movie, 2)],
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private static FeedEntry[] Entries(MediaType mediaType, int count) =>
    [
        .. Enumerable.Range(0, count).Select(i => new FeedEntry(
            $"{mediaType}-{i}", new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero), mediaType,
            $"Probe.{mediaType}.{i}.S2026E{i:D2}.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Probe",
            "https://example.com/video.mp4", 5400, 500_000_000, 720, null, 0.9, null,
            null, "2026", $"{i:D2}"))
    ];

    private async Task<HttpResponseMessage> WithEmptyFeed(string url, MediaType expectedMediaType)
    {
        var feedProbe = fixture.GetProbe<IFeedStoreManager>();
        var task = fixture.Client.GetAsync(url);

        var query = feedProbe.ExpectMsg<QueryFeed>();
        Assert.Equal(expectedMediaType, query.MediaType);
        feedProbe.Reply(new FeedResult([], 0, 0));

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
