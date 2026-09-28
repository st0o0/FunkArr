using System.Xml.Linq;
using FunkArr.Core;
using FunkArr.Messages.Search;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Newznab")]
public sealed class NewznabSearchTests(FunkArrFixture fixture)
{
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
    public async Task Get_nzb_with_valid_base64_id_returns_nzb_content()
    {
        var payload = string.Join('\t', "Test Title", "https://example.com/video.mp4", "", "ARD", "3600", "500000000", "tv");
        var id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));

        var response = await _fixture.Client.GetAsync($"/index/api?t=get&id={Uri.EscapeDataString(id)}&apikey=test-key");

        var contentType = response.Content.Headers.ContentType;
        Assert.NotNull(contentType);
        Assert.Equal("application/x-nzb", contentType.MediaType);
    }
}
