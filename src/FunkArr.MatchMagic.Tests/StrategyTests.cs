using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class StrategyTests
{
    private static readonly FilterGroup _passAll = new();

    [Fact]
    public void SeasonAndEpisodeNumber_extracts_correctly()
    {
        var rule = new Rule("se-extract", 0, 0.95f, MatchStrategy.SeasonAndEpisodeNumber, _passAll,
            SeasonRegex: @"(?<=S)(\d{2,4})(?=/E)",
            EpisodeRegex: @"(?<=E)(\d{2,4})(?=\))");

        var item = TestData.CreateItem(title: "Tatort (S01/E05)");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("01", result.Identification.Season);
        Assert.Equal("05", result.Identification.Episode);
        Assert.Equal(0.95f, result.Confidence);
    }

    [Fact]
    public void SeasonAndEpisodeNumber_no_match_returns_null()
    {
        var rule = new Rule("se-nomatch", 0, null, MatchStrategy.SeasonAndEpisodeNumber, _passAll,
            SeasonRegex: @"(?<=S)(\d{2,4})",
            EpisodeRegex: @"(?<=E)(\d{2,4})");

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        Assert.Null(rule.Match(item, 0.9f));
    }

    [Fact]
    public void ItemTitleExact_extracts_title()
    {
        var rule = new Rule("title-exact", 0, null, MatchStrategy.ItemTitleExact, _passAll,
            TitleRules: [new TitleRule("regex", "title", @"^Tatort[^:]*:\s*(.+)")]);

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("Die goldene Zeit", result.Identification.Title);
        Assert.Equal(0.9f, result.Confidence);
    }

    [Fact]
    public void ItemTitleExact_no_match_returns_null()
    {
        var rule = new Rule("title-nomatch", 0, null, MatchStrategy.ItemTitleExact, _passAll,
            TitleRules: [new TitleRule("regex", "title", @"^heute-show\s+(.+)")]);

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        Assert.Null(rule.Match(item, 0.9f));
    }

    [Fact]
    public void Title_chain_with_static_separator()
    {
        var rule = new Rule("title-chain", 0, null, MatchStrategy.ItemTitleExact, _passAll,
            TitleRules:
            [
                new TitleRule("regex", "title", @"^(\w+):", CaptureGroup: 1),
                new TitleRule("static", Value: " & "),
                new TitleRule("regex", "topic", @"^(\w+)"),
            ]);

        var item = TestData.CreateItem(topic: "Krimi", title: "Tatort: Die goldene Zeit");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("Tatort & Krimi", result.Identification.Title);
    }

    [Fact]
    public void Title_chain_fails_if_any_regex_misses()
    {
        var rule = new Rule("title-chain-fail", 0, null, MatchStrategy.ItemTitleExact, _passAll,
            TitleRules:
            [
                new TitleRule("regex", "title", @"^(\w+):"),
                new TitleRule("regex", "title", @"NOMATCH_(\d+)"),
            ]);

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        Assert.Null(rule.Match(item, 0.9f));
    }

    [Fact]
    public void ItemTitleIncludes_matches()
    {
        var rule = new Rule("title-includes", 0, null, MatchStrategy.ItemTitleIncludes, _passAll,
            TitleRules: [new TitleRule("regex", "title", @":\s*(.+)")]);

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("Die goldene Zeit", result.Identification.Title);
    }

    [Fact]
    public void ItemTitleIncludes_no_match()
    {
        var rule = new Rule("includes-nomatch", 0, null, MatchStrategy.ItemTitleIncludes, _passAll,
            TitleRules: [new TitleRule("static", Value: "Schwarzer Freitag")]);

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        Assert.Null(rule.Match(item, 0.9f));
    }

    [Fact]
    public void ItemTitleIncludes_umlaut_normalized()
    {
        var rule = new Rule("umlaut-test", 0, null, MatchStrategy.ItemTitleIncludes, _passAll,
            TitleRules: [new TitleRule("static", Value: "Löwenzähn")]);

        var item = TestData.CreateItem(title: "Löwenzähn - Folge 312");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
    }

    [Fact]
    public void ItemTitleEqualsAirdate_numeric_format()
    {
        var rule = new Rule("airdate-numeric", 0, null, MatchStrategy.ItemTitleEqualsAirdate, _passAll);

        var item = TestData.CreateItem(title: "heute-show vom 24.10.2024");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("2024-10-24", result.Identification.Title);
    }

    [Fact]
    public void ItemTitleEqualsAirdate_two_digit_year()
    {
        var rule = new Rule("airdate-short-year", 0, null, MatchStrategy.ItemTitleEqualsAirdate, _passAll);

        var item = TestData.CreateItem(title: "Sendung vom 24.10.24");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("2024-10-24", result.Identification.Title);
    }

    [Fact]
    public void ItemTitleEqualsAirdate_german_month()
    {
        var rule = new Rule("airdate-german", 0, null, MatchStrategy.ItemTitleEqualsAirdate, _passAll);

        var item = TestData.CreateItem(title: "heute-show vom 16. Juli 2024");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("2024-07-16", result.Identification.Title);
    }

    [Fact]
    public void ItemTitleEqualsAirdate_no_date_returns_null()
    {
        var rule = new Rule("airdate-nodate", 0, null, MatchStrategy.ItemTitleEqualsAirdate, _passAll);

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        Assert.Null(rule.Match(item, 0.9f));
    }

    [Fact]
    public void ByAbsoluteEpisodeNumber_extracts()
    {
        var rule = new Rule("abs-episode", 0, null, MatchStrategy.ByAbsoluteEpisodeNumber, _passAll,
            EpisodeRegex: @"Folge\s*(\d+)");

        var item = TestData.CreateItem(title: "Löwenzahn - Folge 312");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Null(result.Identification.Season);
        Assert.Equal("312", result.Identification.Episode);
    }

    [Fact]
    public void ByAbsoluteEpisodeNumber_no_match()
    {
        var rule = new Rule("abs-nomatch", 0, null, MatchStrategy.ByAbsoluteEpisodeNumber, _passAll,
            EpisodeRegex: @"Folge\s*(\d+)");

        var item = TestData.CreateItem(title: "Tatort: Die goldene Zeit");
        Assert.Null(rule.Match(item, 0.9f));
    }

    [Fact]
    public void Explicit_capture_group()
    {
        var rule = new Rule("capture-group", 0, null, MatchStrategy.SeasonAndEpisodeNumber, _passAll,
            SeasonRegex: @"(S)(\d{2})",
            EpisodeRegex: @"(E)(\d{2})",
            CaptureGroup: 2);

        var item = TestData.CreateItem(title: "Tatort S01E05");
        var result = rule.Match(item, 0.9f);

        Assert.NotNull(result);
        Assert.Equal("01", result.Identification.Season);
        Assert.Equal("05", result.Identification.Episode);
    }
}
