using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunkArr.MatchMagic;

public sealed record RuleSet(
    string Topic,
    IReadOnlyList<string>? Aliases = null,
    MediaRef? Media = null,
    float? Confidence = null,
    IReadOnlyList<Rule>? Rules = null,
    bool Standalone = false,
    IReadOnlyList<string>? Disable = null)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter<MatchStrategy>(), new JsonStringEnumConverter<FilterOp>(), new JsonStringEnumConverter<MediaType>() },
    };

    public IReadOnlyList<Rule> EffectiveRules => Rules ?? [];

    public static RuleSet FromJson(string json) =>
        JsonSerializer.Deserialize<RuleSet>(json, _jsonOptions)
        ?? throw new JsonException("Failed to deserialize RuleSet");

    public string ToJson() => JsonSerializer.Serialize(this, _jsonOptions);

    public IReadOnlyList<MatchResult> Evaluate(IReadOnlyList<MediaItem> items)
    {
        var sortedRules = EffectiveRules.OrderBy(r => r.Priority).ToList();
        var results = new List<MatchResult>();
        var confidence = Confidence ?? 0f;

        foreach (var item in items)
        {
            foreach (var rule in sortedRules)
            {
                var result = rule.Match(item, confidence);
                if (result is not null)
                {
                    results.Add(result);
                    break;
                }
            }
        }

        return results;
    }
}
