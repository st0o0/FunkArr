using System.Text.Json;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;

namespace FunkArr.RuleSet.DiskModel;

internal static class DiskToMatchingExtensions
{
    public static MatchingConfig? ToMatchingConfig(this DiskRuleSet disk, string ruleSetId)
    {
        if (disk.Rules is null or { Count: 0 })
        {
            return null;
        }

        var rules = TransformRules(disk.Rules);
        return new MatchingConfig(ruleSetId, disk.Confidence ?? 0f, rules);
    }

    public static (string Topic, string[] Aliases, int? TvdbId, string? ImdbId, int? TmdbId,
        string? MediaName, Messages.MediaType? MediaType, EnrichmentConfig Enrichment)? ToIdentity(this DiskRuleSet disk)
    {
        var aliases = disk.Aliases?.ToArray() ?? [];
        return (disk.Topic, aliases,
            disk.Media?.TvdbId, disk.Media?.ImdbId, disk.Media?.TmdbId,
            disk.Media?.Name, disk.Media?.Type,
            disk.Enrichment.ToEnrichmentConfig());
    }

    public static EnrichmentConfig ToEnrichmentConfig(this DiskEnrichment? raw) =>
        new(Enabled: raw?.Enabled ?? true,
            Methods: raw?.Methods?.ToArray() ?? [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
            Title: new TitleMatchConfig(raw?.Title?.Threshold ?? 0.7f),
            Airdate: new AirdateMatchConfig(raw?.Airdate?.Tolerance ?? 7, raw?.Airdate?.MinTitleAffinity ?? 0.3f),
            Runtime: new RuntimeMatchConfig(
                raw?.Runtime?.Tolerance ?? 0.35f,
                raw?.Runtime?.Mode ?? RuntimeMode.Tiebreaker),
            Year: new YearMatchConfig(raw?.Year?.Tolerance ?? 1));

    public static RuleSetDetailRule[] ToDetailRules(this DiskRuleSet disk)
    {
        if (disk.Rules is null or { Count: 0 })
        {
            return [];
        }

        return
        [
            .. disk.Rules.Select(r =>
            {
                RuleSetEnumMapping.TryParseStrategy(r.Strategy, out var strategy);

                return new RuleSetDetailRule(
                    r.Id, r.Priority, r.Confidence, strategy,
                    r.SeasonRegex, r.EpisodeRegex, r.CaptureGroup,
                    r.Filters.ToFilterGroupOutput(),
                    r.TitleRules?.Select(t => new TitleRuleOutput(
                        t.Type ?? TitlePartType.Static, t.Field, t.Pattern, t.CaptureGroup, t.Value)).ToArray());
            })
        ];
    }

    private static FilterGroupOutput? ToFilterGroupOutput(this DiskFilterGroup? group)
    {
        if (group is null)
        {
            return null;
        }

        var all = MapConditions(group.All);
        var any = MapConditions(group.Any);
        var not = MapConditions(group.Not);

        return all is null && any is null && not is null ? null : new FilterGroupOutput(all, any, not);
    }

    private static FilterConditionOutput[]? MapConditions(List<JsonElement>? nodes)
    {
        if (nodes is null or { Count: 0 })
        {
            return null;
        }

        var results = new List<FilterConditionOutput>();
        foreach (var element in nodes)
        {
            var condition = element.Deserialize<DiskFilter>(DiskJsonOptions.Default);
            if (condition?.Field is not null && condition.Op is not null)
            {
                results.Add(new FilterConditionOutput(condition.Field.Value, condition.Op.Value, condition.Value ?? ""));
            }
        }

        return results.Count > 0 ? [.. results] : null;
    }

    private static MatchingRule[] TransformRules(List<DiskRule> rawRules)
    {
        var results = new List<MatchingRule>();

        foreach (var raw in rawRules)
        {
            if (!RuleSetEnumMapping.TryParseStrategy(raw.Strategy, out var strategy))
            {
                continue;
            }

            var identification = strategy switch
            {
                IdentificationStrategy.SeasonAndEpisodeNumber => new IdentificationSpec(
                    strategy, SeasonPattern: raw.SeasonRegex, EpisodePattern: raw.EpisodeRegex, CaptureGroup: raw.CaptureGroup),
                IdentificationStrategy.AbsoluteEpisodeNumber => new IdentificationSpec(
                    strategy, EpisodePattern: raw.EpisodeRegex, CaptureGroup: raw.CaptureGroup),
                IdentificationStrategy.TitleExact => new IdentificationSpec(
                    strategy, TitleParts: TransformTitleRules(raw.TitleRules)),
                IdentificationStrategy.TitleIncludes => new IdentificationSpec(
                    strategy, TitleParts: TransformTitleRules(raw.TitleRules)),
                IdentificationStrategy.AirdateExtraction => new IdentificationSpec(strategy),
                _ => null,
            };

            if (identification is null)
            {
                continue;
            }

            var filters = raw.Filters is not null ? TransformFilterGroup(raw.Filters) : null;
            results.Add(new MatchingRule(raw.Id, raw.Priority, raw.Confidence, filters, identification));
        }

        return [.. results];
    }

    private static TitlePart[]? TransformTitleRules(List<DiskTitleRule>? titleRules)
    {
        if (titleRules is null or { Count: 0 })
        {
            return null;
        }

        var parts = titleRules
            .Where(t => t.Type is not null)
            .Select(t => new TitlePart(t.Type!.Value, Value: t.Value, Pattern: t.Pattern, Field: t.Field, CaptureGroup: t.CaptureGroup))
            .ToArray();

        return parts.Length > 0 ? parts : null;
    }

    private static FilterSpec TransformFilterGroup(DiskFilterGroup raw)
    {
        return new FilterSpec(
            TransformFilterNodes(raw.All),
            TransformFilterNodes(raw.Any),
            TransformFilterNodes(raw.Not));
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
            if (element.TryGetProperty("all", out _) || element.TryGetProperty("any", out _) || element.TryGetProperty("not", out _))
            {
                var nested = element.Deserialize<DiskFilterGroup>(DiskJsonOptions.Default);
                if (nested is not null)
                {
                    results.Add(new FilterNode.GroupNode(TransformFilterGroup(nested)));
                }
            }
            else
            {
                var condition = element.Deserialize<DiskFilter>(DiskJsonOptions.Default);
                if (condition?.Field is not null && condition.Op is not null)
                {
                    results.Add(new FilterNode.ConditionNode(
                        new FilterCondition(condition.Field.Value, condition.Op.Value, condition.Value ?? "")));
                }
            }
        }

        return results.Count > 0 ? [.. results] : null;
    }
}
