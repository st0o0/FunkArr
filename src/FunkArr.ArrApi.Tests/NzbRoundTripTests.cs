using System.Xml.Serialization;
using FunkArr.ArrApi;
using FunkArr.ArrApi.Newznab;
using Xunit;

namespace FunkArr.ArrApi.Tests;

public sealed class NzbRoundTripTests
{
    private static readonly XmlSerializer _serializer = new(typeof(Nzb));

    [Fact]
    public void Generate_then_parse_returns_same_title_and_url()
    {
        var title = "Tatort S01E05";
        var url = "https://example.com/video.mp4";

        var nzbXml = GenerateNzb(title, url);
        var (parsedTitle, parsedUrl) = ParseNzb(nzbXml);

        Assert.Equal(title, parsedTitle);
        Assert.Equal(url, parsedUrl);
    }

    [Fact]
    public void Generate_then_parse_with_special_characters()
    {
        var title = "Tatort: Münchner Nächte & Co.";
        var url = "https://example.com/video.mp4?quality=720&lang=de";

        var nzbXml = GenerateNzb(title, url);
        var (parsedTitle, parsedUrl) = ParseNzb(nzbXml);

        Assert.Equal(title, parsedTitle);
        Assert.Equal(url, parsedUrl);
    }

    private static string GenerateNzb(string title, string url) =>
        IndexerApiEndpoints.Serialize(new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = title },
                    new NzbMeta { Type = "url", Value = url },
                ],
            },
        });

    private static (string? Title, string? Url) ParseNzb(string nzbXml)
    {
        using var reader = new StringReader(nzbXml);
        var nzb = _serializer.Deserialize(reader) as Nzb;
        var title = nzb?.Head?.Metas.FirstOrDefault(m => m.Type == "title")?.Value;
        var url = nzb?.Head?.Metas.FirstOrDefault(m => m.Type == "url")?.Value;
        return (title, url);
    }
}
