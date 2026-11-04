using FunkArr.Messages;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetResolverStateSnapshotTests
{
    [Fact]
    public void GetSnapshot_ReturnsRegisteredRuleSetsResult()
    {
        var state = new RuleSetResolverState();
        state.Apply(new RegisterRuleSet("tatort", "Tatort", ["tatort-krimi"], new ExternalIds(12345, "tt1234", 67890), "Tatort", MediaType.Show));

        var snapshot = state.GetSnapshot();

        var item = Assert.Single(snapshot.Entries);
        Assert.Equal("tatort", item.RuleSetId);
        Assert.Equal("Tatort", item.Topic);
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesEntries()
    {
        var state = new RuleSetResolverState();
        state.Apply(new RegisterRuleSet("tatort", "Tatort", ["tatort-krimi"], new ExternalIds(12345, "tt1234", 67890), "Tatort", MediaType.Show));
        state.Apply(new RegisterRuleSet("heute", "heute show", []));

        var snapshot = state.GetSnapshot();
        var restored = RuleSetResolverState.FromSnapshot(snapshot);

        var restoredSnapshot = restored.GetSnapshot();
        Assert.Equal(snapshot.Entries.Length, restoredSnapshot.Entries.Length);
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesLookupIndex()
    {
        var state = new RuleSetResolverState();
        state.Apply(new RegisterRuleSet("tatort", "Tatort", ["tatort-krimi"]));

        var restored = RuleSetResolverState.FromSnapshot(state.GetSnapshot());

        var resolved = restored.Resolve(new ResolveRuleSet("tatort-krimi"));
        Assert.IsType<RuleSetResolved>(resolved);
        Assert.Equal("tatort", ((RuleSetResolved)resolved).RuleSetId);
    }

}
