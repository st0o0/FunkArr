namespace FunkArr.Api.Models.Setup;

internal sealed record ArrField(string Name, object Value);

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
    ArrField[] Fields)
{
    public static ProwlarrIndexerPayload Create(Uri selfUri, string apiKey)
    {
        var baseUrl = selfUri.ToString().TrimEnd('/');
        return new(
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
                new("baseUrl", $"{baseUrl}/index"),
                new("apiPath", "/api"),
                new("apiKey", apiKey),
                new("categories", new[] { 5000, 2000 })
            ]);
    }
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
    ArrField[] Fields)
{
    public static SonarrRadarrIndexerPayload Create(Uri selfUri, string apiKey, int[] categories)
    {
        var baseUrl = selfUri.ToString().TrimEnd('/');
        return new(
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
                new("baseUrl", $"{baseUrl}/index"),
                new("apiPath", "/api"),
                new("apiKey", apiKey),
                new("categories", categories)
            ]);
    }
}

internal sealed record SabnzbdDownloadClientPayload(
    string Name,
    string Implementation,
    string ImplementationName,
    string ConfigContract,
    string Protocol,
    bool Enable,
    int Priority,
    ArrField[] Fields)
{
    public static SabnzbdDownloadClientPayload Create(Uri selfUri, string apiKey, string category) => new(
        Name: "FunkArr",
        Implementation: "Sabnzbd",
        ImplementationName: "SABnzbd",
        ConfigContract: "SabnzbdSettings",
        Protocol: "usenet",
        Enable: true,
        Priority: 1,
        Fields:
        [
            new("host", selfUri.Host),
            new("port", selfUri.Port),
            new("useSsl", selfUri.Scheme == "https"),
            new("urlBase", "/download"),
            new("apiKey", apiKey),
            new("tvCategory", category),
            new("movieCategory", category)
        ]);
}
