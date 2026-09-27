using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FunkArr.Download;

internal sealed partial class TtmlFormat : ISubtitleFormat
{
    private static readonly XNamespace _tt = "http://www.w3.org/ns/ttml";
    private static readonly XNamespace _ebuttm = "urn:ebu:tt:metadata";
    private static readonly TimeSpan _offsetThreshold = TimeSpan.FromMinutes(30);

    private XDocument? _cached;

    public string Name => "TTML";

    public bool CanParse(string content)
    {
        try
        {
            var doc = XDocument.Parse(content);
            if (doc.Root?.Name.LocalName == "tt")
            {
                _cached = doc;
                return true;
            }
        }
        catch (System.Xml.XmlException)
        {
        }

        return false;
    }

    public List<SubtitleCue> Parse(string content)
    {
        var doc = _cached ?? XDocument.Parse(content);
        _cached = null;

        var paragraphs = doc.Descendants(_tt + "p")
            .Concat(doc.Descendants("p"))
            .Where(p => p.Attribute("begin") is not null
                        && (p.Attribute("end") is not null || p.Attribute("dur") is not null))
            .ToList();

        var offset = DetectOffset(doc, paragraphs);
        var cues = new List<SubtitleCue>();

        foreach (var p in paragraphs)
        {
            var begin = ParseTimestamp(p.Attribute("begin")!.Value) - offset;
            if (begin < TimeSpan.Zero) begin = TimeSpan.Zero;

            var end = p.Attribute("end") is { } endAttr
                ? ParseTimestamp(endAttr.Value) - offset
                : begin + ParseTimestamp(p.Attribute("dur")!.Value);
            if (end < TimeSpan.Zero) end = TimeSpan.Zero;

            var text = ExtractText(p);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            cues.Add(new SubtitleCue(begin, end, text));
        }

        return cues;
    }

    private static TimeSpan DetectOffset(XDocument doc, List<XElement> paragraphs)
    {
        var startOfProgramme = doc.Descendants(_ebuttm + "documentStartOfProgramme").FirstOrDefault();
        if (startOfProgramme is not null)
        {
            var offset = ParseTimestamp(startOfProgramme.Value.Trim());
            if (offset > TimeSpan.Zero)
                return offset;
        }

        if (paragraphs.Count == 0)
            return TimeSpan.Zero;

        var minBegin = paragraphs
            .Select(p => ParseTimestamp(p.Attribute("begin")!.Value))
            .Min();

        return minBegin > _offsetThreshold ? minBegin : TimeSpan.Zero;
    }

    internal static TimeSpan ParseTimestamp(string value)
    {
        if (value.EndsWith('s') && double.TryParse(value.AsSpan(0, value.Length - 1), CultureInfo.InvariantCulture, out var seconds))
            return TimeSpan.FromSeconds(seconds);

        if (TimeSpan.TryParseExact(value, [@"hh\:mm\:ss\.FFF", @"hh\:mm\:ss\,FFF", @"hh\:mm\:ss"], CultureInfo.InvariantCulture, out var ts))
            return ts;

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out ts))
            return ts;

        return TimeSpan.Zero;
    }

    private static string ExtractText(XElement p)
    {
        var sb = new StringBuilder();
        ExtractTextNodes(p, sb);
        var text = sb.ToString().Trim();
        return BrEntityRegex().Replace(text, "\n");
    }

    private static void ExtractTextNodes(XElement element, StringBuilder sb)
    {
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    sb.Append(text.Value);
                    break;
                case XElement { Name.LocalName: "br" }:
                    sb.Append('\n');
                    break;
                case XElement { Name.LocalName: "span" or "p" } child:
                    ExtractTextNodes(child, sb);
                    break;
            }
        }
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrEntityRegex();
}
