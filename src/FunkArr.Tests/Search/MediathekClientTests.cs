using System.Text.Json;
using FunkArr.Search;
using FunkArr.Tests.Shared;

namespace FunkArr.Tests.Search;

public sealed class MediathekClientTests
{
    private static MediathekClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new HttpClient(new FakeHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://mediathekviewweb.de/"),
        }, Microsoft.Extensions.Logging.Abstractions.NullLogger<MediathekClient>.Instance);

    [Fact]
    public async Task QueryAsync_Success_ReturnsDeserializedResponse()
    {
        var response = new
        {
            result = new
            {
                results = new[]
                {
                    new { channel = "ARD", topic = "Tatort", title = "Folge 1", description = "", timestamp = 1700000000L, duration = 5400, size = 1_000_000L, url_website = "", url_video = "https://example.com/video.mp4", url_video_low = "", url_video_hd = "", url_subtitle = "" },
                },
                queryInfo = new { filmlisteTimestamp = "", searchEngineTime = "5ms", resultCount = 1, totalResults = 1 },
            },
        };
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response)));

        var query = new MediathekQuery
        {
            Queries = [new MediathekQueryItem { Fields = ["topic"], Query = "Tatort" }],
        };
        var result = await client.QueryAsync(query);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result.Results);
        Assert.Equal("ARD", result.Result.Results[0].Channel);
        Assert.Equal("Tatort", result.Result.Results[0].Topic);
    }

    [Fact]
    public async Task QueryAsync_HttpError_ReturnsNull()
    {
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.InternalServerError));

        var query = new MediathekQuery
        {
            Queries = [new MediathekQueryItem { Fields = ["topic"], Query = "Tatort" }],
        };
        var result = await client.QueryAsync(query);

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_MalformedJson_ReturnsNull()
    {
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse("not valid json"));

        var query = new MediathekQuery
        {
            Queries = [new MediathekQueryItem { Fields = ["topic"], Query = "Tatort" }],
        };
        var result = await client.QueryAsync(query);

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_EmptyResult_ReturnsEmptyArray()
    {
        var response = new { result = new { results = Array.Empty<object>(), queryInfo = new { filmlisteTimestamp = "", searchEngineTime = "1ms", resultCount = 0, totalResults = 0 } } };
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response)));

        var query = new MediathekQuery
        {
            Queries = [new MediathekQueryItem { Fields = ["topic"], Query = "nonexistent" }],
        };
        var result = await client.QueryAsync(query);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result.Results);
    }
}
