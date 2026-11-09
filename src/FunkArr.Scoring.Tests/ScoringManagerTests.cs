using Akka.Actor;
using Akka.DependencyInjection;
using Akka.TestKit.Xunit;
using FunkArr.Core.Configuration;
using FunkArr.Messages;
using FunkArr.Messages.Scoring;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringManagerTests() : TestKit(CreateConfig())
{
    private static Akka.Actor.Setup.ActorSystemSetup CreateConfig()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptionsMonitor<ScoringOptions>>(
            new TestOptionsMonitor<ScoringOptions>(new ScoringOptions { PoolSize = 1 }));
        var provider = services.BuildServiceProvider();

        return Akka.Actor.Setup.ActorSystemSetup.Create(DependencyResolverSetup.Create(provider));
    }

    private IActorRef CreateManager()
    {
        var resolver = DependencyResolver.For(Sys);
        return Sys.ActorOf(resolver.Props<ScoringManager>());
    }

    private static MatchingConfig CreateAirdateConfig(string ruleSetId = "test", float confidence = 0.9f) =>
        new(ruleSetId, confidence, [
            new MatchingRule("airdate-rule", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AirdateExtraction))
        ]);

    [Fact]
    public void Scores_items_with_loaded_config()
    {
        var manager = CreateManager();

        manager.Tell(CreateAirdateConfig());

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(Guid.Empty, "test", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
        Assert.Equal(0.9, item.Score, 0.001);
    }

    [Fact]
    public void Returns_zeros_for_unknown_ruleset_id()
    {
        var manager = CreateManager();

        var candidates = new[]
        {
            new ScoreCandidate("Test Title", "Test", "ARD", 5400, 720, null, 0),
            new ScoreCandidate("Another Title", "Test", "ZDF", 3600, 1080, null, 0)
        };

        manager.Tell(new ScoreItems(Guid.Empty, "nonexistent", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));
        var result = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Results.Length);
        Assert.All(result.Results, r =>
        {
            Assert.Equal(0.0, r.Score);
            Assert.False(r.Matched);
        });
    }

    [Fact]
    public void Config_update_replaces_previous()
    {
        var manager = CreateManager();

        manager.Tell(CreateAirdateConfig("test", 0.5f));
        manager.Tell(CreateAirdateConfig("test", 0.99f));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(Guid.Empty, "test", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Results);
        Assert.True(item.Matched);
        Assert.Equal(0.99, item.Score, 0.001);
    }

    [Fact]
    public void Multiple_configs_stored_independently()
    {
        var manager = CreateManager();

        manager.Tell(CreateAirdateConfig("show-a", 0.8f));
        manager.Tell(CreateAirdateConfig("show-b", 0.6f));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };

        manager.Tell(new ScoreItems(Guid.Empty, "show-a", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));
        var resultA = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var itemA = Assert.Single(resultA.Results);
        Assert.Equal(0.8, itemA.Score, 0.001);

        manager.Tell(new ScoreItems(Guid.Empty, "show-b", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));
        var resultB = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var itemB = Assert.Single(resultB.Results);
        Assert.Equal(0.6, itemB.Score, 0.001);
    }

    [Fact]
    public void Unknown_ruleset_returns_request_id()
    {
        var manager = CreateManager();
        var requestId = Guid.NewGuid();

        var candidates = new[] { new ScoreCandidate("Test", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(requestId, "nonexistent", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(requestId, result.RequestId);
    }

    [Fact]
    public void Remove_config_returns_zeros_for_removed_ruleset()
    {
        var manager = CreateManager();

        manager.Tell(CreateAirdateConfig());
        manager.Tell(new RemoveMatchingConfig("test"));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(Guid.Empty, "test", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void Remove_unknown_config_does_not_error()
    {
        var manager = CreateManager();

        manager.Tell(new RemoveMatchingConfig("nonexistent"));

        var candidates = new[] { new ScoreCandidate("Test", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(Guid.Empty, "test", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Results);
        Assert.False(item.Matched);
    }

    [Fact]
    public void Remove_config_does_not_affect_other_rulesets()
    {
        var manager = CreateManager();

        manager.Tell(CreateAirdateConfig("show-a", 0.8f));
        manager.Tell(CreateAirdateConfig("show-b", 0.6f));

        manager.Tell(new RemoveMatchingConfig("show-a"));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };

        manager.Tell(new ScoreItems(Guid.Empty, "show-b", new ScoringOrigin(SearchSource.Sonarr, "test"), candidates));
        var result = ExpectMsg<ScoreCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Results);
        Assert.Equal(0.6, item.Score, 0.001);
        Assert.True(item.Matched);
    }
}
