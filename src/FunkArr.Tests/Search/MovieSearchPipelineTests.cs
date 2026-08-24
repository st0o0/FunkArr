using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using FunkArr.Search;
using FunkArr.Search.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.Search;

public sealed class MovieSearchActorTests : TestKit
{
    private readonly List<string> _gatewayQueries = [];

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, _) =>
        {
            var resolverStub = system.ActorOf(Props.Create(() =>
                new StubMovieResolver()));
            registry.Register<MovieResolver>(resolverStub);

            var gatewayStub = system.ActorOf(Props.Create(() =>
                new StubMediathekGateway(this)));
            registry.Register<MediathekGatewayActor>(gatewayStub);
        });
    }

    private IActorRef CreatePipeline()
    {
        var registry = Host.Services.GetRequiredService<ActorRegistry>();
        return Sys.ActorOf(Props.Create(() => new MovieSearchActor(registry)));
    }

    private static MediathekResultItem MakeItem(string title = "Der Untergang", string topic = "Filme") =>
        new()
        {
            Channel = "ARD",
            Topic = topic,
            Title = title,
            UrlVideo = "https://example.com/movie.avc-720.mp4",
            Duration = 7200,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

    [Fact]
    public void Pipeline_Returns_Results_For_MovieSearch()
    {
        var pipeline = CreatePipeline();

        pipeline.Tell(new MovieSearchActor.Search("tt1234567", null));

        var response = ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        Assert.NotEmpty(response.Results);
        Assert.All(response.Results, r => Assert.Equal("ARD", r.Channel));
    }

    [Fact]
    public void OriginalTitle_Fallback_When_Title_Returns_No_Results()
    {
        var pipeline = CreatePipeline();

        pipeline.Tell(new MovieSearchActor.Search("tt9999999", null));

        var response = ExpectMsg<SearchResponse>(TimeSpan.FromSeconds(10));

        Assert.NotEmpty(response.Results);
        // The fallback query "Downfall" should have been tried after "Kein Ergebnis" returned 0 items
        Assert.True(_gatewayQueries.Count >= 2,
            $"Expected at least 2 gateway queries (title + originalTitle fallback), got {_gatewayQueries.Count}");
    }

    private sealed class StubMovieResolver : ReceiveActor
    {
        public StubMovieResolver()
        {
            Receive<ResolveMovie>(msg =>
            {
                TmdbMovieInfo info;
                if (msg.ImdbId == "tt9999999")
                {
                    // German title returns no Mediathek results, original English title does
                    info = new TmdbMovieInfo
                    {
                        Title = "Verschollen",
                        OriginalTitle = "Gone Girl",
                        RuntimeMinutes = 120,
                    };
                }
                else
                {
                    info = new TmdbMovieInfo
                    {
                        Title = "Der Untergang",
                        OriginalTitle = "Der Untergang",
                        RuntimeMinutes = 120,
                    };
                }

                Sender.Tell(new MovieResolved(info));
            });
        }
    }

    private sealed class StubMediathekGateway : ReceiveActor
    {
        public StubMediathekGateway(MovieSearchActorTests parent)
        {
            Receive<FetchItems>(msg =>
            {
                parent._gatewayQueries.Add(msg.SearchTerm);

                // "Verschollen" returns empty (triggers originalTitle fallback)
                if (msg.SearchTerm == "Verschollen")
                {
                    Sender.Tell(new ItemsFetched([]));
                    return;
                }

                // Fallback query "Gone Girl": Mediathek returns items with the title in Topic
                // so MatchesShow passes (Topic contains ShowName or vice versa)
                if (msg.SearchTerm == "Gone Girl")
                {
                    Sender.Tell(new ItemsFetched([MakeItem("Gone Girl - Verschollen", "Verschollen")]));
                    return;
                }

                Sender.Tell(new ItemsFetched([MakeItem(msg.SearchTerm)]));
            });
        }
    }
}
