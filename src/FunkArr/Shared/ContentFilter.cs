namespace FunkArr.Shared;

public static class ContentFilter
{
    private static readonly string[] AccessibilityKeywords =
    [
        "Audiodeskription",
        "Gebärdensprache",
        "Gebardensprache",
        "klare Sprache",
        "Hörfassung",
    ];

    private static readonly string[] ContentTypeKeywords =
    [
        "Trailer",
        "Vorschau",
        "Teaser",
    ];

    public static bool IsAccessibilityVariant(string title)
    {
        foreach (var keyword in AccessibilityKeywords)
        {
            if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldSkipAccessibilityOnly(string title) =>
        IsAccessibilityVariant(title);

    public static bool ShouldSkip(string title, string topic)
    {
        foreach (var keyword in AccessibilityKeywords)
        {
            if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var keyword in ContentTypeKeywords)
        {
            if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                topic.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
