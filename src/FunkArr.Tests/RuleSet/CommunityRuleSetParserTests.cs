using FunkArr.RuleSet;

namespace FunkArr.Tests.RuleSet;

public class CommunityRuleSetParserTests
{
    [Fact]
    public void Parse_TransformsSeasonEpisodeRuleSet()
    {
        var json = """
        [
          {
            "id": 7,
            "mediaId": 2,
            "topic": "Feuer & Flamme",
            "priority": 0,
            "filters": "[{\"attribute\":\"duration\",\"type\":\"GreaterThan\",\"value\":\"35\"}]",
            "titleRegexRules": "[]",
            "episodeRegex": "(?<=\\bS\\d{2,4}\\s*/E)(\\d{2,4})(?=\\))",
            "seasonRegex": "(?<=S)(\\d{2,4})(?=\\s*/E\\d{2,4})",
            "matchingStrategy": "SeasonAndEpisodeNumber",
            "media": {
              "media_id": 2,
              "media_name": "Feuer & Flamme",
              "media_type": "show",
              "media_tmdbId": null,
              "media_imdbId": "tt7995922",
              "media_tvdbId": 329324
            }
          }
        ]
        """;

        var result = CommunityRuleSetParser.Parse(json);

        Assert.Single(result);
        var ruleSet = result[0];
        Assert.Equal("Feuer & Flamme", ruleSet.Topic);
        Assert.Equal(329324, ruleSet.Media.TvdbId);
        Assert.Equal("tt7995922", ruleSet.Media.ImdbId);
        Assert.Equal("community", ruleSet.Source);
        Assert.Equal(1.0, ruleSet.Confidence);

        var rule = Assert.Single(ruleSet.Rules);
        Assert.Equal(0, rule.Priority);
        Assert.Equal(MatchingStrategy.SeasonAndEpisodeNumber, rule.Strategy);
        Assert.NotNull(rule.SeasonRegex);
        Assert.NotNull(rule.EpisodeRegex);

        var filterNode = Assert.Single(rule.Filters.All);
        var filter = Assert.IsType<Filter>(filterNode);
        Assert.Equal("duration", filter.Field);
        Assert.Equal(FilterOp.GreaterThan, filter.Op);
        Assert.Equal("35", filter.Value);
    }

    [Fact]
    public void Parse_TransformsAirdateRuleSet()
    {
        var json = """
        [
          {
            "id": 8,
            "topic": "ZDF Magazin Royale",
            "priority": 0,
            "filters": "[{\"attribute\":\"duration\",\"type\":\"GreaterThan\",\"value\":\"24\"}]",
            "titleRegexRules": "[{\"type\":\"regex\",\"field\":\"title\",\"pattern\":\"^ZDF Magazin Royale vom (\\\\d{1,2}\\\\. \\\\w+ \\\\d{4})\"}]",
            "episodeRegex": "",
            "seasonRegex": "",
            "matchingStrategy": "ItemTitleEqualsAirdate",
            "media": {
              "media_id": 3,
              "media_name": "ZDF Magazin Royale",
              "media_type": "show",
              "media_tvdbId": 390284
            }
          }
        ]
        """;

        var result = CommunityRuleSetParser.Parse(json);

        Assert.Single(result);
        var rule = result[0].Rules[0];
        Assert.Equal(MatchingStrategy.ItemTitleEqualsAirdate, rule.Strategy);
        Assert.Null(rule.SeasonRegex);
        Assert.Null(rule.EpisodeRegex);

        var titleRule = Assert.Single(rule.TitleRules);
        Assert.Equal(TitleRuleType.Regex, titleRule.Type);
        Assert.Equal("title", titleRule.Field);
        Assert.NotNull(titleRule.Pattern);
    }

