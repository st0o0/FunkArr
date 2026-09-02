using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetResolverTests : TestKit
{
    [Fact]
    public void Resolves_by_topic()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("die-anstalt", "Die Anstalt", []));

        resolver.Tell(new ResolveRuleSet("Die Anstalt"));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("die-anstalt", result.RuleSetId);
    }

    [Fact]
    public void Resolves_by_alias()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("bares-fuer-rares", "Bares für Rares",
            ["Bares für Rares - die tägliche Show"]));

        resolver.Tell(new ResolveRuleSet("Bares für Rares - die tägliche Show"));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("bares-fuer-rares", result.RuleSetId);
    }

    [Fact]
    public void Returns_not_found_for_unknown_topic()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new ResolveRuleSet("Unknown Show"));
        var result = ExpectMsg<RuleSetNotFound>();

        Assert.Equal("Unknown Show", result.TopicOrAlias);
    }

    [Fact]
    public void Re_registration_overwrites_previous_entries()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("test-show", "Test Show", ["Old Alias"]));
        resolver.Tell(new RegisterRuleSet("test-show", "Test Show", ["New Alias"]));

        resolver.Tell(new ResolveRuleSet("Old Alias"));
        ExpectMsg<RuleSetNotFound>();

        resolver.Tell(new ResolveRuleSet("New Alias"));
        var result = ExpectMsg<RuleSetResolved>();
        Assert.Equal("test-show", result.RuleSetId);

        resolver.Tell(new ResolveRuleSet("Test Show"));
        var topicResult = ExpectMsg<RuleSetResolved>();
        Assert.Equal("test-show", topicResult.RuleSetId);
    }

    [Fact]
    public void Lookup_is_case_insensitive()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("die-anstalt", "Die Anstalt", []));

        resolver.Tell(new ResolveRuleSet("die anstalt"));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("die-anstalt", result.RuleSetId);
    }

    [Fact]
    public void Multiple_rulesets_do_not_interfere()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("show-a", "Show A", ["Alias A"]));
        resolver.Tell(new RegisterRuleSet("show-b", "Show B", ["Alias B"]));

        resolver.Tell(new ResolveRuleSet("Show A"));
        Assert.Equal("show-a", ExpectMsg<RuleSetResolved>().RuleSetId);

        resolver.Tell(new ResolveRuleSet("Show B"));
        Assert.Equal("show-b", ExpectMsg<RuleSetResolved>().RuleSetId);

        resolver.Tell(new ResolveRuleSet("Alias A"));
        Assert.Equal("show-a", ExpectMsg<RuleSetResolved>().RuleSetId);

        resolver.Tell(new ResolveRuleSet("Alias B"));
        Assert.Equal("show-b", ExpectMsg<RuleSetResolved>().RuleSetId);
    }

    [Fact]
    public void Deregister_removes_topic_and_aliases()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("test-show", "Test Show", ["Test Alias"]));

        resolver.Tell(new ResolveRuleSet("Test Show"));
        ExpectMsg<RuleSetResolved>();

        resolver.Tell(new DeregisterRuleSet("test-show"));

        resolver.Tell(new ResolveRuleSet("Test Show"));
        ExpectMsg<RuleSetNotFound>();

        resolver.Tell(new ResolveRuleSet("Test Alias"));
        ExpectMsg<RuleSetNotFound>();
    }

    [Fact]
    public void Deregister_unknown_does_not_error()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new DeregisterRuleSet("nonexistent"));

        resolver.Tell(new ResolveRuleSet("anything"));
        ExpectMsg<RuleSetNotFound>();
    }

    [Fact]
    public void Deregister_does_not_affect_other_rulesets()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("show-a", "Show A", []));
        resolver.Tell(new RegisterRuleSet("show-b", "Show B", []));

        resolver.Tell(new DeregisterRuleSet("show-a"));

        resolver.Tell(new ResolveRuleSet("Show A"));
        ExpectMsg<RuleSetNotFound>();

        resolver.Tell(new ResolveRuleSet("Show B"));
        Assert.Equal("show-b", ExpectMsg<RuleSetResolved>().RuleSetId);
    }

    [Fact]
    public void Resolves_by_tvdbId()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", [], TvdbId: 83214));

        resolver.Tell(new ResolveRuleSet(null, TvdbId: 83214));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("tatort", result.RuleSetId);
        Assert.Equal("Tatort", result.Topic);
    }

    [Fact]
    public void Resolves_by_imdbId()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", [], ImdbId: "tt0806910"));

        resolver.Tell(new ResolveRuleSet(null, ImdbId: "tt0806910"));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("tatort", result.RuleSetId);
        Assert.Equal("Tatort", result.Topic);
    }

    [Fact]
    public void Resolves_by_tmdbId()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", [], TmdbId: 2116));

        resolver.Tell(new ResolveRuleSet(null, TmdbId: 2116));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("tatort", result.RuleSetId);
        Assert.Equal("Tatort", result.Topic);
    }

    [Fact]
    public void Topic_lookup_takes_precedence_over_id()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", [], TvdbId: 83214));

        resolver.Tell(new ResolveRuleSet("Tatort", TvdbId: 99999));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("tatort", result.RuleSetId);
    }

    [Fact]
    public void Id_resolve_returns_not_found_for_unknown_id()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new ResolveRuleSet(null, TvdbId: 99999));
        ExpectMsg<RuleSetNotFound>();
    }

    [Fact]
    public void Re_registration_updates_id_index()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", [], TvdbId: 83214));
        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", [], TvdbId: 99999));

        resolver.Tell(new ResolveRuleSet(null, TvdbId: 83214));
        ExpectMsg<RuleSetNotFound>();

        resolver.Tell(new ResolveRuleSet(null, TvdbId: 99999));
        Assert.Equal("tatort", ExpectMsg<RuleSetResolved>().RuleSetId);
    }

    [Fact]
    public void Deregister_removes_id_index_entries()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", [], TvdbId: 83214, ImdbId: "tt0806910"));

        resolver.Tell(new DeregisterRuleSet("tatort"));

        resolver.Tell(new ResolveRuleSet(null, TvdbId: 83214));
        ExpectMsg<RuleSetNotFound>();

        resolver.Tell(new ResolveRuleSet(null, ImdbId: "tt0806910"));
        ExpectMsg<RuleSetNotFound>();
    }

    [Fact]
    public void Resolved_includes_topic()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", []));

        resolver.Tell(new ResolveRuleSet("Tatort"));
        var result = ExpectMsg<RuleSetResolved>();

        Assert.Equal("tatort", result.RuleSetId);
        Assert.Equal("Tatort", result.Topic);
    }

    [Fact]
    public void QueryAll_returns_empty_when_no_rulesets()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new QueryRegisteredRuleSets());
        var result = ExpectMsg<RegisteredRuleSetsResult>();

        Assert.Empty(result.Entries);
    }

    [Fact]
    public void QueryAll_returns_single_ruleset()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("tatort", "Tatort", ["Tatort Münster"], TvdbId: 83214, ImdbId: "tt0806910"));

        resolver.Tell(new QueryRegisteredRuleSets());
        var result = ExpectMsg<RegisteredRuleSetsResult>();

        var entry = Assert.Single(result.Entries);
        Assert.Equal("tatort", entry.RuleSetId);
        Assert.Equal("Tatort", entry.Topic);
        Assert.Contains("Tatort Münster", entry.Aliases);
        Assert.Equal(83214, entry.TvdbId);
        Assert.Equal("tt0806910", entry.ImdbId);
    }

    [Fact]
    public void QueryAll_returns_multiple_rulesets()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("show-a", "Show A", []));
        resolver.Tell(new RegisterRuleSet("show-b", "Show B", ["Alias B"], TmdbId: 42));
        resolver.Tell(new RegisterRuleSet("show-c", "Show C", []));

        resolver.Tell(new QueryRegisteredRuleSets());
        var result = ExpectMsg<RegisteredRuleSetsResult>();

        Assert.Equal(3, result.Entries.Length);
        Assert.Contains(result.Entries, e => e.RuleSetId == "show-a");
        Assert.Contains(result.Entries, e => e.RuleSetId == "show-b" && e.TmdbId == 42);
        Assert.Contains(result.Entries, e => e.RuleSetId == "show-c");
    }

    [Fact]
    public void QueryAll_excludes_deregistered_rulesets()
    {
        var resolver = Sys.ActorOf(Props.Create(() => new RuleSetResolver()));

        resolver.Tell(new RegisterRuleSet("show-a", "Show A", []));
        resolver.Tell(new RegisterRuleSet("show-b", "Show B", []));
        resolver.Tell(new DeregisterRuleSet("show-a"));

        resolver.Tell(new QueryRegisteredRuleSets());
        var result = ExpectMsg<RegisteredRuleSetsResult>();

        var entry = Assert.Single(result.Entries);
        Assert.Equal("show-b", entry.RuleSetId);
    }
}
