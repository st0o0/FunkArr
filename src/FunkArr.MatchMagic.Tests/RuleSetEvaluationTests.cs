using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class RuleSetEvaluationTests
{
    private static RuleSet LoadTatort() =>
        RuleSet.FromJson(TestData.LoadResource("tatort-ruleset.json"));

    [Fact]
    public void Matches_episode_via_season_episode_pattern()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort (S01/E05)",
            channel: "ARD",
            durationMinutes: 90);

        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        Assert.Equal("01", results[0].Identification.Season);
        Assert.Equal("05", results[0].Identification.Episode);
        Assert.Equal(0.95f, results[0].Confidence);
    }

    [Fact]
    public void Falls_through_to_title_exact_rule()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort: Die goldene Zeit",
            channel: "ARD",
            durationMinutes: 90);

        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        Assert.Equal("Die goldene Zeit", results[0].Identification.Title);
        Assert.Equal(0.7f, results[0].Confidence);
    }

    [Fact]
    public void Filters_out_audiodeskription()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort: Die goldene Zeit (Audiodeskription)",
            channel: "ARD",
            durationMinutes: 90);

        var results = ruleSet.Evaluate([item]);

        Assert.Empty(results);
    }

    [Fact]
    public void Filters_out_short_duration()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort (S01/E05)",
            channel: "ARD",
            durationMinutes: 30);

        var results = ruleSet.Evaluate([item]);

        Assert.Empty(results);
    }

    [Fact]
    public void Filters_out_wrong_channel_for_rule0_falls_through_to_rule1()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort: Die goldene Zeit (S01/E05)",
            channel: "ZDF",
            durationMinutes: 90);

        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        Assert.Equal(0.7f, results[0].Confidence);
    }

    [Fact]
    public void Multiple_items_matched_independently()
    {
        var ruleSet = LoadTatort();
        var items = new[]
        {
            TestData.CreateItem(title: "Tatort (S01/E05)", channel: "ARD", durationMinutes: 90),
            TestData.CreateItem(title: "Tatort: Schwarzer Freitag", channel: "Das Erste", durationMinutes: 88),
            TestData.CreateItem(title: "Trailer Tatort", channel: "ARD", durationMinutes: 2),
        };

        var results = ruleSet.Evaluate(items);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Empty_rules_returns_empty()
    {
        var ruleSet = new RuleSet("Test", Media: new MediaRef(Name: "Test"), Confidence: 0.5f, Rules: []);
        var item = TestData.CreateItem();

        var results = ruleSet.Evaluate([item]);

        Assert.Empty(results);
    }

    [Fact]
    public void Quality_variants_built_from_urls()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort: Die goldene Zeit",
            channel: "ARD",
            durationMinutes: 90,
            urlHd: "https://example.com/hd.mp4",
            url: "https://example.com/sd.mp4",
            urlLow: "https://example.com/low.mp4");

        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        Assert.Equal(3, results[0].Qualities.Count);
        Assert.Equal(Quality.HD1080, results[0].Qualities[0].Quality);
        Assert.Equal(Quality.HD720, results[0].Qualities[1].Quality);
        Assert.Equal(Quality.SD, results[0].Qualities[2].Quality);
    }

    [Fact]
    public void Quality_variants_skip_null_urls()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort: Die goldene Zeit",
            channel: "ARD",
            durationMinutes: 90,
            urlHd: null,
            url: "https://example.com/sd.mp4",
            urlLow: null);

        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        Assert.Single(results[0].Qualities);
        Assert.Equal(Quality.HD720, results[0].Qualities[0].Quality);
    }

    [Fact]
    public void Size_estimation_uses_bitrate_constants()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort: Die goldene Zeit",
            channel: "ARD",
            durationMinutes: 90,
            urlHd: "https://example.com/hd.mp4",
            url: null,
            urlLow: null);

        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        var hd = results[0].Qualities[0];
        Assert.Equal(Quality.HD1080, hd.Quality);
        Assert.Equal(5400L * 5_000 * 1000 / 8, hd.EstimatedSizeBytes);
    }

    [Fact]
    public void First_match_wins_per_item()
    {
        var ruleSet = LoadTatort();
        var item = TestData.CreateItem(
            title: "Tatort: Die goldene Zeit (S01/E05)",
            channel: "ARD",
            durationMinutes: 90);

        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        Assert.Equal(0.95f, results[0].Confidence);
        Assert.Equal("01", results[0].Identification.Season);
    }

    [Fact]
    public void Confidence_from_file_default_when_rule_has_none()
    {
        var rule = new Rule("airdate-test", 0, null, MatchStrategy.ItemTitleEqualsAirdate, new FilterGroup());
        var ruleSet = new RuleSet("Test", Media: new MediaRef(Name: "Test"), Confidence: 0.85f, Rules: [rule]);

        var item = TestData.CreateItem(title: "Sendung vom 24.10.2024", durationMinutes: 90);
        var results = ruleSet.Evaluate([item]);

        Assert.Single(results);
        Assert.Equal(0.85f, results[0].Confidence);
    }
}
