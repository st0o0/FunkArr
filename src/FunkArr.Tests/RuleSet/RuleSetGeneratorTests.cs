using FunkArr.RuleSet;
using FunkArr.Search;

namespace FunkArr.Tests.RuleSet;

public class RuleSetGeneratorTests
{
    [Fact]
    public void FindBestTopic_ExactMatch()
    {
        var results = new[]
        {
            CreateItem("Feuer & Flamme", "Ep1"),
            CreateItem("Feuerwehr Doku", "Ep2"),
        };

        Assert.Equal("Feuer & Flamme",
            RuleSetGeneratorActor.FindBestTopic(results, "Feuer & Flamme"));
    }

    [Fact]
    public void FindBestTopic_ContainsMatch()
    {
        var results = new[]
        {
            CreateItem("Checker Can, Checker Tobi und Checker Julian", "Ep1"),
        };

        Assert.Equal("Checker Can, Checker Tobi und Checker Julian",
            RuleSetGeneratorActor.FindBestTopic(results, "Checker Tobi"));
    }

    [Fact]
    public void FindBestTopic_SingleFallback()
    {
        var results = new[] { CreateItem("Some Topic", "Ep1") };

        Assert.Equal("Some Topic",
            RuleSetGeneratorActor.FindBestTopic(results, "Completely Different"));
    }

    [Fact]
    public void FindBestTopic_ReturnsNull_ForMultipleNonMatching()
    {
        var results = new[]
        {
            CreateItem("TopicA", "Ep1"),
            CreateItem("TopicB", "Ep2"),
        };

        Assert.Null(RuleSetGeneratorActor.FindBestTopic(results, "TopicC"));
    }

    [Fact]
    public void IsAccessibilityVariant_DetectsVariants()
    {
        Assert.True(RuleSetGeneratorActor.IsAccessibilityVariant("Episode (Audiodeskription)"));
        Assert.True(RuleSetGeneratorActor.IsAccessibilityVariant("Episode (Gebärdensprache)"));
        Assert.True(RuleSetGeneratorActor.IsAccessibilityVariant("Episode (Gebardensprache)"));
        Assert.True(RuleSetGeneratorActor.IsAccessibilityVariant("Episode (klare Sprache)"));
        Assert.False(RuleSetGeneratorActor.IsAccessibilityVariant("Normal Episode"));
    }

    [Fact]
    public void AnalyzePatterns_DetectsSeasonEpisode()
    {
        var samples = Enumerable.Range(1, 10)
            .Select(i => CreateItem("Show", $"Episode {i} (S01/E{i:D2})"))
            .ToArray();

        var result = RuleSetGeneratorActor.AnalyzePatterns(samples, "Show");

        Assert.Equal(10, result.SeasonEpisodeCount);
        Assert.Equal(0, result.DateCount);
    }

    [Fact]
    public void AnalyzePatterns_DetectsDatePattern()
    {
        var samples = new[]
        {
            CreateItem("Show", "Show vom 5. Juni 2026"),
            CreateItem("Show", "Show vom 12. Mai 2026"),
            CreateItem("Show", "Show vom 1. April 2026"),
            CreateItem("Show", "Normal title"),
        };

        var result = RuleSetGeneratorActor.AnalyzePatterns(samples, "Show");

        Assert.Equal(3, result.DateCount);
        Assert.Equal(0, result.SeasonEpisodeCount);
    }

    [Fact]
    public void AnalyzePatterns_DetectsAbsoluteEpisode()
    {
        var samples = Enumerable.Range(1600, 5)
            .Select(i => CreateItem("Sturm der Liebe", $"Sturm der Liebe ({i})"))
            .ToArray();

        var result = RuleSetGeneratorActor.AnalyzePatterns(samples, "Sturm der Liebe");

        Assert.Equal(5, result.AbsoluteEpisodeCount);
    }

