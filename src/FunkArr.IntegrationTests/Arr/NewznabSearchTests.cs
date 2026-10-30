using System.Xml.Linq;
using FunkArr.Core;
using FunkArr.Messages.Search;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Newznab")]
public sealed class NewznabSearchTests(FunkArrFixture fixture)
{
    private static readonly XNamespace _newznab = "http://www.newznab.com/DTD/2010/feeds/attributes/";

    private readonly FunkArrFixture _fixture = fixture;

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

        var response = await _fixture.Client.GetAsync(
            "/index/api?t=tvsearch&cat=5030,5040&extended=1&offset=0&limit=100&apikey=test-key");

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

        var response = await _fixture.Client.GetAsync(
            "/index/api?t=movie&cat=2000,2040&offset=0&limit=100&apikey=test-key");

        var item = await SingleItem(response);
        Assert.Multiple(
            () => Assert.Equal("FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr", item.Element("title")?.Value),
            () => Assert.Equal("2040", CategoryAttribute(item)));
        searchProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task Validation_item_get_link_returns_incorrect_parameter_error()
    {
        var response = await _fixture.Client.GetAsync("/index/api?t=tvsearch&apikey=test-key");
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
    public async Task Get_nzb_with_valid_base64_id_returns_nzb_content()
    {
        var payload = string.Join('\t', "Test Title", "https://example.com/video.mp4", "", "ARD", "3600", "500000000", "tv");
        var id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));

        var response = await _fixture.Client.GetAsync($"/index/api?t=get&id={Uri.EscapeDataString(id)}&apikey=test-key");

        var contentType = response.Content.Headers.ContentType;
        Assert.NotNull(contentType);
        Assert.Equal("application/x-nzb", contentType.MediaType);
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
