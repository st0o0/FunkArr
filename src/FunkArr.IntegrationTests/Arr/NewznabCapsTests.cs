using System.Net;
using System.Xml.Linq;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Newznab")]
public sealed class NewznabCapsTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Caps_returns_200_with_xml_content_type()
    {
        var response = await _fixture.Client.GetAsync("/index/api?t=caps&apikey=test-key", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contentType = response.Content.Headers.ContentType;
        Assert.NotNull(contentType);
        Assert.Contains("xml", contentType.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = XDocument.Parse(content);
        Assert.Equal("caps", doc.Root?.Name.LocalName);
    }

    [Fact]
    public async Task Missing_api_key_returns_error_xml()
    {
        var response = await _fixture.Client.GetAsync("/index/api?t=caps", TestContext.Current.CancellationToken);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("error", content);
        Assert.Contains("100", content);
    }

    [Fact]
    public async Task Wrong_api_key_returns_error_xml()
    {
        var response = await _fixture.Client.GetAsync("/index/api?t=caps&apikey=wrong-key", TestContext.Current.CancellationToken);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("error", content);
        Assert.Contains("100", content);
    }
}