    [Fact]
    public void DetectStrategy_SeasonEpisodeWins()
    {
        var analysis = new RuleSetGeneratorActor.PatternAnalysis
        {
            SeasonEpisodeCount = 10, DateCount = 2, Total = 15,
        };

        Assert.Equal(MatchingStrategy.SeasonAndEpisodeNumber,
            RuleSetGeneratorActor.DetectStrategy(analysis));
    }

    [Fact]
    public void DetectStrategy_DateWinsOverLowSE()
    {
        var analysis = new RuleSetGeneratorActor.PatternAnalysis
        {
            SeasonEpisodeCount = 1, DateCount = 8, Total = 15,
        };

        Assert.Equal(MatchingStrategy.ItemTitleEqualsAirdate,
            RuleSetGeneratorActor.DetectStrategy(analysis));
    }

    [Fact]
    public void DetectStrategy_FallbackToIncludes()
    {
        var analysis = new RuleSetGeneratorActor.PatternAnalysis
        {
            SeasonEpisodeCount = 1, DateCount = 1, AbsoluteEpisodeCount = 0, Total = 15,
        };

        Assert.Equal(MatchingStrategy.ItemTitleIncludes,
            RuleSetGeneratorActor.DetectStrategy(analysis));
    }

    [Fact]
    public void GenerateRegex_ParenSeasonEpisode()
    {
        var samples = new[] { CreateItem("Show", "Episode (S01/E05)") };

        var (season, episode, rules) = RuleSetGeneratorActor.GenerateRegex(
            samples, MatchingStrategy.SeasonAndEpisodeNumber, "Show");

        Assert.NotNull(season);
        Assert.NotNull(episode);
        Assert.Empty(rules);
    }

    [Fact]
    public void GenerateRegex_DateVom()
    {
        var samples = new[] { CreateItem("Show", "Show vom 5. Juni 2026") };

        var (season, episode, rules) = RuleSetGeneratorActor.GenerateRegex(
            samples, MatchingStrategy.ItemTitleEqualsAirdate, "Show");

        Assert.Null(season);
        Assert.Null(episode);
        Assert.Single(rules);
        Assert.Equal(TitleRuleType.Regex, rules[0].Type);
    }

    [Fact]
    public void GenerateRegex_AbsoluteParenNumber()
    {
        var samples = new[] { CreateItem("Sturm der Liebe", "Sturm der Liebe (1606)") };

        var (season, episode, rules) = RuleSetGeneratorActor.GenerateRegex(
            samples, MatchingStrategy.ByAbsoluteEpisodeNumber, "Sturm der Liebe");

        Assert.Null(season);
        Assert.NotNull(episode);
        Assert.Empty(rules);
    }

    [Fact]
    public void DeriveDurationFilter_ComputesMedianHalf()
    {
        var samples = Enumerable.Range(0, 5)
            .Select(_ => CreateItem("Show", "Ep", 2700))
            .ToArray();

        var filter = RuleSetGeneratorActor.DeriveDurationFilter(samples);

        Assert.NotNull(filter);
        Assert.Equal("duration", filter.Field);
        Assert.Equal(FilterOp.GreaterThan, filter.Op);
        Assert.Equal("22", filter.Value);
    }

    [Fact]
    public void ComputeConfidence_HighWhenAllMatch()
    {
        var samples = Enumerable.Range(1, 10)
            .Select(i => CreateItem("Show", $"Episode (S01/E{i:D2})", 2700))
            .ToArray();

        var rule = new Rule
        {
            Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
            SeasonRegex = @"(?<=S)(\d{1,4})(?=/E)",
            EpisodeRegex = @"(?<=E)(\d{1,4})(?=\))",
            Filters = FilterGroup.Empty,
        };

        var confidence = RuleSetGeneratorActor.ComputeConfidence(samples, rule);

        Assert.True(confidence >= 0.8);
    }

    private static MediathekResultItem CreateItem(string topic, string title, int duration = 2700) =>
        new()
        {
            Channel = "ARD",
            Topic = topic,
            Title = title,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Duration = duration,
            Url_Video = "http://video.mp4",
        };
}
