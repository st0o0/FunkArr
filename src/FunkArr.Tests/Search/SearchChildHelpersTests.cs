using System.Text.Json;
using Akka.Event;
using FunkArr.Search;
using FunkArr.Tests.Shared;

namespace FunkArr.Tests.Search;

public sealed class SearchChildHelpersTests
{
    [Fact]
    public void BuildGenericPipelineRecord_SetsAllFields()
    {
        var record = SearchChildHelpers.BuildGenericPipelineRecord("Tatort", 12345, 1, 3, 42);

        Assert.Equal(10, record.Id.Length);
        Assert.Equal("Tatort", record.SearchTopic);
        Assert.Equal(12345, record.TvdbId);
        Assert.Equal(1, record.Season);
        Assert.Equal(3, record.Episode);
        Assert.Equal("generic-pipeline", record.Source);
        Assert.Equal(42, record.TotalResults);
        Assert.Empty(record.Matched);
        Assert.Empty(record.Filtered);
        Assert.Empty(record.Unmatched);
    }

    [Fact]
    public void BuildGenericPipelineRecord_WithNulls_SetsNullValues()
    {
        var record = SearchChildHelpers.BuildGenericPipelineRecord("test", null, null, null, 0);

        Assert.Null(record.TvdbId);
        Assert.Null(record.Season);
        Assert.Null(record.Episode);
    }

    [Fact]
    public void BuildGenericPipelineRecord_TimestampIsRecentUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var record = SearchChildHelpers.BuildGenericPipelineRecord("test", null, null, null, 0);
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(record.Timestamp, before, after);
    }

    [Fact]
    public async Task SearchMediathekAsync_Success_ReturnsResults()
    {
        var response = new
        {
            result = new
            {
                results = new[]
                {
                    new { channel = "ZDF", topic = "Heute Show", title = "Folge 1", description = "", timestamp = 0L, duration = 0, size = 0L, url_website = "", url_video = "", url_video_low = "", url_video_hd = "", url_subtitle = "" },
                },
            },
        };
        var client = CreateMediathekClient(_ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response)));

        var results = await SearchChildHelpers.SearchMediathekAsync(
            client, NoLogger.Instance, "Heute Show");

        Assert.Single(results);
        Assert.Equal("ZDF", results[0].Channel);
    }

    [Fact]
    public async Task SearchMediathekAsync_Exception_ReturnsEmpty()
    {
        var client = CreateMediathekClient(_ => throw new HttpRequestException("connection refused"));

        var results = await SearchChildHelpers.SearchMediathekAsync(
            client, NoLogger.Instance, "anything");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchMediathekAsync_BlankQuery_UsesSize100()
    {
        string? capturedBody = null;
        var response = new { result = Array.Empty<object>() };
        var client = CreateMediathekClient(req =>
        {
            capturedBody = req.Content!.ReadAsStringAsync().Result;
            return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response));
        });

        await SearchChildHelpers.SearchMediathekAsync(client, NoLogger.Instance, "  ");

        Assert.NotNull(capturedBody);
        var doc = JsonDocument.Parse(capturedBody);
        Assert.Equal(100, doc.RootElement.GetProperty("size").GetInt32());
        Assert.Empty(doc.RootElement.GetProperty("queries").EnumerateArray().ToList());
    }

    [Fact]
    public async Task SearchMediathekAsync_NonBlankQuery_UsesSize5000()
    {
        string? capturedBody = null;
        var response = new { result = Array.Empty<object>() };
        var client = CreateMediathekClient(req =>
        {
            capturedBody = req.Content!.ReadAsStringAsync().Result;
            return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(response));
        });

        await SearchChildHelpers.SearchMediathekAsync(client, NoLogger.Instance, "Tatort");

        Assert.NotNull(capturedBody);
        var doc = JsonDocument.Parse(capturedBody);
        Assert.Equal(5000, doc.RootElement.GetProperty("size").GetInt32());
    }

    private static MediathekClient CreateMediathekClient(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new HttpClient(new FakeHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://mediathekviewweb.de/"),
        }, Microsoft.Extensions.Logging.Abstractions.NullLogger<MediathekClient>.Instance);
}
