using FunkArr.Messages;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests;

public sealed class RuleSetLookupTests
{
    private static readonly RegisteredRuleSetEntry[] _entries =
    [
        new("tatort", "Tatort", ["Tatort (ARD)"], new ExternalIds(null, null, null), "Tatort", MediaType.Show),
        new("das-boot", "Das Boot", [], new ExternalIds(null, null, null), "Das Boot", MediaType.Movie),
        new("heute-show", "heute-show", [], new ExternalIds(null, null, null), null, null)
    ];

    [Theory]
    [InlineData("Tatort", "tatort", MediaType.Show)]
    [InlineData("tatort (ard)", "tatort", MediaType.Show)]
    [InlineData(" Das Boot ", "das-boot", MediaType.Movie)]
    [InlineData("heute-show", "heute-show", MediaType.Show)]
    public void TryMatch_finds_topic_and_alias_case_insensitively(string topic, string expectedId, MediaType expectedType)
    {
        var lookup = RuleSetLookup.From(_entries);

        Assert.True(lookup.TryMatch(topic, out var ruleSetId, out var mediaType));
        Assert.Multiple(
            () => Assert.Equal(expectedId, ruleSetId),
            () => Assert.Equal(expectedType, mediaType));
    }

    [Fact]
    public void Group_collects_items_per_ruleset_and_counts_unmatched()
    {
        var lookup = RuleSetLookup.From(_entries);

        var result = lookup.Group(
        [
            TestData.Item("a", "Tatort"),
            TestData.Item("b", "Tatort (ARD)"),
            TestData.Item("c", "Das Boot"),
            TestData.Item("d", "Unbekannt")
        ]);

        Assert.Equal(1, result.Unmatched);
        Assert.Equal(2, result.Groups.Length);
        var tatort = Assert.Single(result.Groups, g => g.RuleSetId == "tatort");
        var boot = Assert.Single(result.Groups, g => g.RuleSetId == "das-boot");
        Assert.Multiple(
            () => Assert.Equal(2, tatort.Items.Length),
            () => Assert.Equal(MediaType.Movie, boot.MediaType));
    }
}
