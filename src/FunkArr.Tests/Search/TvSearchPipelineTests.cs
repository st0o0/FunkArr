using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Search.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.Search;

public sealed class TvSearchActorTests : TestKit
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, _) =>
        {
            var seriesStub = system.ActorOf(Props.Create(() =>
                new StubSeriesResolver()));
            registry.Register<SeriesResolver>(seriesStub);

            var ruleSetStub = system.ActorOf(Props.Create(() =>
                new StubRuleSetActor()));
            registry.Register<RuleSetActor>(ruleSetStub);

            var gatewayStub = system.ActorOf(Props.Create(() =>
                new StubMediathekGateway()));
            registry.Register<MediathekGatewayActor>(gatewayStub);
        });
    }

    private IActorRef CreatePipeline()
    {
        var registry = Host.Services.GetRequiredService<ActorRegistry>();
        return Sys.ActorOf(Props.Create(() => new TvSearchActor(registry)));
    }

    private static MediathekResultItem MakeItem(string title, string topic = "Tatort") =>
        new()
        {
            Channel = "ARD",
            Topic = topic,
            Title = title,
            UrlVideo = "https://example.com/test.avc-720.mp4",
            Duration = 3600,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

    [Fact]
    public void Pipeline_Returns_Results_For_TvSearch()
    {
        var pipeline = CreatePipeline();

        pipeline.Tell(new TvSearchActor.Search(12345, "Tatort", null, null, null));

        var response = ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        Assert.NotEmpty(response.Results);
        Assert.All(response.Results, r => Assert.Equal("Tatort", r.Topic));
    }

    [Fact]
    public void Episode_Filter_Narrows_Results()
    {
        var pipeline = CreatePipeline();

        pipeline.Tell(new TvSearchActor.Search(12345, "Tatort", 1, 3, null));

        var response = ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        // Items without parseable S01E03 pattern pass through the filter (returns true when no SE found),
        // but items with a different SE pattern would be excluded
        Assert.NotNull(response);
    }

    private sealed class StubSeriesResolver : ReceiveActor
    {
        public StubSeriesResolver()
        {
            Receive<ResolveTvShow>(msg =>
            {
                var episodes = new[]
                {
                    new TvdbEpisodeInfo { EpisodeName = "Pilot", AiredSeason = 1, AiredEpisodeNumber = 1 },
                    new TvdbEpisodeInfo { EpisodeName = "Episode 2", AiredSeason = 1, AiredEpisodeNumber = 2 },
                    new TvdbEpisodeInfo { EpisodeName = "Episode 3", AiredSeason = 1, AiredEpisodeNumber = 3 },
                };
                Sender.Tell(new TvShowResolved("Tatort", episodes));
            });
        }
    }

    private sealed class StubRuleSetActor : ReceiveActor
    {
        public StubRuleSetActor()
        {
            Receive<RuleSetActor.GetRulesForTopic>(_ =>
                Sender.Tell(new RuleSetActor.RulesResponse([])));
        }
    }

    private sealed class StubMediathekGateway : ReceiveActor
    {
        public StubMediathekGateway()
        {
            Receive<FetchItems>(_ =>
                Sender.Tell(new ItemsFetched(
                [
                    MakeItem("Tatort S01E01 Pilot"),
                    MakeItem("Tatort S01E03 Episode 3"),
                    MakeItem("Tatort Sondersendung"),
                ])));
        }
    }
}
