using System.Text;
using System.Text.RegularExpressions;

namespace FunkArr.RuleSet;

public static partial class TopicSlugGenerator
{
    public static string Generate(string topic)
    {
        var sb = new StringBuilder(topic.Length);
        foreach (var c in topic)
        {
            var mapped = c switch
            {
                'ä' or 'Ä' => "ae",
                'ö' or 'Ö' => "oe",
                'ü' or 'Ü' => "ue",
                'ß' => "ss",
                '&' => "-und-",
                _ => null,
            };

            if (mapped is not null)
            {
                sb.Append(mapped);
            }
            else if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append('-');
            }
        }

        var slug = CollapseHyphens().Replace(sb.ToString(), "-").Trim('-');
        return slug.Length == 0 ? "unknown" : slug;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseHyphens();
}
