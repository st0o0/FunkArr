using System.Globalization;
using System.Text.RegularExpressions;

namespace FunkArr.MatchMagic;

public sealed record Filter(string Field, FilterOp Op, string Value)
{
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(100);

    public bool Evaluate(MediaItem item)
    {
        var fieldValue = ResolveField(item);
        if (fieldValue is null)
        {
            return false;
        }

        return Op switch
        {
            FilterOp.GreaterThan => CompareNumeric(fieldValue, Value) > 0,
            FilterOp.LessThan => CompareNumeric(fieldValue, Value) < 0,
            FilterOp.Eq => string.Equals(fieldValue, Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Contains => fieldValue.Contains(Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.NotContains => !fieldValue.Contains(Value, StringComparison.OrdinalIgnoreCase),
            FilterOp.Regex => EvaluateRegex(fieldValue, Value),
            _ => false,
        };
    }

    private string? ResolveField(MediaItem item) => Field switch
    {
        "duration" => (item.Duration / 60).ToString(CultureInfo.InvariantCulture),
        "title" => item.Title,
        "description" => item.Description,
        "topic" => item.Topic,
        "channel" => item.Channel,
        "timestamp" => item.Timestamp.ToString(CultureInfo.InvariantCulture),
        _ => null,
    };

    private static int CompareNumeric(string left, string right)
    {
        if (double.TryParse(left, CultureInfo.InvariantCulture, out var l) &&
            double.TryParse(right, CultureInfo.InvariantCulture, out var r))
        {
            return l.CompareTo(r);
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateRegex(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.None, _regexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
