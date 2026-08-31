using System.Text;
using System.Xml.Linq;
using FunkArr.ArrApi;
using FunkArr.ArrApi.Newznab;
using Xunit;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NzbGeneratorTests
{
    [Fact]
    public void Nzb_serialization_creates_valid_xml_with_head_meta()
    {
        var nzb = new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = "Tatort S01E05" },
                    new NzbMeta { Type = "url", Value = "https://example.com/video.mp4" },
                ],
            },
        };

        var xml = IndexerApiEndpoints.Serialize(nzb);
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Equal("nzb", root.Name.LocalName);
        Assert.NotNull(root.Element("head"));
        Assert.NotNull(root.Element("file"));

        var metas = root.Element("head")!.Elements("meta").ToList();
        var titleMeta = metas.First(m => m.Attribute("type")!.Value == "title");
        var urlMeta = metas.First(m => m.Attribute("type")!.Value == "url");

        Assert.Equal("Tatort S01E05", titleMeta.Value);
        Assert.Equal("https://example.com/video.mp4", urlMeta.Value);
    }

    [Fact]
    public void Nzb_serialization_contains_file_with_groups_and_segments()
    {
        var nzb = new Nzb
        {
            Head = new NzbHead
            {
                Metas =
                [
                    new NzbMeta { Type = "title", Value = "Test" },
                    new NzbMeta { Type = "url", Value = "https://example.com/test.mp4" },
                ],
            },
        };

        var xml = IndexerApiEndpoints.Serialize(nzb);
        var doc = XDocument.Parse(xml);
        var file = doc.Root!.Element("file")!;

        Assert.Equal("1", file.Attribute("post_id")!.Value);
        Assert.NotNull(file.Element("groups"));
        Assert.NotNull(file.Element("segments"));
        Assert.Equal("a.b.mediathek", file.Element("groups")!.Element("group")!.Value);
    }

    [Fact]
    public void Base64_decode_roundtrips()
    {
        var original = "hello";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(original));

        Assert.Equal(original, Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
    }
}
