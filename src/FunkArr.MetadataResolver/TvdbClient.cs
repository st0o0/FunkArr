using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.MetadataResolver;

public sealed class TvdbClient(HttpClient httpClient, IOptionsMonitor<TvdbOptions> options, ILogger<TvdbClient> log)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private string? _token;

    public bool IsConfigured => !string.IsNullOrEmpty(options.CurrentValue.ApiKey);

    public async Task<TvdbEpisode[]> GetEpisodesAsync(int seriesId, int? seasonFilter)
    {
        await EnsureAuthenticated();

        var episodes = new List<TvdbEpisode>();
        var page = 0;

        while (true)
        {
            var url = $"series/{seriesId}/episodes/default?page={page}";

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

            foreach (var ep in result.Data.Episodes)
            {
                if (seasonFilter is null || ep.SeasonNumber == seasonFilter)
                {
                    episodes.Add(ep);
                }
            }

            if (string.IsNullOrEmpty(result.Links?.Next))
            {
                break;
            }

            page++;
        }

        return episodes.ToArray();
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
