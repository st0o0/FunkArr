using System.Text;

namespace FunkArr.ArrApi;

public static class QueryStringRedactor
{
    public static string RedactApiKey(string queryString)
    {
        var span = queryString.AsSpan();
        var result = new StringBuilder(queryString.Length);
        var pos = 0;

        while (pos < span.Length)
        {
            var remaining = span[pos..];
            var keyIndex = remaining.IndexOf("apikey=", StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0)
            {
                result.Append(remaining);
                break;
            }

            result.Append(remaining[..(keyIndex + "apikey=".Length)]);
            pos += keyIndex + "apikey=".Length;

            var valueEnd = span[pos..].IndexOf('&');
            if (valueEnd < 0)
            {
                result.Append("[REDACTED]");
                pos = span.Length;
            }
            else
            {
                result.Append("[REDACTED]");
                pos += valueEnd;
            }
        }

        return result.ToString();
    }
}
