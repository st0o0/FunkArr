using System.Text.Json;

namespace FunkArr.RuleSet;

public static class CommunityRuleSetParser
{
    public static IReadOnlyList<RuleSetFile> Parse(string json)
    {
        var upstream = JsonSerializer.Deserialize<UpstreamRuleSet[]>(json, UpstreamJsonOptions);
        if (upstream is null)
        {
            return [];
        }

        return upstream
            .GroupBy(r => r.Topic, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new RuleSetFile
                {
                    Topic = first.Topic,
                    Media = new MediaReference
                    {
                        TvdbId = first.Media?.MediaTvdbId ?? first.Media?.TvdbId,
                        ImdbId = first.Media?.MediaImdbId ?? first.Media?.ImdbId,
                        TmdbId = first.Media?.TmdbId,
                        Name = first.Media?.MediaName ?? first.Media?.Name ?? first.Topic,
                        Type = first.Media?.MediaType ?? first.Media?.Type ?? "show",
                    },
                    Source = "community",
                    Confidence = 1.0,
                    Rules = group
                        .Select(ToRule)
                        .OrderBy(r => r.Priority)
                        .ToList(),
                };
            })
            .ToList();
    }

    private static Rule ToRule(UpstreamRuleSet upstream)
    {
        var filters = ParseFilters(upstream.Filters);
        return new Rule
        {
            Priority = upstream.Priority,
            Filters = filters.Count > 0
                ? new FilterGroup { All = filters }
                : FilterGroup.Empty,
            Strategy = ParseStrategy(upstream.MatchingStrategy),
            SeasonRegex = NullIfEmpty(upstream.SeasonRegex),
            EpisodeRegex = NullIfEmpty(upstream.EpisodeRegex),
            TitleRules = ParseTitleRules(upstream.TitleRegexRules),
        };
    }

    private static IReadOnlyList<FilterNode> ParseFilters(string? filtersJson)
    {
        if (string.IsNullOrWhiteSpace(filtersJson) || filtersJson == "[]")
        {
            return [];
        }

        var upstreamFilters = JsonSerializer.Deserialize<UpstreamFilter[]>(filtersJson, UpstreamJsonOptions);
        if (upstreamFilters is null)
        {
            return [];
        }

        return upstreamFilters
            .Select(f => (FilterNode)new Filter
            {
                Field = MapFilterField(f.Attribute),
                Op = MapFilterOp(f.Type),
                Value = f.Value?.ToString() ?? string.Empty,
            })
            .ToList();
    }

    private static IReadOnlyList<TitleRule> ParseTitleRules(string? rulesJson)
    {
        if (string.IsNullOrWhiteSpace(rulesJson) || rulesJson == "[]")
        {
            return [];
        }

        var upstreamRules = JsonSerializer.Deserialize<UpstreamTitleRule[]>(rulesJson, UpstreamJsonOptions);
        if (upstreamRules is null)
        {
            return [];
        }

        return upstreamRules
            .Select(r => new TitleRule
            {
                Type = r.Type?.Equals("static", StringComparison.OrdinalIgnoreCase) == true
                    ? TitleRuleType.Static
                    : TitleRuleType.Regex,
                Field = NullIfEmpty(r.Field),
                Pattern = NullIfEmpty(r.Pattern),
                Value = NullIfEmpty(r.Value),
            })
            .ToList();
    }

    private static MatchingStrategy ParseStrategy(string? strategy) =>
        strategy switch
        {
            "SeasonAndEpisodeNumber" => MatchingStrategy.SeasonAndEpisodeNumber,
            "ItemTitleExact" => MatchingStrategy.ItemTitleExact,
            "ItemTitleIncludes" => MatchingStrategy.ItemTitleIncludes,
            "ItemTitleEqualsAirdate" => MatchingStrategy.ItemTitleEqualsAirdate,
            "ByAbsoluteEpisodeNumber" => MatchingStrategy.ByAbsoluteEpisodeNumber,
            _ => MatchingStrategy.ItemTitleIncludes,
        };

    private static string MapFilterField(string? attribute) =>
        attribute?.ToLowerInvariant() switch
        {
            "duration" => "duration",
            "title" => "title",
            "description" => "description",
            "topic" => "topic",
            "channel" => "channel",
            _ => attribute?.ToLowerInvariant() ?? "duration",
        };

    private static FilterOp MapFilterOp(string? type) =>
        type switch
        {
            "GreaterThan" => FilterOp.GreaterThan,
            "LowerThan" or "LessThan" => FilterOp.LessThan,
            "ExactMatch" => FilterOp.ExactMatch,
            "Contains" => FilterOp.Contains,
            "Regex" => FilterOp.Regex,
            "Eq" => FilterOp.Eq,
            "NotContains" => FilterOp.NotContains,
            _ => FilterOp.GreaterThan,
        };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static readonly JsonSerializerOptions UpstreamJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record UpstreamRuleSet
    {
        public int Id { get; init; }
        public string Topic { get; init; } = string.Empty;
        public int Priority { get; init; }
        public string? Filters { get; init; }
        public string? TitleRegexRules { get; init; }
        public string? EpisodeRegex { get; init; }
        public string? SeasonRegex { get; init; }
        public string? MatchingStrategy { get; init; }
        public UpstreamMedia? Media { get; init; }
    }

    private sealed record UpstreamMedia
    {
        public string? Name { get; init; }
        public string? Type { get; init; }
        public int? TvdbId { get; init; }
        public string? ImdbId { get; init; }
        public int? TmdbId { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("media_name")]
        public string? MediaName { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("media_type")]
        public string? MediaType { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("media_tvdbId")]
        public int? MediaTvdbId { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("media_imdbId")]
        public string? MediaImdbId { get; init; }
    }

    private sealed record UpstreamFilter
    {
        public string? Attribute { get; init; }
        public string? Type { get; init; }
        public object? Value { get; init; }
    }

    private sealed record UpstreamTitleRule
    {
        public string? Type { get; init; }
        public string? Field { get; init; }
        public string? Pattern { get; init; }
        public string? Value { get; init; }
    }
}
