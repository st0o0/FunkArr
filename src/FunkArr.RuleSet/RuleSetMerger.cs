using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FunkArr.Messages.Scoring;
using FilterNode = FunkArr.Messages.Scoring.FilterNode;

namespace FunkArr.RuleSet;

public static class RuleSetMerger
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static MatchingConfig? BuildFromJson(string ruleSetId, string json)
    {
        var raw = JsonSerializer.Deserialize<RawRuleSet>(json, _jsonOptions);
        if (raw is null)
        {
            return null;
        }

        var rules = TransformRules(raw.Rules ?? []);
        var confidence = raw.Confidence ?? 0f;

        return new MatchingConfig(ruleSetId, confidence, rules);
    }

    public static MatchingConfig? Build(string ruleSetId, string? communityJson, string? localJson)
    {
        var community = communityJson is not null
            ? JsonSerializer.Deserialize<RawRuleSet>(communityJson, _jsonOptions)
            : null;
        var local = localJson is not null
            ? JsonSerializer.Deserialize<RawRuleSet>(localJson, _jsonOptions)
            : null;

        var resolved = Resolve(community, local);
        if (resolved is null)
        {
            return null;
        }

        var rules = TransformRules(resolved.Rules ?? []);
        var confidence = resolved.Confidence ?? 0f;

        return new MatchingConfig(ruleSetId, confidence, rules);
    }

    public static (string Topic, string[] Aliases, int? TvdbId, string? ImdbId, int? TmdbId, string? MediaName, string? MediaType)? ExtractIdentity(
        string? communityJson, string? localJson)
    {
        var community = communityJson is not null
            ? JsonSerializer.Deserialize<RawRuleSet>(communityJson, _jsonOptions)
            : null;
        var local = localJson is not null
            ? JsonSerializer.Deserialize<RawRuleSet>(localJson, _jsonOptions)
            : null;

        var resolved = Resolve(community, local);
        if (resolved is null)
        {
            return null;
        }

        var aliases = resolved.Aliases?.ToArray() ?? [];
        var media = resolved.Media;
        return (resolved.Topic, aliases, media?.TvdbId, media?.ImdbId, media?.TmdbId, media?.Name, media?.Type);
    }

    private static readonly JsonSerializerOptions _exportOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static string? ResolveToJson(string? communityJson, string? localJson)
    {
        var community = communityJson is not null
            ? JsonSerializer.Deserialize<RawRuleSet>(communityJson, _jsonOptions)
            : null;
        var local = localJson is not null
            ? JsonSerializer.Deserialize<RawRuleSet>(localJson, _jsonOptions)
            : null;

        var resolved = Resolve(community, local);
        if (resolved is null)
        {
            return null;
        }

        var node = JsonSerializer.SerializeToNode(resolved, _exportOptions);
        if (node is JsonObject obj)
        {
            obj.Remove("standalone");
            obj.Remove("disable");
        }

        return node?.ToJsonString(_exportOptions);
    }

    private static RawRuleSet? Resolve(RawRuleSet? community, RawRuleSet? local)
    {
        if (community is null && local is null)
        {
            return null;
        }

        if (community is null)
        {
            return local;
        }

        if (local is null)
        {
            return community;
        }

        if (local.Standalone)
        {
            return local;
        }

        return Merge(community, local);
    }

    private static RawRuleSet Merge(RawRuleSet community, RawRuleSet local)
    {
        var rules = MergeRules(community.Rules ?? [], local.Rules ?? [], local.Disable);
        var aliases = MergeAliases(community.Aliases, local.Aliases);
        var confidence = local.Confidence ?? community.Confidence;
        var media = MergeMedia(community.Media, local.Media);

        return new RawRuleSet
        {
            Topic = community.Topic,
            Aliases = aliases,
            Media = media,
            Confidence = confidence,
            Rules = rules,
        };
    }

    private static RawMedia? MergeMedia(RawMedia? community, RawMedia? local)
    {
        if (community is null && local is null)
        {
            return null;
        }

        if (community is null)
        {
            return local;
        }

        if (local is null)
        {
            return community;
        }

        return new RawMedia
        {
            TvdbId = local.TvdbId ?? community.TvdbId,
            ImdbId = local.ImdbId ?? community.ImdbId,
            TmdbId = local.TmdbId ?? community.TmdbId,
            Name = local.Name ?? community.Name,
            Type = local.Type ?? community.Type,
        };
    }

    private static List<RawRule> MergeRules(
        List<RawRule> communityRules,
        List<RawRule> localRules,
        List<string>? disable)
    {
        var disabledIds = disable is { Count: > 0 }
            ? new HashSet<string>(disable, StringComparer.Ordinal)
            : null;

        var localById = new Dictionary<string, RawRule>(StringComparer.Ordinal);
        foreach (var rule in localRules)
        {
            localById[rule.Id] = rule;
        }

        var merged = new List<RawRule>();

        foreach (var rule in communityRules)
        {
            if (disabledIds is not null && disabledIds.Contains(rule.Id))
            {
                continue;
            }

            if (localById.TryGetValue(rule.Id, out var replacement))
            {
                merged.Add(replacement);
                localById.Remove(rule.Id);
            }
            else
            {
                merged.Add(rule);
            }
        }

        foreach (var rule in localRules)
        {
            if (localById.ContainsKey(rule.Id))
            {
                merged.Add(rule);
            }
        }

        merged.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        return merged;
    }

    private static List<string>? MergeAliases(List<string>? community, List<string>? local)
    {
        if (community is null or { Count: 0 } && local is null or { Count: 0 })
        {
            return null;
        }

        var set = new HashSet<string>(StringComparer.Ordinal);

        if (community is { Count: > 0 })
        {
            foreach (var alias in community)
            {
                set.Add(alias);
            }
        }

        if (local is { Count: > 0 })
        {
            foreach (var alias in local)
            {
                set.Add(alias);
            }
        }

        return set.ToList();
    }

    private static MatchingRule[] TransformRules(List<RawRule> rawRules)
    {
        var results = new List<MatchingRule>();

        foreach (var raw in rawRules)
        {
            var identification = TransformIdentification(raw);
            if (identification is null)
            {
                continue;
            }

            var filters = raw.Filters is not null
                ? TransformFilterGroup(raw.Filters)
                : null;

            results.Add(new MatchingRule(
                raw.Id,
                raw.Priority,
                raw.Confidence,
                filters,
                identification));
        }

        return results.ToArray();
    }

    private static IdentificationSpec? TransformIdentification(RawRule raw)
    {
        if (!TryParseStrategy(raw.Strategy, out var strategy))
        {
            return null;
        }

        return strategy switch
        {
            IdentificationStrategy.SeasonAndEpisodeNumber => new IdentificationSpec(
                IdentificationStrategy.SeasonAndEpisodeNumber,
                SeasonPattern: raw.SeasonRegex,
                EpisodePattern: raw.EpisodeRegex,
                CaptureGroup: raw.CaptureGroup),

            IdentificationStrategy.AbsoluteEpisodeNumber => new IdentificationSpec(
                IdentificationStrategy.AbsoluteEpisodeNumber,
                EpisodePattern: raw.EpisodeRegex,
                CaptureGroup: raw.CaptureGroup),

            IdentificationStrategy.TitleExact => new IdentificationSpec(
                IdentificationStrategy.TitleExact,
                TitleParts: TransformTitleRules(raw.TitleRules)),

            IdentificationStrategy.TitleIncludes => new IdentificationSpec(
                IdentificationStrategy.TitleIncludes,
                TitleParts: TransformTitleRules(raw.TitleRules)),

            IdentificationStrategy.AirdateExtraction => new IdentificationSpec(
                IdentificationStrategy.AirdateExtraction),

            _ => null,
        };
    }

    private static bool TryParseStrategy(string? value, out IdentificationStrategy strategy)
    {
        strategy = default;
        if (value is null) return false;

        var bytes = System.Text.Encoding.UTF8.GetBytes($"\"{value}\"");
        try
        {
            strategy = JsonSerializer.Deserialize<IdentificationStrategy>(bytes, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static TitlePart[]? TransformTitleRules(List<RawTitleRule>? titleRules)
    {
        if (titleRules is null or { Count: 0 })
        {
            return null;
        }

        var parts = new List<TitlePart>();
        foreach (var raw in titleRules)
        {
            if (raw.Type is null)
            {
                continue;
            }

            parts.Add(new TitlePart(
                raw.Type.Value,
                Value: raw.Value,
                Pattern: raw.Pattern,
                Field: raw.Field,
                CaptureGroup: raw.CaptureGroup));
        }

        return parts.Count > 0 ? parts.ToArray() : null;
    }

    private static FilterSpec TransformFilterGroup(RawFilterGroup raw)
    {
        var all = TransformFilterNodes(raw.All);
        var any = TransformFilterNodes(raw.Any);
        var not = TransformFilterNodes(raw.Not);
        return new FilterSpec(all, any, not);
    }

    private static FilterNode[]? TransformFilterNodes(List<JsonElement>? nodes)
    {
        if (nodes is null or { Count: 0 })
        {
            return null;
        }

        var results = new List<FilterNode>();
        foreach (var element in nodes)
        {
            if (element.TryGetProperty("all", out _) ||
                element.TryGetProperty("any", out _) ||
                element.TryGetProperty("not", out _))
            {
                var nested = element.Deserialize<RawFilterGroup>(_jsonOptions);
                if (nested is not null)
                {
                    results.Add(new FilterNode.GroupNode(TransformFilterGroup(nested)));
                }
            }
            else
            {
                var condition = TransformCondition(element);
                if (condition is not null)
                {
                    results.Add(new FilterNode.ConditionNode(condition));
                }
            }
        }

        return results.Count > 0 ? results.ToArray() : null;
    }

    private static FilterCondition? TransformCondition(JsonElement element)
    {
        if (!element.TryGetProperty("field", out var fieldEl) ||
            !element.TryGetProperty("op", out var opEl) ||
            !element.TryGetProperty("value", out var valueEl))
        {
            return null;
        }

        var field = fieldEl.Deserialize<FilterField?>(_jsonOptions);
        var op = opEl.Deserialize<FilterOp?>(_jsonOptions);

        if (field is null || op is null)
        {
            return null;
        }

        return new FilterCondition(field.Value, op.Value, valueEl.GetString() ?? "");
    }

    private sealed class RawRuleSet
    {
        public string Topic { get; set; } = "";
        public List<string>? Aliases { get; set; }
        public RawMedia? Media { get; set; }
        public float? Confidence { get; set; }
        public List<RawRule>? Rules { get; set; }
        public bool Standalone { get; set; }
        public List<string>? Disable { get; set; }
    }

    private sealed class RawMedia
    {
        public int? TvdbId { get; set; }
        public string? ImdbId { get; set; }
        public int? TmdbId { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
    }

    private sealed class RawRule
    {
        public string Id { get; set; } = "";
        public int Priority { get; set; }
        public float? Confidence { get; set; }
        public string? Strategy { get; set; }
        public RawFilterGroup? Filters { get; set; }
        public string? SeasonRegex { get; set; }
        public string? EpisodeRegex { get; set; }
        public int? CaptureGroup { get; set; }
        public List<RawTitleRule>? TitleRules { get; set; }
    }

    private sealed class RawFilterGroup
    {
        public List<JsonElement>? All { get; set; }
        public List<JsonElement>? Any { get; set; }
        public List<JsonElement>? Not { get; set; }
    }

    private sealed class RawTitleRule
    {
        public TitlePartType? Type { get; set; }
        public FilterField? Field { get; set; }
        public string? Pattern { get; set; }
        public int? CaptureGroup { get; set; }
        public string? Value { get; set; }
    }
}
