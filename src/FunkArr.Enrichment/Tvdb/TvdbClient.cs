using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Core.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terp.Diagnostics;

namespace FunkArr.Enrichment.Tvdb;

public sealed class TvdbClient(HttpClient httpClient, IOptionsMonitor<TvdbOptions> options, IMemoryCache cache, ILogger<TvdbClient> log)
{
    private static readonly KeyValuePair<string, object?> _apiTag = new("api", "tvdb");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private string? _token;

    public bool IsConfigured => !string.IsNullOrEmpty(options.CurrentValue.ApiKey);

    public async Task<TvdbEpisode[]> GetEpisodesAsync(int seriesId)
    {
        var cacheKey = $"tvdb:episodes:{seriesId}";
        if (cache.TryGetValue(cacheKey, out TvdbEpisode[]? cached) && cached is not null)
        {
            log.LogDebug("TVDB cache hit for series {SeriesId}", seriesId);
            Telemetry.Requests.Up(_apiTag, new KeyValuePair<string, object?>("status", "hit"));
            return cached;
        }

        log.LogDebug("TVDB cache miss for series {SeriesId}, fetching", seriesId);
        Telemetry.Requests.Up(_apiTag, new KeyValuePair<string, object?>("status", "miss"));
        var sw = Stopwatch.StartNew();
        var episodes = await FetchEpisodesAsync(seriesId);
        Telemetry.Duration.Record(sw.Elapsed.TotalSeconds, _apiTag);
        cache.Set(cacheKey, episodes, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
            Size = 1
        });
        return episodes;
    }

    private async Task<TvdbEpisode[]> FetchEpisodesAsync(int seriesId)
    {
        await EnsureAuthenticated();

        var episodes = new List<TvdbEpisode>();
        var page = 0;

        while (true)
        {
            var url = QueryHelpers.AddQueryString(
                $"series/{seriesId}/episodes/default",
                "page",
                page.ToString(CultureInfo.InvariantCulture));

            var response = await SendAuthenticated(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    log.LogWarning("TVDB token expired for series {SeriesId}, re-authenticating", seriesId);
                    _token = null;
                    await EnsureAuthenticated();
                    response = await SendAuthenticated(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        log.LogWarning("TVDB request failed after re-auth: {StatusCode} for {Url}", (int)response.StatusCode, url);
                        break;
                    }
                }
                else
                {
                    log.LogWarning("TVDB request failed: {StatusCode} for {Url}", (int)response.StatusCode, url);
                    break;
                }
            }

            var result = await response.Content.ReadFromJsonAsync<TvdbSeriesEpisodesResponse>(_jsonOptions);
            if (result?.Data?.Episodes is null)
            {
                break;
            }

            episodes.AddRange(result.Data.Episodes);

            if (string.IsNullOrEmpty(result.Links?.Next))
            {
                break;
            }

            page++;
        }

        return [.. episodes];
    }

    private async Task EnsureAuthenticated()
    {
        if (_token is not null)
        {
            return;
        }

        var apiKey = options.CurrentValue.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("TVDB API key is not configured");
        }

        var loginBody = new { apikey = apiKey, pin = "" };
        var loginResponse = await httpClient.PostAsJsonAsync("login", loginBody, _jsonOptions);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<TvdbLoginResponse>(_jsonOptions);
        _token = loginResult?.Data?.Token
            ?? throw new InvalidOperationException("TVDB login did not return a token");

        log.LogDebug("TVDB authentication successful");
    }

    private async Task<HttpResponseMessage> SendAuthenticated(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return await httpClient.SendAsync(request);
    }
}

public sealed record TvdbEpisode(
    [property: JsonPropertyName("seasonNumber")] int SeasonNumber,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("aired")] string? Aired,
    [property: JsonPropertyName("runtime")] int? Runtime);

file sealed class TvdbLoginResponse
{
    public TvdbLoginData? Data { get; set; }
}

file sealed class TvdbLoginData
{
    public string? Token { get; set; }
}

file sealed class TvdbSeriesEpisodesResponse
{
    public TvdbSeriesEpisodesData? Data { get; set; }
    public TvdbLinks? Links { get; set; }
}

file sealed class TvdbSeriesEpisodesData
{
    public TvdbEpisode[]? Episodes { get; set; }
}

file sealed class TvdbLinks
{
    public string? Next { get; set; }
}
