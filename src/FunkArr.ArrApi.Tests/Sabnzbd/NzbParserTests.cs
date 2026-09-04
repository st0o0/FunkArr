using System.Xml.Serialization;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class NzbParserTests
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
    public void Parse_extracts_title_and_url_from_head_meta()
    {
        const string nzb = """
                           <?xml version="1.0" encoding="utf-16"?>
                           <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
                             <head>
                               <meta type="title">Tatort S01E05</meta>
                               <meta type="url">https://example.com/video.mp4</meta>
                             </head>
                             <file post_id="1">
                               <groups><group>a.b.mediathek</group></groups>
                               <segments><segment number="1">FunkArr@news.example.com</segment></segments>
                             </file>
                           </nzb>
                           """;

        var (title, url) = Parse(nzb);

        Assert.Equal("Tatort S01E05", title);
        Assert.Equal("https://example.com/video.mp4", url);
    }

    [Fact]
    public void Parse_returns_null_for_missing_head()
    {
        const string nzb = """
                           <?xml version="1.0" encoding="utf-16"?>
                           <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
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
    public void Parse_returns_null_for_missing_url_meta()
    {
        const string nzb = """
                           <?xml version="1.0" encoding="utf-16"?>
                           <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
                             <head>
                               <meta type="title">Test</meta>
                             </head>
                             <file post_id="1">
                               <groups><group>a.b.mediathek</group></groups>
                               <segments><segment number="1">FunkArr@news.example.com</segment></segments>
                             </file>
                           </nzb>
                           """;

        var (title, url) = Parse(nzb);

        Assert.Equal("Test", title);
        Assert.Null(url);
    }

    [Fact]
    public void Parse_returns_null_for_invalid_xml()
    {
        var (title, url) = Parse("not xml at all");

        Assert.Null(title);
        Assert.Null(url);
    }
}
