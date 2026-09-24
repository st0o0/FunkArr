using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetEnumMappingTests
{
    [Theory]
    [InlineData("seasonAndEpisodeNumber", IdentificationStrategy.SeasonAndEpisodeNumber)]
    [InlineData("byAbsoluteEpisodeNumber", IdentificationStrategy.AbsoluteEpisodeNumber)]
    [InlineData("itemTitleExact", IdentificationStrategy.TitleExact)]
    [InlineData("itemTitleIncludes", IdentificationStrategy.TitleIncludes)]
    [InlineData("itemTitleEqualsAirdate", IdentificationStrategy.AirdateExtraction)]
    public void TryParseStrategy_valid_values(string input, IdentificationStrategy expected)
    {
        Assert.True(RuleSetEnumMapping.TryParseStrategy(input, out var result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("TitleIncludes")]
    public void TryParseStrategy_invalid_values(string? input)
    {
        Assert.False(RuleSetEnumMapping.TryParseStrategy(input, out _));
    }

    [Theory]
    [InlineData(IdentificationStrategy.SeasonAndEpisodeNumber, "seasonAndEpisodeNumber")]
    [InlineData(IdentificationStrategy.AbsoluteEpisodeNumber, "byAbsoluteEpisodeNumber")]
    [InlineData(IdentificationStrategy.TitleExact, "itemTitleExact")]
    [InlineData(IdentificationStrategy.TitleIncludes, "itemTitleIncludes")]
    [InlineData(IdentificationStrategy.AirdateExtraction, "itemTitleEqualsAirdate")]
    public void Strategy_ToDiskValue(IdentificationStrategy strategy, string expected)
    {
        Assert.Equal(expected, strategy.ToDiskValue());
    }

    [Theory]
    [InlineData(FilterField.Title, "title")]
    [InlineData(FilterField.Topic, "topic")]
    [InlineData(FilterField.Channel, "channel")]
    [InlineData(FilterField.Description, "description")]
    [InlineData(FilterField.Duration, "duration")]
    [InlineData(FilterField.Timestamp, "timestamp")]
    public void FilterField_ToDiskValue(FilterField field, string expected)
    {
        Assert.Equal(expected, field.ToDiskValue());
    }

    [Theory]
    [InlineData("title", FilterField.Title)]
    [InlineData("duration", FilterField.Duration)]
    public void TryParseFilterField_valid(string input, FilterField expected)
    {
        Assert.True(RuleSetEnumMapping.TryParseFilterField(input, out var result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryParseFilterField_null_returns_false()
    {
        Assert.False(RuleSetEnumMapping.TryParseFilterField(null, out _));
    }

    [Theory]
    [InlineData(FilterOp.Eq, "eq")]
    [InlineData(FilterOp.Contains, "contains")]
    [InlineData(FilterOp.NotContains, "notContains")]
    [InlineData(FilterOp.GreaterThan, "greaterThan")]
    [InlineData(FilterOp.LessThan, "lessThan")]
    [InlineData(FilterOp.Regex, "regex")]
    public void FilterOp_ToDiskValue(FilterOp op, string expected)
    {
        Assert.Equal(expected, op.ToDiskValue());
    }

    [Theory]
    [InlineData("greaterThan", FilterOp.GreaterThan)]
    [InlineData("contains", FilterOp.Contains)]
    public void TryParseFilterOp_valid(string input, FilterOp expected)
    {
        Assert.True(RuleSetEnumMapping.TryParseFilterOp(input, out var result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(TitlePartType.Static, "static")]
    [InlineData(TitlePartType.Regex, "regex")]
    public void TitlePartType_ToDiskValue(TitlePartType type, string expected)
    {
        Assert.Equal(expected, type.ToDiskValue());
    }

    [Theory]
    [InlineData("static", TitlePartType.Static)]
    [InlineData("regex", TitlePartType.Regex)]
    public void TryParseTitlePartType_valid(string input, TitlePartType expected)
    {
        Assert.True(RuleSetEnumMapping.TryParseTitlePartType(input, out var result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(EnrichmentMethod.Title, "title")]
    [InlineData(EnrichmentMethod.Airdate, "airdate")]
    public void EnrichmentMethod_ToDiskValue(EnrichmentMethod method, string expected)
    {
        Assert.Equal(expected, method.ToDiskValue());
    }

    [Theory]
    [InlineData(RuntimeMode.Tiebreaker, "tiebreaker")]
    [InlineData(RuntimeMode.Filter, "filter")]
    public void RuntimeMode_ToDiskValue(RuntimeMode mode, string expected)
    {
        Assert.Equal(expected, mode.ToDiskValue());
    }

    [Theory]
    [InlineData(MediaType.Show, "show")]
    [InlineData(MediaType.Movie, "movie")]
    public void MediaType_ToDiskValue(MediaType type, string expected)
    {
        Assert.Equal(expected, type.ToDiskValue());
    }

    [Theory]
    [InlineData("show", MediaType.Show)]
    [InlineData("movie", MediaType.Movie)]
    public void TryParseMediaType_valid(string input, MediaType expected)
    {
        Assert.True(RuleSetEnumMapping.TryParseMediaType(input, out var result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryParseMediaType_unknown_returns_false()
    {
        Assert.False(RuleSetEnumMapping.TryParseMediaType("podcast", out _));
    }

    [Theory]
    [InlineData(IdentificationStrategy.SeasonAndEpisodeNumber)]
    [InlineData(IdentificationStrategy.AbsoluteEpisodeNumber)]
    [InlineData(IdentificationStrategy.TitleExact)]
    [InlineData(IdentificationStrategy.TitleIncludes)]
    [InlineData(IdentificationStrategy.AirdateExtraction)]
    public void Strategy_roundtrips(IdentificationStrategy strategy)
    {
        var disk = strategy.ToDiskValue();
        Assert.True(RuleSetEnumMapping.TryParseStrategy(disk, out var parsed));
        Assert.Equal(strategy, parsed);
    }
}
