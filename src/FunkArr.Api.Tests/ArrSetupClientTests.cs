using System.Net;

namespace FunkArr.Api.Tests;

public sealed class ArrSetupClientTests
{
    private static readonly ArrConnection _connection = new("http://localhost:8989", "test-api-key");

    private static ArrSetupClient CreateClient(DelegatingHandler handler)
    {
        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        return new ArrSetupClient(httpClient);
    }

    private static StubHandler Respond(HttpStatusCode statusCode, string body) =>
        new(() => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });

    [Fact]
    public async Task PostResourceAsync_SuccessResponse_ReturnsIdAndName()
    {
        using var handler = Respond(HttpStatusCode.Created, """{"id":42,"name":"FunkArr"}""");
        var client = CreateClient(handler);

        var result = await client.PostResourceAsync(_connection, "/api/v1/indexer", new { Name = "test" });

        Assert.True(result.Success);
        Assert.Equal(42, result.Id);
        Assert.Equal("FunkArr", result.Name);
        Assert.Null(result.Error);
        Assert.Null(result.Message);
        Assert.Null(result.ValidationErrors);
    }

    [Fact]
    public async Task PostResourceAsync_SuccessWithProviderMessage_ReturnsMessage()
    {
        using var handler = Respond(HttpStatusCode.Created,
            """{"id":1,"name":"FunkArr","message":{"message":"Test warning","type":"warning"}}""");
        var client = CreateClient(handler);

        var result = await client.PostResourceAsync(_connection, "/api/v1/indexer", new { Name = "test" });

        Assert.True(result.Success);
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Message);
        Assert.Equal("Test warning", result.Message.Message);
        Assert.Equal("warning", result.Message.Type);
    }

    [Fact]
    public async Task PostResourceAsync_ValidationError_ReturnsValidationErrors()
    {
        using var handler = Respond(HttpStatusCode.BadRequest,
            """[{"propertyName":"BaseUrl","errorMessage":"is required","isWarning":false}]""");
        var client = CreateClient(handler);

        var result = await client.PostResourceAsync(_connection, "/api/v1/indexer", new { Name = "test" });

        Assert.False(result.Success);
        Assert.NotNull(result.ValidationErrors);
        var error = Assert.Single(result.ValidationErrors);
        Assert.Equal("BaseUrl", error.PropertyName);
        Assert.Equal("is required", error.ErrorMessage);
        Assert.False(error.IsWarning);
    }

    [Fact]
    public async Task PostResourceAsync_ServerError_ReturnsErrorMessage()
    {
        using var handler = Respond(HttpStatusCode.InternalServerError,
            """{"message":"Internal error"}""");
        var client = CreateClient(handler);

        var result = await client.PostResourceAsync(_connection, "/api/v1/indexer", new { Name = "test" });

        Assert.False(result.Success);
        Assert.Equal("Internal error", result.Error);
    }

    [Fact]
    public async Task PostResourceAsync_Timeout_ReturnsTimedOutError()
    {
        using var handler = new StubHandler(() => throw new TaskCanceledException("Request timed out"));
        var client = CreateClient(handler);

        var result = await client.PostResourceAsync(_connection, "/api/v1/indexer", new { Name = "test" });

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("timed out", result.Error);
    }

    [Fact]
    public async Task PostResourceAsync_InvalidUrl_ReturnsInvalidUrlError()
    {
        var connection = new ArrConnection("ftp://invalid-scheme", "key");
        using var handler = new StubHandler(() => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        var result = await client.PostResourceAsync(connection, "/api/v1/indexer", new { Name = "test" });

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("Invalid URL", result.Error);
    }

    [Fact]
    public async Task PostResourceAsync_ConnectionFailure_ReturnsConnectionFailedError()
    {
        using var handler = new StubHandler(() => throw new HttpRequestException("No connection could be made"));
        var client = CreateClient(handler);

        var result = await client.PostResourceAsync(_connection, "/api/v1/indexer", new { Name = "test" });

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("Connection failed", result.Error);
    }

    private sealed class StubHandler(Func<HttpResponseMessage> responseFactory) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responseFactory());
    }
}
