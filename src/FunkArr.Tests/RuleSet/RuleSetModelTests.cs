using System.Text.Json;
using FunkArr.RuleSet;

namespace FunkArr.Tests.RuleSet;

public class RuleSetModelTests
{
    [Fact]
    public void RuleSetFile_RoundTripsJson()
    {
        var ruleSet = new RuleSetFile
        {
            Topic = "Feuer & Flamme",
            Media = new MediaReference { TvdbId = 329324, ImdbId = "tt7995922", Name = "Feuer & Flamme" },
            Source = "community",
            Confidence = 1.0,
            Rules =
            [
                new Rule
                {
                    Priority = 0,
                    Filters = new FilterGroup { All = [new Filter { Field = "duration", Op = FilterOp.GreaterThan, Value = "35" }] },
                    Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                    SeasonRegex = @"(?<=S)(\d{2,4})(?=\s*/E\d{2,4})",
                    EpisodeRegex = @"(?<=\bS\d{2,4}\s*/E)(\d{2,4})(?=\))",
                },
            ],
        };

        var json = JsonSerializer.Serialize(ruleSet, RuleSetJsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<RuleSetFile>(json, RuleSetJsonOptions.Default);

        Assert.NotNull(deserialized);
        Assert.Equal("Feuer & Flamme", deserialized.Topic);
        Assert.Equal(329324, deserialized.Media.TvdbId);
        Assert.Equal("community", deserialized.Source);
        Assert.Single(deserialized.Rules);
        Assert.Equal(MatchingStrategy.SeasonAndEpisodeNumber, deserialized.Rules[0].Strategy);
        var filter = Assert.Single(deserialized.Rules[0].Filters.All);
        Assert.IsType<Filter>(filter);
        Assert.Equal(FilterOp.GreaterThan, ((Filter)filter).Op);
    }

    [Fact]
    public void RuleSetFile_SerializesEnumsAsCamelCaseStrings()
    {
        var rule = new Rule
        {
            Strategy = MatchingStrategy.ItemTitleEqualsAirdate,
            Filters = new FilterGroup { All = [new Filter { Field = "duration", Op = FilterOp.LessThan, Value = "30" }] },
            TitleRules = [new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = "test" }],
        };

        var json = JsonSerializer.Serialize(rule, RuleSetJsonOptions.Default);

        Assert.Contains("\"itemTitleEqualsAirdate\"", json);
        Assert.Contains("\"lessThan\"", json);
        Assert.Contains("\"regex\"", json);
    }

    [Fact]
    public void RuleSetFile_DeserializesAllStrategies()
    {
        var strategies = new[]
        {
            ("\"seasonAndEpisodeNumber\"", MatchingStrategy.SeasonAndEpisodeNumber),
            ("\"itemTitleExact\"", MatchingStrategy.ItemTitleExact),
            ("\"itemTitleIncludes\"", MatchingStrategy.ItemTitleIncludes),
            ("\"itemTitleEqualsAirdate\"", MatchingStrategy.ItemTitleEqualsAirdate),
            ("\"byAbsoluteEpisodeNumber\"", MatchingStrategy.ByAbsoluteEpisodeNumber),
        };

        foreach (var (jsonValue, expected) in strategies)
        {
            var json = $$"""{"priority":0,"filters":{},"strategy":{{jsonValue}},"titleRules":[]}""";
            var rule = JsonSerializer.Deserialize<Rule>(json, RuleSetJsonOptions.Default);

            Assert.NotNull(rule);
            Assert.Equal(expected, rule.Strategy);
        }
    }

    [Fact]
    public void RuleSetFile_WithTitleRules_RoundTrips()
    {
        var ruleSet = new RuleSetFile
        {
            Topic = "Wer weiß denn sowas?",
            Media = new MediaReference { TvdbId = 298954, Name = "Wer weiß denn sowas?" },
            Source = "community",
            Rules =
            [
                new Rule
                {
                    Strategy = MatchingStrategy.ItemTitleExact,
                    TitleRules =
                    [
                        new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^(.+?)\s+und\s+" },
                        new TitleRule { Type = TitleRuleType.Static, Value = " & " },
                        new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @"\s+und\s+([^|]+?)(?:\s*\||$)" },
                    ],
                },
            ],
        };

        var json = JsonSerializer.Serialize(ruleSet, RuleSetJsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<RuleSetFile>(json, RuleSetJsonOptions.Default);

        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Rules[0].TitleRules.Count);
        Assert.Equal(TitleRuleType.Static, deserialized.Rules[0].TitleRules[1].Type);
        Assert.Equal(" & ", deserialized.Rules[0].TitleRules[1].Value);
    }
}
