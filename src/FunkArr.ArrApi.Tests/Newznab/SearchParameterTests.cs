using System.Xml.Linq;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using Xunit;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class SearchParameterTests
{
    [Fact]
    public void Rss_with_offset_reflects_in_response()
    {
        var rss = new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = 10, Total = 0 },
                Items = [],
            },
        };
        var xml = IndexerApiEndpoints.Serialize(rss);
        var doc = XDocument.Parse(xml);

        XNamespace ns = NewznabNamespace.Uri;
        var response = doc.Root!.Element("channel")!.Element(ns + "response")!;

        Assert.Equal("10", response.Attribute("offset")!.Value);
        Assert.Equal("0", response.Attribute("total")!.Value);
    }

    [Fact]
    public void Rss_default_offset_is_zero()
    {
        var rss = new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = 0, Total = 0 },
                Items = [],
            },
        };
        var xml = IndexerApiEndpoints.Serialize(rss);
        var doc = XDocument.Parse(xml);

        XNamespace ns = NewznabNamespace.Uri;
        var response = doc.Root!.Element("channel")!.Element(ns + "response")!;

        Assert.Equal("0", response.Attribute("offset")!.Value);
    }
}
