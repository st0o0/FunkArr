using FunkArr.RuleSet;
using FunkArr.Search;

namespace FunkArr.Tests.RuleSet;

public class RuleSetMatchingEngineTests
{
    [Fact]
    public void SeasonAndEpisode_MatchesFeuerUndFlamme()
    {
        var item = CreateItem("Feuer & Flamme", "Folge 8: Doppelalarm fur Christoph 9 (S11/E08)", 2700);
        var rules = new[]
        {
            new Rule
            {
                Priority = 0,
                Filters = new FilterGroup { All = [new Filter { Field = "duration", Op = FilterOp.GreaterThan, Value = "35" }] },
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                SeasonRegex = @"(?<=S)(\d{2,4})(?=\s*/E\d{2,4})",
                EpisodeRegex = @"(?<=\bS\d{2,4}\s*/E)(\d{2,4})(?=\))",
            },
        };
        var episodes = new[] { CreateEpisode("Doppelalarm", 11, 8) };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "Feuer & Flamme");

        Assert.NotNull(result);
        Assert.Equal(11, result.Episode.AiredSeason);
        Assert.Equal(8, result.Episode.AiredEpisodeNumber);
    }

    [Fact]
    public void SeasonAndEpisode_NoMatchWhenEpisodeNotInTvdb()
    {
        var item = CreateItem("Feuer & Flamme", "Folge 99 (S99/E01)", 2700);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                SeasonRegex = @"(?<=S)(\d{2,4})(?=\s*/E\d{2,4})",
                EpisodeRegex = @"(?<=\bS\d{2,4}\s*/E)(\d{2,4})(?=\))",
            },
        };
        var episodes = new[] { CreateEpisode("Ep1", 11, 1) };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "Feuer & Flamme");

        Assert.Null(result);
    }

    [Fact]
    public void Airdate_MatchesHeuteShow()
    {
        var item = CreateItem("heute-show", "heute-show vom 5. Juni 2026 - heute-show (S2026/E17)", 2100);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.ItemTitleEqualsAirdate,
                TitleRules =
                [
                    new TitleRule
                    {
                        Type = TitleRuleType.Regex,
                        Field = "title",
                        Pattern = @"vom\s+(\d{1,2}\.\s*\w+\s*\d{4})",
                    },
                ],
            },
        };
        var episodes = new[] { CreateEpisode("Episode 17", 2026, 17, "2026-06-05") };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "heute-show");

        Assert.NotNull(result);
        Assert.Equal("2026-06-05", result.Episode.FirstAired);
    }

    [Fact]
    public void AbsoluteEpisode_MatchesSturmDerLiebe()
    {
        var item = CreateItem("Sturm der Liebe", "Sturm der Liebe (1606)", 2880);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.ByAbsoluteEpisodeNumber,
                Filters = new FilterGroup { All = [new Filter { Field = "duration", Op = FilterOp.GreaterThan, Value = "35" }] },
                EpisodeRegex = @"\((\d{3,4})\)",
            },
        };
        var episodes = new[] { CreateEpisode("Episode 1606", 1, 1606) };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "Sturm der Liebe");

        Assert.NotNull(result);
        Assert.Equal(1606, result.Episode.AiredEpisodeNumber);
    }

    [Fact]
    public void TitleExact_MatchesTatort()
    {
        var item = CreateItem("Tatort", "Tatort: Die goldene Zeit", 5340);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.ItemTitleExact,
                TitleRules =
                [
                    new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^Tatort:\s*(.+)" },
                ],
            },
        };
        var episodes = new[]
        {
            CreateEpisode("Die goldene Zeit", 1, 3, "2025-03-15"),
        };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "Tatort");

        Assert.NotNull(result);
        Assert.Equal("Die goldene Zeit", result.MatchedTitle);
    }

    [Fact]
    public void TitleIncludes_MatchesCheckerTobi()
    {
        var item = CreateItem("Checker Can, Checker Tobi und Checker Julian", "Der Brot-Check", 1440);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.ItemTitleIncludes,
                TitleRules =
                [
                    new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^(Der .+-Check)" },
                ],
            },
        };
        var episodes = new[] { CreateEpisode("Der Brot-Check", 1, 5) };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "Checker Tobi");

        Assert.NotNull(result);
        Assert.Contains("Brot-Check", result.MatchedTitle);
    }

    [Fact]
    public void SkipsAudiodeskription()
    {
        var item = CreateItem("Tatort", "Die goldene Zeit (Audiodeskription)", 5340);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.ItemTitleExact,
                TitleRules = [new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^Tatort:\s*(.+)" }],
            },
        };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, [], "Tatort");

        Assert.Null(result);
    }

    [Fact]
    public void DurationFilter_ConvertsSecondsToMinutes()
    {
        var item = CreateItem("Show", "Episode (S01/E01)", 300);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                Filters = new FilterGroup { All = [new Filter { Field = "duration", Op = FilterOp.GreaterThan, Value = "35" }] },
                SeasonRegex = @"S(\d+)",
                EpisodeRegex = @"E(\d+)",
            },
        };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, [CreateEpisode("Ep", 1, 1)], "Show");

        Assert.Null(result);
    }

    [Fact]
    public void FirstMatchWins_StopsAfterFirstRule()
    {
        var item = CreateItem("Show", "Show: Episode Title (S01/E01)", 2700);
        var rules = new[]
        {
            new Rule
            {
                Priority = 0,
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                SeasonRegex = @"S(\d+)",
                EpisodeRegex = @"E(\d+)",
            },
            new Rule
            {
                Priority = 10,
                Strategy = MatchingStrategy.ItemTitleExact,
                TitleRules = [new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @":\s*(.+?)(?:\s*\(|$)" }],
            },
        };
        var episodes = new[] { CreateEpisode("Episode Title", 1, 1) };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "Show");

        Assert.NotNull(result);
        Assert.Equal("S01E01", result.MatchedTitle);
    }

    [Fact]
    public void MultipleFilters_AllMustPass()
    {
        var item = CreateItem("Show", "Episode (S01/E01)", 1500);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                Filters = new FilterGroup
                {
                    All =
                    [
                        new Filter { Field = "duration", Op = FilterOp.GreaterThan, Value = "15" },
                        new Filter { Field = "duration", Op = FilterOp.LessThan, Value = "30" },
                    ],
                },
                SeasonRegex = @"S(\d+)",
                EpisodeRegex = @"E(\d+)",
            },
        };
        var episodes = new[] { CreateEpisode("Ep", 1, 1) };

        var result = RuleSetMatchingEngine.EvaluateRules(item, rules, episodes, "Show");

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("5. Juni 2026", 2026, 6, 5)]
    [InlineData("15. Januar 2024", 2024, 1, 15)]
    [InlineData("16.08.2026", 2026, 8, 16)]
    [InlineData("1. März 2025", 2025, 3, 1)]
    public void TryParseGermanDate_ParsesFormats(string input, int year, int month, int day)
    {
        Assert.True(RuleSetMatchingEngine.TryParseGermanDate(input, out var result));
        Assert.Equal(new DateTime(year, month, day), result);
    }

    [Fact]
    public void BuildTitle_CombinesRegexAndStatic()
    {
        var item = CreateItem("Show", "Alice und Bob | Extra", 2700);
        var titleRules = new TitleRule[]
        {
            new() { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^(.+?)\s+und\s+" },
            new() { Type = TitleRuleType.Static, Value = " & " },
            new() { Type = TitleRuleType.Regex, Field = "title", Pattern = @"\s+und\s+([^|]+?)(?:\s*\||$)" },
        };

        var result = RuleSetMatchingEngine.BuildTitle(item, titleRules);

        Assert.Equal("Alice & Bob", result);
    }

    [Fact]
    public void BuildTitle_ReturnsNull_WhenRegexFails()
    {
        var item = CreateItem("Show", "No match here", 2700);
        var titleRules = new TitleRule[]
        {
            new() { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^Tatort:\s*(.+)" },
        };

        var result = RuleSetMatchingEngine.BuildTitle(item, titleRules);

        Assert.Null(result);
    }

    [Fact]
    public void EvaluateRulesWithTraces_MatchedItem_ProducesMatchedTrace()
    {
        var item = CreateItem("Feuer & Flamme", "Folge 8: Doppelalarm (S11/E08)", 2700);
        var rules = new[]
        {
            new Rule
            {
                Priority = 0,
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                Confidence = 0.95,
                SeasonRegex = @"S(\d+)",
                EpisodeRegex = @"E(\d+)",
            },
        };
        var episodes = new[] { CreateEpisode("Doppelalarm", 11, 8) };

        var (matches, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            [item], rules, episodes, "Feuer & Flamme");

        Assert.Single(matches);
        Assert.Single(traces);
        var trace = Assert.IsType<MatchedTrace>(traces[0]);
        Assert.Equal(0, trace.RuleIndex);
        Assert.Equal(MatchingStrategy.SeasonAndEpisodeNumber, trace.Strategy);
        Assert.Equal(0.95, trace.Confidence);
        Assert.Equal(11, trace.Season);
        Assert.Equal(8, trace.Episode);
    }

    [Fact]
    public void EvaluateRulesWithTraces_FilteredItem_ProducesFilteredTrace()
    {
        var item = CreateItem("Tatort", "Die goldene Zeit (Audiodeskription)", 5340);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.ItemTitleExact,
                TitleRules = [new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^Tatort:\s*(.+)" }],
            },
        };

        var (matches, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            [item], rules, [], "Tatort");

        Assert.Empty(matches);
        Assert.Single(traces);
        var trace = Assert.IsType<FilteredTrace>(traces[0]);
        Assert.Equal("accessibility-skip", trace.Reason);
    }

    [Fact]
    public void EvaluateRulesWithTraces_UnmatchedItem_ProducesUnmatchedTrace()
    {
        var item = CreateItem("Show", "No match here", 2700);
        var rules = new[]
        {
            new Rule
            {
                Priority = 0,
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                SeasonRegex = @"S(\d+)",
                EpisodeRegex = @"E(\d+)",
            },
            new Rule
            {
                Priority = 10,
                Strategy = MatchingStrategy.ItemTitleExact,
                TitleRules = [new TitleRule { Type = TitleRuleType.Regex, Field = "title", Pattern = @"^Show:\s*(.+)" }],
            },
        };
        var episodes = new[] { CreateEpisode("Something", 1, 1) };

        var (matches, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            [item], rules, episodes, "Show");

        Assert.Empty(matches);
        Assert.Single(traces);
        var trace = Assert.IsType<UnmatchedTrace>(traces[0]);
        Assert.Equal(2, trace.RuleFailures.Count);
        Assert.All(trace.RuleFailures, f => Assert.Equal("strategy-no-match", f.FailReason));
    }

    [Fact]
    public void EvaluateRulesWithTraces_MixedResults_CorrectTraceCounts()
    {
        var matched = CreateItem("Show", "Episode (S01/E01)", 2700);
        var filtered = CreateItem("Show", "Episode (Audiodeskription)", 2700);
        var unmatched = CreateItem("Show", "Something else entirely", 2700);
        var rules = new[]
        {
            new Rule
            {
                Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                SeasonRegex = @"S(\d+)",
                EpisodeRegex = @"E(\d+)",
            },
        };
        var episodes = new[] { CreateEpisode("Ep", 1, 1) };

        var (matches, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            [matched, filtered, unmatched], rules, episodes, "Show");

        Assert.Single(matches);
        Assert.Equal(3, traces.Count);
        Assert.Single(traces.OfType<MatchedTrace>());
        Assert.Single(traces.OfType<FilteredTrace>());
        Assert.Single(traces.OfType<UnmatchedTrace>());
    }

    private static MediathekResultItem CreateItem(string topic, string title, int duration) =>
        new()
        {
            Channel = "ARD",
            Topic = topic,
            Title = title,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Duration = duration,
            Url_Video = "http://video.mp4",
        };

    private static TvdbEpisodeInfo CreateEpisode(
        string name, int season, int episode, string? firstAired = null) =>
        new()
        {
            EpisodeName = name,
            AiredSeason = season,
            AiredEpisodeNumber = episode,
            FirstAired = firstAired ?? "2025-01-01",
        };
}
