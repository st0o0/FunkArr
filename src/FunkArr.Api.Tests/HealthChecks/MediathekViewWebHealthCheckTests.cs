using System.Net;
using FunkArr.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace FunkArr.Api.Tests.HealthChecks;

public sealed class MediathekViewWebHealthCheckTests
{
    [Fact]
    public async Task Returns_healthy_when_reachable()
    {
        var factory = CreateFactory(HttpStatusCode.OK);
        var check = new MediathekViewWebHealthCheck(factory);
        var context = CreateContext(HealthStatus.Degraded);

        var result = await check.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Returns_degraded_when_server_error()
    {
        var factory = CreateFactory(HttpStatusCode.ServiceUnavailable);
        var check = new MediathekViewWebHealthCheck(factory);
        var context = CreateContext(HealthStatus.Degraded);

        var result = await check.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("503", result.Description);
    }

    [Fact]
    public async Task Returns_degraded_when_unreachable()
    {
        var factory = CreateThrowingFactory();
        var check = new MediathekViewWebHealthCheck(factory);
        var context = CreateContext(HealthStatus.Degraded);

        var result = await check.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("unreachable", result.Description);
    }

    private static IHttpClientFactory CreateFactory(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler(statusCode);
        return CreateFactory(new HttpClient(handler));
    }

    private static IHttpClientFactory CreateThrowingFactory()
    {
        var handler = new ThrowingHttpMessageHandler();
        return CreateFactory(new HttpClient(handler));
    }

    private static IHttpClientFactory CreateFactory(HttpClient client)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);
        return factory;
    }

    private static HealthCheckContext CreateContext(HealthStatus failureStatus) =>
        new()
        {
            Registration = new HealthCheckRegistration(
                "test", _ => null!, failureStatus, null)
        };

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }
}
