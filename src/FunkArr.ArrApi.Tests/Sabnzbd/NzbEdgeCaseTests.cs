using System.Xml.Serialization;
using FunkArr.ArrApi.Newznab.Models;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class NzbEdgeCaseTests
{
    private static readonly XmlSerializer _serializer = new(typeof(Nzb));

    private static (string? Title, string? Url) Parse(string nzbContent)
    {
        Nzb? nzb;
        try
        {
            using var reader = new StringReader(nzbContent);
            nzb = _serializer.Deserialize(reader) as Nzb;
        }
        catch (InvalidOperationException)
        {
            return (null, null);
        }

        var title = nzb?.Head?.Metas.FirstOrDefault(m => m.Type == "title")?.Value;
        var url = nzb?.Head?.Metas.FirstOrDefault(m => m.Type == "url")?.Value;
        return (title, url);
    }

    [Fact]
    public void Empty_string_returns_null()
    {
        var (title, url) = Parse("");

        Assert.Null(title);
        Assert.Null(url);
    }

    [Fact]
    public void Whitespace_only_returns_null()
    {
        var (title, url) = Parse("   \n\t  ");

        Assert.Null(title);
        Assert.Null(url);
    }

    [Fact]
    public void Html_content_returns_null()
    {
        var (title, url) = Parse("<html><body>Not an NZB</body></html>");

        Assert.Null(title);
        Assert.Null(url);
    }

    [Fact]
    public void Nzb_with_empty_head_returns_null_title_and_url()
    {
        const string nzb = """
                           <?xml version="1.0" encoding="utf-16"?>
                           <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
                             <head></head>
                             <file post_id="1">
                               <groups><group>a.b.mediathek</group></groups>
                               <segments><segment number="1">FunkArr@news.example.com</segment></segments>
                             </file>
                           </nzb>
                           """;

        var (title, url) = Parse(nzb);

        Assert.Null(title);
        Assert.Null(url);
    }

    [Fact]
    public void Nzb_with_unknown_meta_type_returns_null_for_title()
    {
        const string nzb = """
                           <?xml version="1.0" encoding="utf-16"?>
                           <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
                             <head>
                               <meta type="unknown">value</meta>
                             </head>
                             <file post_id="1">
                               <groups><group>a.b.mediathek</group></groups>
                               <segments><segment number="1">FunkArr@news.example.com</segment></segments>
                             </file>
                           </nzb>
                           """;

        var (title, url) = Parse(nzb);

        Assert.Null(title);
        Assert.Null(url);
    }

    [Fact]
    public void Nzb_with_empty_meta_value_returns_empty_string()
    {
        const string nzb = """
                           <?xml version="1.0" encoding="utf-16"?>
                           <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
                             <head>
                               <meta type="title"></meta>
                               <meta type="url"></meta>
                             </head>
                             <file post_id="1">
                               <groups><group>a.b.mediathek</group></groups>
                               <segments><segment number="1">FunkArr@news.example.com</segment></segments>
                             </file>
                           </nzb>
                           """;

        var (title, url) = Parse(nzb);

        Assert.Equal("", title);
        Assert.Equal("", url);
    }
}
