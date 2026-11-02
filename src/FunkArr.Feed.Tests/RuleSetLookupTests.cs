using FunkArr.Messages;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests;

public sealed class RuleSetLookupTests
{
    private static readonly RegisteredRuleSetEntry[] _entries =
    [
        new("tatort", "Tatort", ["Tatort (ARD)"], new ExternalIds(null, null, null), "Tatort", MediaType.Show),
        new("das-boot", "Das Boot", [], new ExternalIds(null, "tt0082096", 387), "Das Boot", MediaType.Movie),
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
}
