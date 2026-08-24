using System.Globalization;
using System.Text.RegularExpressions;

namespace FunkArr.Search.Matching;

public static partial class DateMatcher
{
    public static bool MatchesAirDate(string title, string? description, DateTimeOffset expectedAirDate)
    {
        var candidates = ExtractDates(title);
        if (description is not null)
        {
            candidates.AddRange(ExtractDates(description));
        }

        if (candidates.Count == 0)
        {
            return true;
        }

        var expectedDate = expectedAirDate.Date;
        return candidates.Exists(d => d == expectedDate);
    }

    private static List<DateTime> ExtractDates(string text)
    {
        var dates = new List<DateTime>();

        foreach (Match match in GermanDatePattern().Matches(text))
        {
            if (DateTime.TryParseExact(
                match.Value, "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                dates.Add(date);
            }
        }

        foreach (Match match in IsoDatePattern().Matches(text))
        {
            if (DateTime.TryParseExact(
                match.Value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    [GeneratedRegex(@"\d{2}\.\d{2}\.\d{4}")]
    private static partial Regex GermanDatePattern();

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}")]
    private static partial Regex IsoDatePattern();
}
