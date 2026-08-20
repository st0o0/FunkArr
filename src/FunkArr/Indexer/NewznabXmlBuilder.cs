using System.Globalization;
using System.Text;
using System.Xml;
using FunkArr.Shared.Models;

namespace FunkArr.Indexer;

public static class NewznabXmlBuilder
{
    private const string NewznabNs = "http://www.newznab.com/DTD/2010/feeds/attributes/";

    public static string BuildCapsResponse(string baseUrl)
    {
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, WriterSettings());

        writer.WriteStartDocument();
        writer.WriteStartElement("caps");

        writer.WriteStartElement("server");
        writer.WriteAttributeString("version", "1.0");
        writer.WriteAttributeString("title", "FunkArr");
        writer.WriteEndElement();

        writer.WriteStartElement("searching");
        WriteSearchCap(writer, "search", "yes");
        WriteSearchCap(writer, "tv-search", "yes", "tvdbid");
        WriteSearchCap(writer, "movie-search", "yes", "imdbid");
        writer.WriteEndElement();

        writer.WriteStartElement("categories");
        WriteCategory(writer, "5000", "TV");
        WriteCategory(writer, "5040", "TV/HD");
        WriteCategory(writer, "5050", "TV/SD");
        WriteCategory(writer, "2000", "Movies");
        WriteCategory(writer, "2040", "Movies/HD");
        WriteCategory(writer, "2050", "Movies/SD");
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.Flush();
        return sw.ToString();
    }

    public static string BuildSearchResponse(IReadOnlyList<NewznabResult> results)
    {
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, WriterSettings());

        writer.WriteStartDocument();
        writer.WriteStartElement("rss");
        writer.WriteAttributeString("version", "2.0");
        writer.WriteAttributeString("xmlns", "newznab", null, NewznabNs);

        writer.WriteStartElement("channel");
        writer.WriteElementString("title", "FunkArr");

        writer.WriteStartElement("newznab", "response", NewznabNs);
        writer.WriteAttributeString("offset", "0");
        writer.WriteAttributeString("total", results.Count.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();

        foreach (var result in results)
        {
            writer.WriteStartElement("item");
            writer.WriteElementString("title", result.Title);
            writer.WriteElementString("guid", result.Guid);
            writer.WriteElementString("link", result.DownloadUrl);
            writer.WriteElementString("pubDate", result.PublishDate.ToString("R"));

            writer.WriteStartElement("enclosure");
            writer.WriteAttributeString("url", result.DownloadUrl);
            writer.WriteAttributeString("length", result.SizeBytes.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("type", "application/x-nzb");
            writer.WriteEndElement();

            WriteNewznabAttr(writer, "category", result.Category);
            WriteNewznabAttr(writer, "size", result.SizeBytes.ToString(CultureInfo.InvariantCulture));
            WriteNewznabAttr(writer, "language", "German");

            if (result.QualityInfo is { } qi)
            {
                WriteNewznabAttr(writer, "video", qi.Codec);

                if (qi.ProbeSource != ProbeSource.Estimated)
                {
                    var resStr = $"{qi.Resolution.Height}p";
                    WriteNewznabAttr(writer, "resolution", resStr);
                }
            }

            if (result.TvdbId is > 0)
            {
                WriteNewznabAttr(writer, "tvdbid", result.TvdbId.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (result.Season is not null)
            {
                WriteNewznabAttr(writer, "season", result.Season.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (result.Episode is not null)
            {
                WriteNewznabAttr(writer, "episode", result.Episode.Value.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Flush();
        return sw.ToString();
    }

    public static string BuildErrorResponse(int code, string description)
    {
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, WriterSettings());

        writer.WriteStartDocument();
        writer.WriteStartElement("error");
        writer.WriteAttributeString("code", code.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("description", description);
        writer.WriteEndElement();
        writer.Flush();
        return sw.ToString();
    }

    public static string BuildReleaseTitle(string showName, int season, int episode, QualityTier quality, string codec = "h264")
    {
        var sanitized = showName.Replace(' ', '.');
        var qualityStr = QualityString(quality);
        return $"{sanitized}.S{season:D2}E{episode:D2}.GERMAN.{qualityStr}.WEB.{codec}-FA";
    }

    public static string BuildMovieReleaseTitle(string movieName, int year, QualityTier quality, string codec = "h264")
    {
        var sanitized = movieName.Replace(' ', '.');
        var qualityStr = QualityString(quality);
        return $"{sanitized}.{year}.GERMAN.{qualityStr}.WEB.{codec}-FA";
    }

    internal static string QualityString(QualityTier quality) => quality switch
    {
        QualityTier.HD1080 => "1080p",
        QualityTier.HD720 => "720p",
        _ => "480p",
    };

    private static XmlWriterSettings WriterSettings() => new()
    {
        Indent = true,
        Encoding = new UTF8Encoding(false),
        OmitXmlDeclaration = false,
    };

    private static void WriteSearchCap(XmlWriter writer, string type, string available, string? supportedParams = null)
    {
        writer.WriteStartElement(type);
        writer.WriteAttributeString("available", available);
        if (supportedParams is not null)
        {
            writer.WriteAttributeString("supportedParams", supportedParams);
        }

        writer.WriteEndElement();
    }

    private static void WriteCategory(XmlWriter writer, string id, string name)
    {
        writer.WriteStartElement("category");
        writer.WriteAttributeString("id", id);
        writer.WriteAttributeString("name", name);
        writer.WriteEndElement();
    }

    private static void WriteNewznabAttr(XmlWriter writer, string name, string value)
    {
        writer.WriteStartElement("attr", NewznabNs);
        writer.WriteAttributeString("name", name);
        writer.WriteAttributeString("value", value);
        writer.WriteEndElement();
    }
}
