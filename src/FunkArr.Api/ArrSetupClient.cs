using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Api.Models;

namespace FunkArr.Api;

public sealed class ArrSetupClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ArrResourceResponse> PostResourceAsync<T>(
        ArrConnection connection, string apiPath, T payload)
    {
        if (!Uri.TryCreate(connection.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
        {
            return new ArrResourceResponse(false, Error: "Invalid URL -- must start with http:// or https://");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, apiPath));
            request.Headers.Add("X-Api-Key", connection.ApiKey);
            request.Content = JsonContent.Create(payload, options: _jsonOptions);

            using var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return ParseSuccessResponse(body);
            }

            return ParseErrorResponse(body, response);
        }
        catch (TaskCanceledException)
        {
            return new ArrResourceResponse(false, Error: "Connection timed out");
        }
        catch (HttpRequestException ex)
        {
            return new ArrResourceResponse(false, Error: $"Connection failed: {ex.Message}");
        }
    }

    private static ArrResourceResponse ParseSuccessResponse(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            int? id = root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number
                ? idProp.GetInt32()
                : null;

            var name = root.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString()
                : null;

            ArrProviderMessage? message = null;
            if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.Object)
            {
                var msgText = msgProp.TryGetProperty("message", out var mt) ? mt.GetString() ?? "" : "";
                var msgType = msgProp.TryGetProperty("type", out var mty) ? mty.GetString() ?? "info" : "info";
                message = new ArrProviderMessage(msgText, msgType);
            }

            return new ArrResourceResponse(true, Id: id, Name: name, Message: message);
        }
        catch (JsonException)
        {
            return new ArrResourceResponse(true);
        }
    }

    private static ArrResourceResponse ParseErrorResponse(string body, HttpResponseMessage response)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var errors = new List<ArrValidationError>();
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var propName = item.TryGetProperty("propertyName", out var pn) ? pn.GetString() ?? "" : "";
                    var errMsg = item.TryGetProperty("errorMessage", out var em) ? em.GetString() ?? "" : "";
                    var isWarning = item.TryGetProperty("isWarning", out var iw) && iw.ValueKind == JsonValueKind.True;
                    errors.Add(new ArrValidationError(propName, errMsg, isWarning));
                }

                if (errors.Count > 0)
                {
                    var summary = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return new ArrResourceResponse(false, Error: summary, ValidationErrors: errors);
                }
            }

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("message", out var msg))
            {
                return new ArrResourceResponse(false, Error: msg.GetString());
            }
        }
        catch (JsonException)
        {
            // noop
        }

        return new ArrResourceResponse(false,
            Error: $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
    }
}

public sealed record ArrConnection(string BaseUrl, string ApiKey);
