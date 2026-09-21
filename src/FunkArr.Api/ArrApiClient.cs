using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Api.Models;

namespace FunkArr.Api;

public sealed class ArrApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<CreateArrResourceResponse> PostResourceAsync<T>(
        ArrConnection connection, string apiPath, T payload)
    {
        if (!Uri.TryCreate(connection.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
        {
            return new CreateArrResourceResponse(false, "Invalid URL — must start with http:// or https://");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, apiPath));
            request.Headers.Add("X-Api-Key", connection.ApiKey);
            request.Content = JsonContent.Create(payload, options: _jsonOptions);

            using var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return new CreateArrResourceResponse(true);
            }

            var body = await response.Content.ReadAsStringAsync();
            var errorMessage = TryExtractErrorMessage(body) ??
                               $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";

            return new CreateArrResourceResponse(false, errorMessage);
        }
        catch (TaskCanceledException)
        {
            return new CreateArrResourceResponse(false, "Connection timed out");
        }
        catch (HttpRequestException ex)
        {
            return new CreateArrResourceResponse(false, $"Connection failed: {ex.Message}");
        }
    }

    private static string? TryExtractErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("message", out var msg))
            {
                return msg.GetString();
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                var errors = new List<string>();
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("errorMessage", out var errMsg))
                    {
                        errors.Add(errMsg.GetString() ?? "");
                    }
                }

                if (errors.Count > 0)
                {
                    return string.Join("; ", errors);
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}

public sealed record ArrConnection(string BaseUrl, string ApiKey);
