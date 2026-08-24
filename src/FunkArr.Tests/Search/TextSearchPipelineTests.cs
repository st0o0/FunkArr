using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using FunkArr.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.Search;

public sealed class TextSearchActorTests : TestKit
{
    private int _fetchCount;

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, _) =>
        {
            var gatewayStub = system.ActorOf(Props.Create(() =>
                new StubMediathekGateway(this)));
            registry.Register<MediathekGatewayActor>(gatewayStub);
        });
    }

    private IActorRef CreatePipeline()
    {
        var registry = Host.Services.GetRequiredService<ActorRegistry>();
        return Sys.ActorOf(Props.Create(() => new TextSearchActor(registry)));
    }

    private static MediathekResultItem MakeItem(string title = "Test Episode", string topic = "Test") =>
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
    public void Pipeline_Returns_Results_For_Search()
    {
        var pipeline = CreatePipeline();

        pipeline.Tell(new TextSearchActor.Search("Test"));

        var response = ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        Assert.NotEmpty(response.Results);
        Assert.All(response.Results, r => Assert.Equal("ARD", r.Channel));
    }

    [Fact]
    public void Empty_Query_Returns_Results()
    {
        var pipeline = CreatePipeline();

        pipeline.Tell(new TextSearchActor.Search(""));

        var response = ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        Assert.NotEmpty(response.Results);
    }

    [Fact]
    public void Cache_Hit_Does_Not_Fetch_Again()
    {
        var pipeline = CreatePipeline();

        pipeline.Tell(new TextSearchActor.Search("Test"));
        ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        var countAfterFirst = _fetchCount;

        pipeline.Tell(new TextSearchActor.Search("Test"));
        ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        Assert.Equal(countAfterFirst, _fetchCount);
    }

    private sealed class StubMediathekGateway : ReceiveActor
    {
        public StubMediathekGateway(TextSearchActorTests parent)
        {
            Receive<FetchItems>(_ =>
            {
                Interlocked.Increment(ref parent._fetchCount);
                Sender.Tell(new ItemsFetched([MakeItem(), MakeItem("Another Episode")]));
            });
        }
    }
}
