using System.Net;
using System.Net.Http.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FunkArr.Api.Tests;

public sealed class EndpointExceptionFilterTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private ActorSystem _actorSystem = null!;

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        _actorSystem = ActorSystem.Create("endpoint-exception-filter-tests");
        var registry = ActorRegistry.For(_actorSystem);
        registry.Register<IMediathekManager>(_actorSystem.ActorOf(Props.Create(() => new UnknownResponder())));
        builder.Services.AddSingleton<IActorRegistry>(registry);

        _app = builder.Build();
        _app.MapMediathekApi();

        var group = _app.MapGroup("/test").AddEndpointFilter<EndpointExceptionFilter>();
        group.MapGet("/ask-timeout", IResult () => throw new AskTimeoutException("timed out"));
        group.MapGet("/timeout", IResult () => throw new TimeoutException("timed out"));
        group.MapGet("/failure", IResult () => throw new InvalidOperationException("boom"));

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _actorSystem.Terminate();
    }

    [Theory]
    [InlineData("/test/ask-timeout")]
    [InlineData("/test/timeout")]
    public async Task Timeouts_return_gateway_timeout(string path)
    {
        var response = await Client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal("Gateway Timeout", problem.Title);
    }

    [Fact]
    public async Task Unhandled_exception_returns_internal_server_error()
    {
        var response = await Client.GetAsync("/test/failure", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal("Internal Server Error", problem.Title);
    }

    [Fact]
    public async Task Unexpected_actor_response_returns_internal_server_error()
    {
        var response = await Client.GetAsync("/api/mediathek/search?q=tatort", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal("Internal Server Error", problem.Title);
    }

    [Fact]
    public void Unexpected_actor_response_exception_names_the_response_type()
    {
        var ex = new UnexpectedActorResponseException(new UnknownMediathekResponse());

        Assert.Multiple(
            () => Assert.Equal(nameof(UnknownMediathekResponse), ex.ResponseType),
            () => Assert.Equal($"Unexpected actor response: {nameof(UnknownMediathekResponse)}", ex.Message));
    }

    private sealed record UnknownMediathekResponse : QueryMediathekResponse;

    private sealed class UnknownResponder : ReceiveActor
    {
        public UnknownResponder() => ReceiveAny(_ => Sender.Tell(new UnknownMediathekResponse()));
    }
}
