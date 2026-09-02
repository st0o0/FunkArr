using System.Xml.Linq;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NewznabXmlTests
{
    [Fact]
    public void Caps_xml_contains_required_elements()
    {
        var xml = IndexerApiEndpoints.Serialize(new Caps());
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Equal("caps", root.Name.LocalName);
        Assert.NotNull(root.Element("server"));
        Assert.NotNull(root.Element("limits"));
        Assert.NotNull(root.Element("registration"));
        Assert.NotNull(root.Element("searching"));
        Assert.NotNull(root.Element("categories"));
    }

    [Fact]
    public void Caps_declares_server_element()
    {
        var xml = IndexerApiEndpoints.Serialize(new Caps());
        var doc = XDocument.Parse(xml);
        var server = doc.Root!.Element("server")!;

        Assert.Equal("FunkArr", server.Attribute("title")!.Value);
    }

    [Fact]
    public void Caps_declares_search_types()
    {
        var xml = IndexerApiEndpoints.Serialize(new Caps());
        var doc = XDocument.Parse(xml);
        var searching = doc.Root!.Element("searching")!;

        var search = searching.Element("search")!;
        Assert.Equal("yes", search.Attribute("available")!.Value);
        Assert.Equal("q", search.Attribute("supportedParams")!.Value);

        var tvSearch = searching.Element("tv-search")!;
        Assert.Equal("yes", tvSearch.Attribute("available")!.Value);
        Assert.Contains("tvdbid", tvSearch.Attribute("supportedParams")!.Value);

        var movieSearch = searching.Element("movie-search")!;
        Assert.Equal("yes", movieSearch.Attribute("available")!.Value);
        Assert.Contains("imdbid", movieSearch.Attribute("supportedParams")!.Value);
        Assert.Contains("tmdbid", movieSearch.Attribute("supportedParams")!.Value);

        var audioSearch = searching.Element("audio-search")!;
        Assert.Equal("no", audioSearch.Attribute("available")!.Value);

        var bookSearch = searching.Element("book-search")!;
        Assert.Equal("no", bookSearch.Attribute("available")!.Value);
    }

    [Fact]
    public void Caps_categories_declares_tv_and_movies()
    {
        var xml = IndexerApiEndpoints.Serialize(new Caps());
        var doc = XDocument.Parse(xml);
        var categories = doc.Root!.Element("categories")!.Elements("category").ToList();

        Assert.Equal(2, categories.Count);
        var movies = categories.First(c => c.Attribute("id")!.Value == "2000");
        Assert.Equal("Movies", movies.Attribute("name")!.Value);
        var movieSubs = movies.Elements("subcat").ToList();
        Assert.Contains(movieSubs, s => s.Attribute("id")!.Value == "2030");
        Assert.Contains(movieSubs, s => s.Attribute("id")!.Value == "2040");

        var tv = categories.First(c => c.Attribute("id")!.Value == "5000");
        Assert.Equal("TV", tv.Attribute("name")!.Value);
        var tvSubs = tv.Elements("subcat").ToList();
        Assert.Contains(tvSubs, s => s.Attribute("id")!.Value == "5030");
        Assert.Contains(tvSubs, s => s.Attribute("id")!.Value == "5040");
    }

    [Fact]
    public void Caps_limits()
    {
        var xml = IndexerApiEndpoints.Serialize(new Caps());
        var doc = XDocument.Parse(xml);
        var limits = doc.Root!.Element("limits")!;

        Assert.Equal("500", limits.Attribute("max")!.Value);
        Assert.Equal("100", limits.Attribute("default")!.Value);
    }

    [Fact]
    public void Empty_rss_has_correct_structure()
    {
        var xml = IndexerApiEndpoints.Serialize(new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = 0, Total = 0 },
                Items = [],
            },
        });
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Equal("rss", root.Name.LocalName);
        var channel = root.Element("channel")!;
        Assert.Equal("FunkArr", channel.Element("title")!.Value);

        XNamespace ns = NewznabNamespace.Uri;
        var response = channel.Element(ns + "response")!;
        Assert.Equal("0", response.Attribute("offset")!.Value);
        Assert.Equal("0", response.Attribute("total")!.Value);
    }

    [Fact]
    public void Rss_with_item_serializes_correctly()
    {
        var rss = new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = 0, Total = 1 },
                Items =
                [
                    new Item
                    {
                        Title = "Tatort.S01E05.GERMAN.720p.WEB.h264",
                        Guid = new ItemGuid { Value = "https://example.com#720p" },
                        Link = "https://example.com/video.mp4",
                        PubDate = "Sat, 01 Jun 2024 20:00:00 GMT",
                        Category = "TV > HD",
                        Description = "Test episode",
                        Enclosure = new Enclosure
                        {
                            Url = "/index/api/nzb?url=dGVzdA==&title=dGVzdA==",
                            Length = 1500000000,
                        },
                        Attributes =
                        [
                            new NewznabAttribute { Name = "category", Value = "5000" },
                            new NewznabAttribute { Name = "category", Value = "5040" },
                        ],
                    },
                ],
            },
        };

        var xml = IndexerApiEndpoints.Serialize(rss);
        var doc = XDocument.Parse(xml);
        var items = doc.Root!.Element("channel")!.Elements("item").ToList();

        Assert.Single(items);
        Assert.Equal("Tatort.S01E05.GERMAN.720p.WEB.h264", items[0].Element("title")!.Value);

        var enclosure = items[0].Element("enclosure")!;
        Assert.Equal("application/x-nzb", enclosure.Attribute("type")!.Value);
        Assert.Equal("1500000000", enclosure.Attribute("length")!.Value);
    }
}
