using System.Text.Json;
using FunkArr.Configuration;

namespace FunkArr.Setup;

internal static class ArrRegistrationChecker
{
    private const string NameNeedle = "funkarr";

    public static async Task<CheckResult> CheckProwlarrRegisteredAsync(
        HttpClient client, string prowlarrUrl, string? selfUrl, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(
            $"{prowlarrUrl.TrimEnd('/')}/api/v1/indexer", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return Evaluate(
            category: "prowlarr",
            name: "prowlarr-registered",
            entries: doc.RootElement,
            selfUrl: selfUrl,
            hostFieldNames: ["baseurl", "apipath"],
            notRegisteredFixGuidance:
                "In Prowlarr, go to Settings > Indexers > Add Indexer > Newznab and add FunkArr. Use the base URL (e.g. http://funkarr:6969) without /api — Prowlarr appends the API path automatically.");
    }

    public static async Task<CheckResult> CheckArrDownloadClientRegisteredAsync(
        HttpClient client, ArrInstanceConnection instance, string? selfUrl, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(
            $"{instance.Url.TrimEnd('/')}/api/v3/downloadclient", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return Evaluate(
            category: instance.Type.ToString().ToLowerInvariant(),
            name: $"{instance.Name}-registered",
            entries: doc.RootElement,
            selfUrl: selfUrl,
            hostFieldNames: ["host", "port"],
            notRegisteredFixGuidance:
                $"In {instance.Name}, go to Settings > Download Clients > Add > SABnzbd and add FunkArr using its host, port, and API key.");
    }

    private static CheckResult Evaluate(
        string category,
        string name,
        JsonElement entries,
        string? selfUrl,
        string[] hostFieldNames,
        string notRegisteredFixGuidance)
    {
        if (entries.ValueKind != JsonValueKind.Array)
        {
            return new CheckResult(
                category, name, CheckStatus.Warning,
                "Received an unexpected response shape; could not confirm registration.",
                notRegisteredFixGuidance);
        }

        var (selfHost, selfPort) = ParseSelfUrl(selfUrl);
        var anyNameMatch = false;

        foreach (var entry in entries.EnumerateArray())
        {
            var entryName = entry.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (entryName is null || !entryName.Contains(NameNeedle, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            anyNameMatch = true;

            if (selfHost is null)
            {
                continue;
            }

            if (EntryHostMatches(entry, hostFieldNames, selfHost, selfPort))
            {
                return new CheckResult(
                    category, name, CheckStatus.Pass,
                    $"FunkArr is registered as '{entryName}' and its configured host matches this instance.",
                    null);
            }
        }

        if (anyNameMatch)
        {
            return new CheckResult(
                category, name, CheckStatus.Warning,
                "Found an entry that looks like FunkArr, but could not confirm it points at this instance.",
                notRegisteredFixGuidance);
        }

        return new CheckResult(
            category, name, CheckStatus.Warning,
            "No entry matching FunkArr was found.",
            notRegisteredFixGuidance);
    }

    private static bool EntryHostMatches(
        JsonElement entry, string[] hostFieldNames, string selfHost, int? selfPort)
    {
        if (!entry.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var field in fields.EnumerateArray())
        {
            var fieldName = field.TryGetProperty("name", out var fn) ? fn.GetString() : null;
            if (fieldName is null || !hostFieldNames.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!field.TryGetProperty("value", out var valueProp))
            {
                continue;
            }

            var value = valueProp.ValueKind switch
            {
                JsonValueKind.String => valueProp.GetString(),
                JsonValueKind.Number => valueProp.ToString(),
                _ => null,
            };

            if (value is null)
            {
                continue;
            }

            if (value.Contains(selfHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (selfPort is not null && value == selfPort.Value.ToString())
            {
                return true;
            }
        }

        return false;
    }

    private static (string? Host, int? Port) ParseSelfUrl(string? selfUrl)
    {
        if (string.IsNullOrWhiteSpace(selfUrl))
        {
            return (null, null);
        }

        var candidate = selfUrl.Contains("://", StringComparison.Ordinal) ? selfUrl : $"http://{selfUrl}";

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            ? (uri.Host, uri.IsDefaultPort ? null : uri.Port)
            : (null, null);
    }
}
