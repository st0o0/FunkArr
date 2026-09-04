namespace FunkArr.MetadataResolver;

internal static class LevenshteinDistance
{
    public static float Similarity(string a, string b)
    {
        a = Normalize(a);
        b = Normalize(b);

        if (a.Length == 0 && b.Length == 0) return 1.0f;
        if (a.Length == 0 || b.Length == 0) return 0.0f;

        var maxLen = Math.Max(a.Length, b.Length);
        var distance = Compute(a, b);
        return 1.0f - (float)distance / maxLen;
    }

    private static int Compute(string a, string b)
    {
        var m = a.Length;
        var n = b.Length;
        var prev = new int[n + 1];
        var curr = new int[n + 1];

        for (var j = 0; j <= n; j++) prev[j] = j;

        for (var i = 1; i <= m; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= n; j++)
            {
                var cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                curr[j] = Math.Min(Math.Min(
                    prev[j] + 1,
                    curr[j - 1] + 1),
                    prev[j - 1] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[n];
    }

    private static string Normalize(string input) => input
        .Replace("ä", "ae", StringComparison.Ordinal)
        .Replace("ö", "oe", StringComparison.Ordinal)
        .Replace("ü", "ue", StringComparison.Ordinal)
        .Replace("Ä", "Ae", StringComparison.Ordinal)
        .Replace("Ö", "Oe", StringComparison.Ordinal)
        .Replace("Ü", "Ue", StringComparison.Ordinal)
        .Replace("ß", "ss", StringComparison.Ordinal)
        .Trim();
}
