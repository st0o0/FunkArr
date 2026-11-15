using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Messages.Mediathek;
using FunkArr.Search.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkArr.Search.Tests;

public sealed class MediathekViewWebManagerTests : TestKit
{
    private static QueryMediathek CreateQuery(string topic = "Tatort") =>
        new([new MediathekQueryField(["topic"], topic)], null, null, false, 0, 10, null, null);

    private static MediathekClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://mediathekviewweb.de/api/query") },
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 }),
            NullLogger<MediathekClient>.Instance);

    [Fact]
    public void Successful_query_returns_completed()
    {
        var json = """{"result":{"results":[],"queryInfo":{"totalResults":0,"filmlisteTimestamp":0}}}""";
        var client = CreateClient(new SuccessHandler(json));
        var manager = Sys.ActorOf(Props.Create(() => new MediathekViewWebManager(client)));

        manager.Tell(CreateQuery(), TestActor);

        var result = ExpectMsg<QueryMediathekCompleted>(
            TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Http_error_returns_error_response()
    {
        var client = CreateClient(new ErrorHandler());
        var manager = Sys.ActorOf(Props.Create(() => new MediathekViewWebManager(client)));

        manager.Tell(CreateQuery(), TestActor);

        var result = ExpectMsg<QueryMediathekError>(
            TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result.Cause);
    }

    [Fact]
    public void Queue_full_returns_queue_full_response()
    {
        var client = CreateClient(new NeverRespondHandler());
        var manager = Sys.ActorOf(Props.Create(() => new MediathekViewWebManager(client, maxConcurrent: 1)));

        for (var i = 0; i < 80; i++)
        {
            manager.Tell(CreateQuery($"query-{i}"), TestActor);
        }

        ExpectMsg<QueryMediathekQueueFull>(TimeSpan.FromSeconds(10),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private sealed class SuccessHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private sealed class ErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }

    private sealed class NeverRespondHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}
