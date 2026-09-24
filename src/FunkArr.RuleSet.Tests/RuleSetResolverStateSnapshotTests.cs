using FunkArr.Messages;
using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetResolverStateSnapshotTests
{
    [Fact]
    public void GetSnapshot_ReturnsRegisteredRuleSetsResult()
    {
        var state = RuleSetResolverState.Empty
            .Apply(new RegisterRuleSet("tatort", "Tatort", ["tatort-krimi"], 12345, "tt1234", 67890, "Tatort", MediaType.Show, null));

        var snapshot = state.GetSnapshot();

        Assert.Single(snapshot.Entries);
        Assert.Equal("tatort", snapshot.Entries[0].RuleSetId);
        Assert.Equal("Tatort", snapshot.Entries[0].Topic);
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesEntries()
    {
        var state = RuleSetResolverState.Empty
            .Apply(new RegisterRuleSet("tatort", "Tatort", ["tatort-krimi"], 12345, "tt1234", 67890, "Tatort", MediaType.Show, null))
            .Apply(new RegisterRuleSet("heute", "heute show", [], null, null, null, null, null, null));

        var snapshot = state.GetSnapshot();
        var restored = RuleSetResolverStateExtensions.FromSnapshot(snapshot);

        var restoredSnapshot = restored.GetSnapshot();
        Assert.Equal(snapshot.Entries.Length, restoredSnapshot.Entries.Length);
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesLookupIndex()
    {
        var state = RuleSetResolverState.Empty
            .Apply(new RegisterRuleSet("tatort", "Tatort", ["tatort-krimi"], null, null, null, null, null, null));

        var restored = RuleSetResolverStateExtensions.FromSnapshot(state.GetSnapshot());

        var resolved = restored.Resolve(new ResolveRuleSet("tatort-krimi", null, null, null));
        Assert.IsType<RuleSetResolved>(resolved);
        Assert.Equal("tatort", ((RuleSetResolved)resolved).RuleSetId);
    }

}
