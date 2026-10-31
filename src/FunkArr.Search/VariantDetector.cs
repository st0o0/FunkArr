using System.Text.RegularExpressions;
using FunkArr.Messages;

namespace FunkArr.Search;

internal static class VariantDetector
{
    private const RegexOptions _options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(100);

    private static readonly (MediathekVariant Variant, Regex Pattern)[] _patterns =
    [
        (MediathekVariant.AudioDescription,
            new Regex(@"\baudio-?deskription\b|\bhörfassung\b|\(\s*AD\s*\)|\bmit\s+AD\b", _options, _timeout)),
        (MediathekVariant.SignLanguage,
            new Regex(@"\bgebärdensprache\b|\bDGS\b", _options, _timeout)),
        (MediathekVariant.PlainLanguage,
            new Regex(@"\b(klare|leichte|einfache)r?\s+sprache\b", _options, _timeout)),
        (MediathekVariant.OriginalVersion,
            new Regex(@"\boriginal(version|fassung)\b|\boriginal\s+mit\s+untertiteln?\b|\bOV\b|\bOmU\b", _options, _timeout))
    ];

    internal static MediathekVariant Detect(string? title, string? topic)
    {
        foreach (var (variant, pattern) in _patterns)
        {
            if (Matches(pattern, title) || Matches(pattern, topic))
            {
                return variant;
            }
        }

        return MediathekVariant.Standard;
    }

    private static bool Matches(Regex pattern, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        try
        {
            return pattern.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
