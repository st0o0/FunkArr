using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet.DiskModel;

internal static class MatchingToDiskExtensions
{
    public static DiskRuleSet ToDiskRuleSet(this RuleSetBody body) => new()
    {
        Topic = body.Topic,
        Aliases = body.Aliases?.ToList(),
        Media = body.Media is not null
            ? new DiskMedia
            {
                Name = body.Media.Name,
                Type = body.Media.Type,
                TvdbId = body.Media.TvdbId,
                ImdbId = body.Media.ImdbId,
                TmdbId = body.Media.TmdbId,
            }
            : null,
        Confidence = body.Confidence,
        Rules = [.. body.Rules.Select(MapRule)],
        Standalone = body.Standalone ?? false,
        Disable = body.Disable?.ToList(),
        Enrichment = body.Enrichment is not null ? MapEnrichment(body.Enrichment) : null,
    };

    private static DiskRule MapRule(RuleSetRuleInput rule) => new()
    {
        Id = rule.Id,
        Priority = rule.Priority,
        Confidence = rule.Confidence,
        Strategy = rule.Strategy?.ToDiskValue(),
        SeasonRegex = rule.SeasonRegex,
        EpisodeRegex = rule.EpisodeRegex,
        CaptureGroup = rule.CaptureGroup,
        Filters = rule.Filters is not null ? MapFilterGroup(rule.Filters) : null,
        TitleRules = rule.TitleRules?.Select(MapTitleRule).ToList(),
    };

    private static DiskFilterGroup MapFilterGroup(RuleSetFilterGroupInput group) => new()
    {
        All = group.All?.Select(MapConditionToElement).ToList(),
        Any = group.Any?.Select(MapConditionToElement).ToList(),
        Not = group.Not?.Select(MapConditionToElement).ToList(),
    };

    private static System.Text.Json.JsonElement MapConditionToElement(RuleSetFilterConditionInput condition)
    {
        var obj = new DiskFilter
        {
            Field = condition.Field,
            Op = condition.Op,
            Value = condition.Value,
        };
        var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj, DiskJsonOptions.Default);
        return System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();
    }

    private static DiskTitleRule MapTitleRule(RuleSetTitleRuleInput titleRule) => new()
    {
        Type = titleRule.Type,
        Field = titleRule.Field,
        Pattern = titleRule.Pattern,
        CaptureGroup = titleRule.CaptureGroup,
        Value = titleRule.Value,
    };

    private static DiskEnrichment MapEnrichment(Messages.Enrichment.EnrichmentConfig config) => new()
    {
        Enabled = config.Enabled,
        Methods = [.. config.Methods],
        Title = new DiskTitleMatch { Threshold = config.Title.Threshold },
        Airdate = new DiskAirdateMatch { Tolerance = config.Airdate.Tolerance, MinTitleAffinity = config.Airdate.MinTitleAffinity },
        Runtime = new DiskRuntimeMatch { Tolerance = config.Runtime.Tolerance, Mode = config.Runtime.Mode },
        Year = new DiskYearMatch { Tolerance = config.Year.Tolerance },
    };
}
