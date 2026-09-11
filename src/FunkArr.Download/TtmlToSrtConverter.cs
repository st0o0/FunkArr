using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FunkArr.Download;

internal static partial class TtmlToSrtConverter
{
    private static readonly XNamespace _tt = "http://www.w3.org/ns/ttml";

    public static string Convert(string ttml)
    {
        var doc = XDocument.Parse(ttml);
        var paragraphs = doc.Descendants(_tt + "p")
            .Concat(doc.Descendants("p"))
            .Where(p => p.Attribute("begin") is not null
                        && (p.Attribute("end") is not null || p.Attribute("dur") is not null))
            .ToList();

        var sb = new StringBuilder();
        var index = 1;

        foreach (var p in paragraphs)
        {
            var begin = ParseTimestamp(p.Attribute("begin")!.Value);
            var end = p.Attribute("end") is { } endAttr
                ? ParseTimestamp(endAttr.Value)
                : begin + ParseTimestamp(p.Attribute("dur")!.Value);
            var text = ExtractText(p);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.Append(index++);
            sb.Append('\n');
            sb.Append(FormatSrtTimestamp(begin));
            sb.Append(" --> ");
            sb.Append(FormatSrtTimestamp(end));
            sb.Append('\n');
            sb.Append(text);
            sb.Append("\n\n");
        }

        return sb.ToString();
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

    private static string FormatSrtTimestamp(TimeSpan ts) =>
        $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";

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
                case XElement child when child.Name.LocalName == "br":
                    sb.Append('\n');
                    break;
                case XElement child when child.Name.LocalName is "span" or "p":
                    ExtractTextNodes(child, sb);
                    break;
            }
        }
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrEntityRegex();
}
