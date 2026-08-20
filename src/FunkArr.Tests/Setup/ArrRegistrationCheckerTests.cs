using System.Net;
using FunkArr.Configuration;
using FunkArr.Setup;
using FunkArr.Tests.Shared;

namespace FunkArr.Tests.Setup;

public sealed class ArrRegistrationCheckerTests
{
    [Fact]
    public async Task CheckProwlarrRegisteredAsync_NameAndHostMatch_ReturnsPass()
    {
        var json = """
        [
            {
                "name": "FunkArr",
                "fields": [ { "name": "baseUrl", "value": "http://funkarr:9797" } ]
            }
        ]
        """;
        using var client = CreateClient(json);

        var result = await ArrRegistrationChecker.CheckProwlarrRegisteredAsync(
            client, "http://prowlarr:9696", "http://funkarr:9797", CancellationToken.None);

        Assert.Equal(CheckStatus.Pass, result.Status);
        Assert.Equal("prowlarr-registered", result.Name);
    }

    [Fact]
    public async Task CheckProwlarrRegisteredAsync_NameOnlyMatch_ReturnsWarning()
    {
        var json = """
        [
            {
                "name": "FunkArr",
                "fields": [ { "name": "baseUrl", "value": "http://someother:9797" } ]
            }
        ]
        """;
        using var client = CreateClient(json);

        var result = await ArrRegistrationChecker.CheckProwlarrRegisteredAsync(
            client, "http://prowlarr:9696", "http://funkarr:9797", CancellationToken.None);

        Assert.Equal(CheckStatus.Warning, result.Status);
        Assert.NotNull(result.FixGuidance);
    }

    [Fact]
    public async Task CheckProwlarrRegisteredAsync_NoSelfUrl_NameMatch_ReturnsWarning()
    {
        var json = """
        [ { "name": "FunkArr Newznab", "fields": [] } ]
        """;
        using var client = CreateClient(json);

        var result = await ArrRegistrationChecker.CheckProwlarrRegisteredAsync(
            client, "http://prowlarr:9696", null, CancellationToken.None);

        Assert.Equal(CheckStatus.Warning, result.Status);
    }

    [Fact]
    public async Task CheckProwlarrRegisteredAsync_NoMatch_ReturnsWarning()
    {
        var json = """
        [ { "name": "SomeOtherIndexer", "fields": [] } ]
        """;
        using var client = CreateClient(json);

        var result = await ArrRegistrationChecker.CheckProwlarrRegisteredAsync(
            client, "http://prowlarr:9696", "http://funkarr:9797", CancellationToken.None);

        Assert.Equal(CheckStatus.Warning, result.Status);
        Assert.NotNull(result.FixGuidance);
    }

    [Fact]
    public async Task CheckArrDownloadClientRegisteredAsync_HostPortMatch_ReturnsPass()
    {
        var json = """
        [
            {
                "name": "FunkArr",
                "fields": [ { "name": "host", "value": "funkarr" }, { "name": "port", "value": 9797 } ]
            }
        ]
        """;
        using var client = CreateClient(json);
        var instance = new ArrInstanceConnection { Name = "Sonarr", Type = ArrType.Sonarr, Url = "http://sonarr:8989", ApiKey = "key" };

        var result = await ArrRegistrationChecker.CheckArrDownloadClientRegisteredAsync(
            client, instance, "http://funkarr:9797", CancellationToken.None);

        Assert.Equal(CheckStatus.Pass, result.Status);
        Assert.Equal("Sonarr-registered", result.Name);
    }

    private static HttpClient CreateClient(string json) =>
        new(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(json)));
}
