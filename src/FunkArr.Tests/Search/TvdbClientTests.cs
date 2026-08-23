using System.Text.Json;
using FunkArr.Search;
using FunkArr.Tests.Shared;

namespace FunkArr.Tests.Search;

public sealed class TvdbClientTests
{
    private static TvdbClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new HttpClient(new FakeHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://api.thetvdb.com/"),
        });

    [Fact]
    public async Task GetShowAsync_Success_ReturnsShowInfo()
    {
        var response = new { data = new { seriesName = "Tatort", aliases = new[] { "Crime Scene" }, overview = "A German crime series" } };
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response)));

        var result = await client.GetShowAsync(12345);

        Assert.NotNull(result);
        Assert.Equal("Tatort", result.SeriesName);
        Assert.Contains("Crime Scene", result.Aliases);
    }

    [Fact]
    public async Task GetShowAsync_HttpError_ReturnsNull()
    {
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.NotFound));

        var result = await client.GetShowAsync(99999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetShowAsync_VerifiesUrlContainsTvdbId()
    {
        string? capturedUrl = null;
        var response = new { data = new { seriesName = "Test", aliases = Array.Empty<string>(), overview = "" } };
        var client = CreateClient(req =>
        {
            capturedUrl = req.RequestUri!.AbsolutePath;
            return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response));
        });

        await client.GetShowAsync(42);

        Assert.NotNull(capturedUrl);
        Assert.Contains("/series/42", capturedUrl);
    }

    [Fact]
    public async Task GetEpisodesAsync_Success_ReturnsEpisodeArray()
    {
        var response = new
        {
            data = new[]
            {
                new { episodeName = "Pilot", airedSeason = 1, airedEpisodeNumber = 1, firstAired = "2020-01-01", overview = "" },
                new { episodeName = "Second", airedSeason = 1, airedEpisodeNumber = 2, firstAired = "2020-01-08", overview = "" },
            },
        };
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response)));

        var result = await client.GetEpisodesAsync(12345, 1);

        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("Pilot", result[0].EpisodeName);
        Assert.Equal(1, result[0].AiredSeason);
        Assert.Equal(1, result[0].AiredEpisodeNumber);
        Assert.Equal("Second", result[1].EpisodeName);
    }

    [Fact]
    public async Task GetEpisodesAsync_HttpError_ReturnsNull()
    {
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.InternalServerError));

        var result = await client.GetEpisodesAsync(12345, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEpisodesAsync_VerifiesUrlContainsSeasonQuery()
    {
        string? capturedUrl = null;
        var response = new { data = Array.Empty<object>() };
        var client = CreateClient(req =>
        {
            capturedUrl = req.RequestUri!.ToString();
            return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response));
        });

        await client.GetEpisodesAsync(100, 3);

        Assert.NotNull(capturedUrl);
        Assert.Contains("/series/100/episodes/query", capturedUrl);
        Assert.Contains("airedSeason=3", capturedUrl);
    }
}
