using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using FunkArr.Configuration;
using FunkArr.RuleSet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.RuleSet;

public sealed class GitHubReleaseClientTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "funkarr-test-" + Guid.NewGuid().ToString("N")[..8]);

    private string CommunityPath => Path.Combine(_tempDir, "community");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -- Version check tests -------------------------------------------------

    [Fact]
    public async Task RefreshAsync_SkipsDownload_WhenLocalVersionMatchesRemote()
    {
        Directory.CreateDirectory(CommunityPath);
        File.WriteAllText(Path.Combine(CommunityPath, "version.txt"), "1.0.0");

        var sut = CreateClient(
            releasesResponse: SingleReleaseJson("1.0.0"),
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshAsync_Downloads_WhenLocalVersionDiffersFromRemote()
    {
        Directory.CreateDirectory(CommunityPath);
        File.WriteAllText(Path.Combine(CommunityPath, "version.txt"), "0.9.0");

        var sut = CreateClient(
            releasesResponse: SingleReleaseJson("1.0.0"),
            assetResponse: CreateTestZip(),
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.True(result);
        Assert.Equal("1.0.0", File.ReadAllText(Path.Combine(CommunityPath, "version.txt")).Trim());
    }

    [Fact]
    public async Task RefreshAsync_Downloads_WhenVersionFileIsMissing()
    {
        // community dir does not exist at all
        var sut = CreateClient(
            releasesResponse: SingleReleaseJson("1.0.0"),
            assetResponse: CreateTestZip(),
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.True(result);
        Assert.True(Directory.Exists(CommunityPath));
        Assert.Equal("1.0.0", File.ReadAllText(Path.Combine(CommunityPath, "version.txt")).Trim());
    }

    // -- Atomic extraction tests ---------------------------------------------

    [Fact]
    public async Task RefreshAsync_ReplacesDirectoryAtomically()
    {
        Directory.CreateDirectory(CommunityPath);
        File.WriteAllText(Path.Combine(CommunityPath, "old-file.json"), "{}");
        File.WriteAllText(Path.Combine(CommunityPath, "version.txt"), "0.9.0");

        var sut = CreateClient(
            releasesResponse: SingleReleaseJson("1.0.0"),
            assetResponse: CreateTestZip("rule1.json", "rule2.json"),
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.True(result);
        Assert.False(File.Exists(Path.Combine(CommunityPath, "old-file.json")));
        Assert.True(File.Exists(Path.Combine(CommunityPath, "rule1.json")));
        Assert.True(File.Exists(Path.Combine(CommunityPath, "rule2.json")));
    }

    [Fact]
    public async Task RefreshAsync_PreservesOldDirectory_WhenExtractionFails()
    {
        Directory.CreateDirectory(CommunityPath);
        File.WriteAllText(Path.Combine(CommunityPath, "version.txt"), "0.9.0");
        File.WriteAllText(Path.Combine(CommunityPath, "keep-me.json"), "original");

        // Invalid ZIP bytes will cause extraction to fail
        var sut = CreateClient(
            releasesResponse: SingleReleaseJson("1.0.0"),
            assetResponse: Encoding.UTF8.GetBytes("not-a-zip"),
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        // The method catches extraction errors internally and restores
        Assert.True(Directory.Exists(CommunityPath));
        Assert.Equal("original", File.ReadAllText(Path.Combine(CommunityPath, "keep-me.json")));
    }

    // -- Error case tests ----------------------------------------------------

    [Fact]
    public async Task RefreshAsync_ReturnsFalse_WhenNoReleasesFound()
    {
        var sut = CreateClient(
            releasesResponse: "[]",
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsFalse_WhenPinnedVersionNotFound()
    {
        var options = new FunkArrOptions
        {
            RuleSetRepository = "st0o0/funkarr",
            RuleSetVersion = "2.0.0",
        };

        var sut = CreateClient(
            releasesResponse: SingleReleaseJson("1.0.0"),
            options: options);

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsFalse_WhenReleaseHasNoZipAsset()
    {
        var release = JsonSerializer.Serialize(new[]
        {
            new
            {
                tag_name = "community-rulesets-v1.0.0",
                assets = new[]
                {
                    new { name = "other-file.tar.gz", browser_download_url = "https://example.com/other.tar.gz" }
                }
            }
        });

        var sut = CreateClient(
            releasesResponse: release,
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsFalse_WhenDownloadFails()
    {
        var sut = CreateClient(
            releasesResponse: SingleReleaseJson("1.0.0"),
            assetStatusCode: HttpStatusCode.InternalServerError,
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshAsync_FindsLatestRelease_WithMatchingTagPrefix()
    {
        var releases = JsonSerializer.Serialize(new[]
        {
            new
            {
                tag_name = "other-prefix-v3.0.0",
                assets = new[] { new { name = "irrelevant.txt", browser_download_url = "https://example.com/irrelevant.txt" } }
            },
            new
            {
                tag_name = "community-rulesets-v2.0.0",
                assets = new[] { new { name = "community-rulesets.zip", browser_download_url = "https://example.com/community-rulesets.zip" } }
            }
        });

        var sut = CreateClient(
            releasesResponse: releases,
            assetResponse: CreateTestZip(),
            options: DefaultOptions());

        var result = await sut.RefreshAsync(CommunityPath);

        Assert.True(result);
        Assert.Equal("2.0.0", File.ReadAllText(Path.Combine(CommunityPath, "version.txt")).Trim());
    }

    // -- Helpers -------------------------------------------------------------

    private static FunkArrOptions DefaultOptions() => new()
    {
        RuleSetRepository = "st0o0/funkarr",
        RuleSetVersion = "latest",
    };

    private static string SingleReleaseJson(string version) =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                tag_name = $"community-rulesets-v{version}",
                assets = new[]
                {
                    new
                    {
                        name = "community-rulesets.zip",
                        browser_download_url = "https://example.com/community-rulesets.zip"
                    }
                }
            }
        });

    private static byte[] CreateTestZip(params string[] fileNames)
    {
        if (fileNames.Length == 0)
            fileNames = ["dummy.json"];

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in fileNames)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write("{}");
            }
        }

        return ms.ToArray();
    }

    private GitHubReleaseClient CreateClient(
        string releasesResponse,
        FunkArrOptions options,
        byte[]? assetResponse = null,
        HttpStatusCode assetStatusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(releasesResponse, assetResponse, assetStatusCode);

        var services = new ServiceCollection();
        services.AddHttpClient("GitHubRelease")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        return new GitHubReleaseClient(
            factory,
            Options.Create(options),
            NullLogger<GitHubReleaseClient>.Instance);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _releasesJson;
        private readonly byte[]? _assetBytes;
        private readonly HttpStatusCode _assetStatusCode;

        public MockHttpMessageHandler(
            string releasesJson,
            byte[]? assetBytes,
            HttpStatusCode assetStatusCode)
        {
            _releasesJson = releasesJson;
            _assetBytes = assetBytes;
            _assetStatusCode = assetStatusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Host == "api.github.com")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_releasesJson, Encoding.UTF8, "application/json"),
                });
            }

            // Asset download
            if (_assetStatusCode != HttpStatusCode.OK || _assetBytes is null)
            {
                return Task.FromResult(new HttpResponseMessage(_assetStatusCode));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_assetBytes),
            });
        }
    }
}