    [Fact]
    public void Parse_GroupsMultipleEntriesByTopic()
    {
        var json = """
        [
          {
            "id": 1, "topic": "Tatort", "priority": 0,
            "filters": "[]", "titleRegexRules": "[]",
            "matchingStrategy": "SeasonAndEpisodeNumber",
            "media": { "media_name": "Tatort", "media_type": "show", "media_tvdbId": 83214 }
          },
          {
            "id": 2, "topic": "Tatort", "priority": 10,
            "filters": "[]", "titleRegexRules": "[{\"type\":\"regex\",\"field\":\"title\",\"pattern\":\"^Tatort:\\\\s*(.+)\"}]",
            "matchingStrategy": "ItemTitleExact",
            "media": { "media_name": "Tatort", "media_type": "show", "media_tvdbId": 83214 }
          }
        ]
        """;

        var result = CommunityRuleSetParser.Parse(json);

        Assert.Single(result);
        Assert.Equal(2, result[0].Rules.Count);
        Assert.Equal(0, result[0].Rules[0].Priority);
        Assert.Equal(10, result[0].Rules[1].Priority);
    }

    [Fact]
    public void Parse_TransformsAbsoluteEpisodeNumber()
    {
        var json = """
        [
          {
            "id": 87, "topic": "Sturm der Liebe", "priority": 0,
            "filters": "[{\"attribute\":\"duration\",\"type\":\"GreaterThan\",\"value\":\"35\"}]",
            "titleRegexRules": "[]",
            "episodeRegex": "Episode\\s(\\d+)",
            "seasonRegex": "",
            "matchingStrategy": "ByAbsoluteEpisodeNumber",
            "media": { "media_name": "Sturm der Liebe", "media_type": "show", "media_tvdbId": 176491 }
          }
        ]
        """;

        var result = CommunityRuleSetParser.Parse(json);

        var rule = result[0].Rules[0];
        Assert.Equal(MatchingStrategy.ByAbsoluteEpisodeNumber, rule.Strategy);
        Assert.NotNull(rule.EpisodeRegex);
        Assert.Null(rule.SeasonRegex);
    }

    [Fact]
    public void Parse_HandlesMultipleFilters()
    {
        var json = """
        [
          {
            "id": 140, "topic": "Doctor Who", "priority": 0,
            "filters": "[{\"attribute\":\"duration\",\"type\":\"GreaterThan\",\"value\":\"15\"},{\"attribute\":\"duration\",\"type\":\"LowerThan\",\"value\":\"30\"}]",
            "titleRegexRules": "[]",
            "matchingStrategy": "SeasonAndEpisodeNumber",
            "media": { "media_name": "Doctor Who", "media_type": "show", "media_tvdbId": 12345 }
          }
        ]
        """;

        var result = CommunityRuleSetParser.Parse(json);

        var filters = result[0].Rules[0].Filters.All;
        Assert.Equal(2, filters.Count);
        Assert.Equal(FilterOp.GreaterThan, ((Filter)filters[0]).Op);
        Assert.Equal(FilterOp.LessThan, ((Filter)filters[1]).Op);
    }

    [Fact]
    public void Parse_HandlesEmptyInput()
    {
        Assert.Empty(CommunityRuleSetParser.Parse("[]"));
    }

    [Fact]
    public void Parse_TransformsTitleExactWithMultipleRules()
    {
        var json = """
        [
          {
            "id": 152, "topic": "Wer weiß denn sowas?", "priority": 0,
            "filters": "[{\"attribute\":\"duration\",\"type\":\"GreaterThan\",\"value\":\"31\"}]",
            "titleRegexRules": "[{\"type\":\"regex\",\"field\":\"title\",\"pattern\":\"^(.+?)\\\\s+und\\\\s+\"},{\"type\":\"static\",\"field\":\"title\",\"pattern\":\"\",\"value\":\" & \"},{\"type\":\"regex\",\"field\":\"title\",\"pattern\":\"\\\\s+und\\\\s+([^|]+?)(?:\\\\s*\\\\||$)\"}]",
            "matchingStrategy": "ItemTitleExact",
            "media": { "media_name": "Wer weiß denn sowas?", "media_type": "show", "media_tvdbId": 298954 }
          }
        ]
        """;

        var result = CommunityRuleSetParser.Parse(json);

        var titleRules = result[0].Rules[0].TitleRules;
        Assert.Equal(3, titleRules.Count);
        Assert.Equal(TitleRuleType.Regex, titleRules[0].Type);
        Assert.Equal(TitleRuleType.Static, titleRules[1].Type);
        Assert.Equal(" & ", titleRules[1].Value);
        Assert.Equal(TitleRuleType.Regex, titleRules[2].Type);
    }
}
