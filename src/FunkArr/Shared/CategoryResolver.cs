namespace FunkArr.Shared;

public static class CategoryResolver
{
    public static string Resolve(string basePath, string? category, Dictionary<string, string> categoryConfig)
    {
        if (string.IsNullOrEmpty(category))
        {
            return basePath;
        }

        if (categoryConfig.TryGetValue(category, out var configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(basePath, configured);
        }

        var sanitized = SanitizeDirectoryName(category);
        return Path.Combine(basePath, sanitized);
    }

    private static string SanitizeDirectoryName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var span = name.AsSpan();
        Span<char> buffer = stackalloc char[span.Length];
        var pos = 0;

        foreach (var c in span)
        {
            if (!invalid.Contains(c))
            {
                buffer[pos++] = c;
            }
        }

        return pos == 0 ? "_" : new string(buffer[..pos]);
    }
}
