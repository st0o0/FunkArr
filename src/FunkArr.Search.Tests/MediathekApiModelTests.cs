using System.Reflection;
using System.Text.Json;

namespace FunkArr.Search.Tests;

public sealed class MediathekApiModelTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Theory]
    [InlineData("1761800000", 1761800000L)]
    [InlineData("\"1761800000\"", 1761800000L)]
    [InlineData("\"\"", null)]
    [InlineData("null", null)]
    [InlineData("\"abc\"", null)]
    public void FilmlisteTimestamp_is_read_leniently(string rawValue, long? expected)
    {
        var json = $$"""
            {"result":{"results":[{"channel":"ARD","topic":"Tatort","title":"T","timestamp":1,"duration":60,
            "filmlisteTimestamp":{{rawValue}}}],"queryInfo":{"totalResults":1,"filmlisteTimestamp":{{rawValue}}}},"err":null}
            """;

        var response = JsonSerializer.Deserialize<MediathekApiResponse>(json, _jsonOptions);

        Assert.NotNull(response?.Result?.Results);
        Assert.NotNull(response.Result.QueryInfo);
        var item = Assert.Single(response.Result.Results);
        Assert.Multiple(
            () => Assert.Equal(expected, item.FilmlisteTimestamp),
            () => Assert.Equal(expected, response.Result.QueryInfo.FilmlisteTimestamp));
    }

    [Fact]
    public void FilmlisteTimestamp_missing_is_null()
    {
        const string json = """
            {"result":{"results":[{"channel":"ARD","topic":"Tatort","title":"T","timestamp":1,"duration":60}],
            "queryInfo":{"totalResults":1}},"err":null}
            """;

        var response = JsonSerializer.Deserialize<MediathekApiResponse>(json, _jsonOptions);

        Assert.NotNull(response?.Result?.Results);
        Assert.NotNull(response.Result.QueryInfo);
        Assert.Multiple(
            () => Assert.Null(Assert.Single(response.Result.Results).FilmlisteTimestamp),
            () => Assert.Null(response.Result.QueryInfo.FilmlisteTimestamp));
    }

    [Fact]
    public void Full_response_deserializes_all_fields()
    {
        var json = LoadResource("mvw-full-response.json");
        var response = JsonSerializer.Deserialize<MediathekApiResponse>(json, _jsonOptions)!;

        Assert.Null(response.Err);
        Assert.NotNull(response.Result);
        Assert.NotNull(response.Result.QueryInfo);
        Assert.Equal(42, response.Result.QueryInfo.TotalResults);
        Assert.NotNull(response.Result.Results);
        Assert.Equal(2, response.Result.Results.Length);

        var first = response.Result.Results[0];
        Assert.Equal("ARD", first.Channel);
        Assert.Equal("Tatort", first.Topic);
        Assert.Equal("Tatort: Der letzte Schrey", first.Title);
        Assert.Equal("Ein Münchner Kommissar ermittelt in einem mysteriösen Mordfall.", first.Description);
        Assert.Equal(1719244800, first.Timestamp);
        Assert.Equal(5400, first.Duration);
        Assert.Equal(1200000000, first.Size);
        Assert.Equal("https://example.com/tatort-sd.mp4", first.UrlVideo);
        Assert.Equal("https://example.com/tatort-low.mp4", first.UrlVideoLow);
        Assert.Equal("https://example.com/tatort-hd.mp4", first.UrlVideoHd);
        Assert.Equal("https://example.com/tatort.srt", first.UrlSubtitle);
        Assert.Equal("https://www.ardmediathek.de/tatort", first.UrlWebsite);
    }

    [Fact]
    public void Full_response_partial_fields_are_null()
    {
        var json = LoadResource("mvw-full-response.json");
        var response = JsonSerializer.Deserialize<MediathekApiResponse>(json, _jsonOptions)!;

        Assert.NotNull(response.Result);
        Assert.NotNull(response.Result.Results);
        Assert.Equal(2, response.Result.Results.Length);
        var second = response.Result.Results[1];
        Assert.Equal("Das Erste", second.Channel);
        Assert.Null(second.Description);
        Assert.Null(second.UrlVideoLow);
        Assert.Equal("https://example.com/tatort2-hd.mp4", second.UrlVideoHd);
        Assert.Null(second.UrlSubtitle);
        Assert.Null(second.UrlWebsite);
    }

    [Fact]
    public void Empty_results_deserialize()
    {
        var json = LoadResource("mvw-empty-response.json");
        var response = JsonSerializer.Deserialize<MediathekApiResponse>(json, _jsonOptions)!;

        Assert.Null(response.Err);
        Assert.NotNull(response.Result);
        Assert.NotNull(response.Result.Results);
        Assert.Empty(response.Result.Results);
        Assert.NotNull(response.Result.QueryInfo);
        Assert.Equal(0, response.Result.QueryInfo.TotalResults);
    }

    [Fact]
    public void Error_response_deserializes()
    {
        var json = LoadResource("mvw-error-response.json");
        var response = JsonSerializer.Deserialize<MediathekApiResponse>(json, _jsonOptions)!;

        Assert.Equal("search query too short", response.Err);
        Assert.Null(response.Result);
    }

    private static string LoadResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"FunkArr.Search.Tests.Resources.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
