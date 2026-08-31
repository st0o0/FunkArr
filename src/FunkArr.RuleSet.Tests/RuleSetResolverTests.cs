using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Messages.RuleSet;
using Xunit;

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
}
