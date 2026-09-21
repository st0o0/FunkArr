namespace FunkArr.Api.Models;

internal sealed record ArrField<T>(string Name, T Value);

internal sealed record ProwlarrIndexerPayload(
    string Name,
    string Implementation,
    string ImplementationName,
    string ConfigContract,
    string Protocol,
    bool Enable,
    bool Redirect,
    int Priority,
    int AppProfileId,
    ArrField<object>[] Fields)
{
    public static ProwlarrIndexerPayload Create(string funkArrUrl, string apiKey) => new(
        Name: "FunkArr",
        Implementation: "Newznab",
        ImplementationName: "Newznab",
        ConfigContract: "NewznabSettings",
        Protocol: "usenet",
        Enable: true,
        Redirect: true,
        Priority: 25,
        AppProfileId: 1,
        Fields:
        [
            new("baseUrl", $"{funkArrUrl}/index"),
            new("apiPath", "/api"),
            new("apiKey", apiKey),
            new("categories", new[] { 5000, 2000 }),
        ]);
}

internal sealed record SonarrRadarrIndexerPayload(
    string Name,
    string Implementation,
    string ImplementationName,
    string ConfigContract,
    string Protocol,
    bool EnableRss,
    bool EnableAutomaticSearch,
    bool EnableInteractiveSearch,
    int Priority,
    ArrField<object>[] Fields)
{
    public static SonarrRadarrIndexerPayload Create(string funkArrUrl, string apiKey, int[] categories) => new(
        Name: "FunkArr",
        Implementation: "Newznab",
        ImplementationName: "Newznab",
        ConfigContract: "NewznabSettings",
        Protocol: "usenet",
        EnableRss: true,
        EnableAutomaticSearch: true,
        EnableInteractiveSearch: true,
        Priority: 25,
        Fields:
        [
            new("baseUrl", $"{funkArrUrl}/index"),
            new("apiPath", "/api"),
            new("apiKey", apiKey),
            new("categories", categories),
        ]);
}

internal sealed record SabnzbdDownloadClientPayload(
    string Name,
    string Implementation,
    string ImplementationName,
    string ConfigContract,
    string Protocol,
    bool Enable,
    int Priority,
    ArrField<object>[] Fields)
{
    public static SabnzbdDownloadClientPayload Create(string funkArrUrl, string apiKey, string category)
    {
        var uri = new Uri(funkArrUrl);
        return new(
            Name: "FunkArr",
            Implementation: "Sabnzbd",
            ImplementationName: "SABnzbd",
            ConfigContract: "SabnzbdSettings",
            Protocol: "usenet",
            Enable: true,
            Priority: 1,
            Fields:
            [
                new("host", uri.Host),
                new("port", uri.Port),
                new("urlBase", "/download"),
                new("apiKey", apiKey),
                new("tvCategory", category),
                new("movieCategory", category),
            ]);
    }
}
