using System.Text;

namespace FunkArr.Indexer;

public static class FakeNzbBuilder
{
    public static string BuildFakeNzbUrl(string baseUrl, string downloadUrl, string title)
    {
        var encodedUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(downloadUrl));
        var encodedTitle = Convert.ToBase64String(Encoding.UTF8.GetBytes(title));
        return $"{baseUrl}/index/api/fake_nzb?url={Uri.EscapeDataString(encodedUrl)}&title={Uri.EscapeDataString(encodedTitle)}";
    }

    public static string BuildFakeNzbXml(string downloadUrl, string title, string? subtitleUrl = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<!DOCTYPE nzb PUBLIC "-//newzBin//DTD NZB 1.1//EN" "http://www.newzbin.com/DTD/nzb/nzb-1.1.dtd">""");
        sb.AppendLine("""<nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">""");
        sb.AppendLine($"<!-- FUNKARR_URL:{Convert.ToBase64String(Encoding.UTF8.GetBytes(downloadUrl))} -->");
        sb.AppendLine($"<!-- FUNKARR_TITLE:{Convert.ToBase64String(Encoding.UTF8.GetBytes(title))} -->");
        if (subtitleUrl is not null)
        {
            sb.AppendLine($"<!-- FUNKARR_SUBTITLE:{Convert.ToBase64String(Encoding.UTF8.GetBytes(subtitleUrl))} -->");
        }

        sb.AppendLine("""<file poster="FunkArr" subject="[1/1] &quot;""" + title + """&quot; (1/1)">""");
        sb.AppendLine("""<groups><group>alt.binaries.multimedia</group></groups>""");
        sb.AppendLine("""<segments><segment bytes="1" number="1">funkarr@funkarr</segment></segments>""");
        sb.AppendLine("</file>");
        sb.AppendLine("</nzb>");
        return sb.ToString();
    }

    public static (string? url, string? title, string? subtitleUrl) ParseFakeNzb(string nzbContent)
    {
        string? url = null, title = null, subtitleUrl = null;

        foreach (var line in nzbContent.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (TryExtractComment(trimmed, "FUNKARR_URL:", out var urlValue))
            {
                url = Encoding.UTF8.GetString(Convert.FromBase64String(urlValue));
            }
            else if (TryExtractComment(trimmed, "FUNKARR_TITLE:", out var titleValue))
            {
                title = Encoding.UTF8.GetString(Convert.FromBase64String(titleValue));
            }
            else if (TryExtractComment(trimmed, "FUNKARR_SUBTITLE:", out var subValue))
            {
                subtitleUrl = Encoding.UTF8.GetString(Convert.FromBase64String(subValue));
            }
        }

        return (url, title, subtitleUrl);
    }

    private static bool TryExtractComment(ReadOnlySpan<char> line, string marker, out string value)
    {
        value = string.Empty;
        var prefix = $"<!-- {marker}";
        var suffix = " -->";

        if (!line.StartsWith(prefix) || !line.EndsWith(suffix))
        {
            return false;
        }

        var start = prefix.Length;
        var end = line.Length - suffix.Length;
        if (end <= start)
        {
            return false;
        }

        value = line[start..end].ToString();
        return true;
    }
}
