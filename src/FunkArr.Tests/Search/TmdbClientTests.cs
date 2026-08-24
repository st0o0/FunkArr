using System.Text.Json;
using FunkArr.Configuration;
using FunkArr.Search;
using FunkArr.Search.Resolvers;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Search;

public sealed class TmdbClientTests
{
    private static readonly SearchOptions OptionsWithKey = new() { TmdbApiKey = "test-key" };
    private static readonly SearchOptions OptionsWithoutKey = new();

    private static TmdbClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder, SearchOptions? options = null) =>
        new(new HttpClient(new FakeHttpMessageHandler(responder)) { BaseAddress = new Uri("https://api.themoviedb.org/3/") },
            Options.Create(options ?? OptionsWithKey));

    [Fact]
    public async Task FindByImdbIdAsync_ResolvesToMovieInfo()
    {
        var findResponse = new { movie_results = new[] { new { id = 424, title = "Schindlers Liste", original_title = "Schindler's List", release_date = "1993-12-15" } } };
        var detailResponse = new { runtime = 195 };

        var client = CreateClient(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("/find/"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(findResponse));
            }

            if (path.Contains("/movie/424"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(detailResponse));
            }

            return FakeHttpMessageHandler.JsonResponse("{}");
        });

        var result = await client.FindByImdbIdAsync("tt0108052");

        Assert.NotNull(result);
        Assert.Equal("Schindlers Liste", result.Title);
        Assert.Equal("Schindler's List", result.OriginalTitle);
        Assert.Equal(1993, result.ReleaseYear);
        Assert.Equal(195, result.RuntimeMinutes);
    }

    [Fact]
    public async Task FindByImdbIdAsync_NotFound_ReturnsNull()
    {
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(new { movie_results = Array.Empty<object>() })));

        var result = await client.FindByImdbIdAsync("tt9999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByImdbIdAsync_HttpError_ReturnsNull()
    {
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.InternalServerError));

        var result = await client.FindByImdbIdAsync("tt0108052");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByImdbIdAsync_NoApiKey_ReturnsNull()
    {
        var client = CreateClient(_ => throw new InvalidOperationException("Should not be called"), OptionsWithoutKey);

        var result = await client.FindByImdbIdAsync("tt0108052");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchMovieAsync_ResolvesToMovieInfo()
    {
        var searchResponse = new { results = new[] { new { id = 601, title = "Das Leben der Anderen", original_title = "Das Leben der Anderen", release_date = "2006-03-23" } } };
        var detailResponse = new { runtime = 137 };

        var client = CreateClient(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("/search/movie"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(searchResponse));
            }

            if (path.Contains("/movie/601"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(detailResponse));
            }

            return FakeHttpMessageHandler.JsonResponse("{}");
        });

        var result = await client.SearchMovieAsync("The Lives of Others");

        Assert.NotNull(result);
        Assert.Equal("Das Leben der Anderen", result.Title);
        Assert.Equal(137, result.RuntimeMinutes);
        Assert.Equal(2006, result.ReleaseYear);
    }

    [Fact]
    public async Task SearchMovieAsync_NoResults_ReturnsNull()
    {
        var client = CreateClient(_ =>
            FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(new { results = Array.Empty<object>() })));

        var result = await client.SearchMovieAsync("xyznonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByImdbIdAsync_RuntimeFetchFails_ReturnsInfoWithNullRuntime()
    {
        var findResponse = new { movie_results = new[] { new { id = 424, title = "Schindlers Liste", original_title = "Schindler's List", release_date = "1993-12-15" } } };

        var client = CreateClient(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.Contains("/find/"))
            {
                return FakeHttpMessageHandler.JsonResponse(JsonSerializer.Serialize(findResponse));
            }

            return FakeHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.InternalServerError);
        });

        var result = await client.FindByImdbIdAsync("tt0108052");

        Assert.NotNull(result);
        Assert.Equal("Schindlers Liste", result.Title);
        Assert.Null(result.RuntimeMinutes);
    }
}
