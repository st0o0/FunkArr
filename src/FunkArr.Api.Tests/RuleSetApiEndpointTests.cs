using System.IO.Abstractions;
using System.Net;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Tests;

public sealed class RuleSetApiEndpointTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private ActorSystem _actorSystem = null!;
    private FakeDataFiles _dataFiles = null!;

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        _dataFiles = new FakeDataFiles();
        var funkArrOptions = Options.Create(new FunkArrOptions { DataPath = Path.GetTempPath() });
        var downloadOptions = Options.Create(new DownloadOptions { Path = Path.GetTempPath() });

        // Minimal API's endpoint metadata is built for the whole group, so every
        // route in MapRuleSetApi needs its DI-resolvable parameter types registered,
        // even routes the tests below never call.
        _actorSystem = ActorSystem.Create("rulesetapi-endpoint-tests");
        var registry = ActorRegistry.For(_actorSystem);

        builder.Services.AddLogging();
        builder.Services.AddOutputCache();
        builder.Services.AddSingleton<IActorRegistry>(registry);
        builder.Services.AddSingleton<IDataFiles>(_dataFiles);
        builder.Services.AddSingleton(new DataPaths(funkArrOptions, downloadOptions));

        _app = builder.Build();
        _app.MapRuleSetApi();

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _actorSystem.Terminate();
    }

    [Fact]
    public async Task Get_raw_returns_404_for_unknown_ruleset()
    {
        var response = await Client.GetAsync("/api/rulesets/missing-id/raw");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_raw_returns_stored_json_for_existing_local_ruleset()
    {
        var dataPaths = _app.Services.GetRequiredService<DataPaths>();
        var path = Path.Join(dataPaths.LocalRuleSets, "ard-tagesschau.json");
        _dataFiles.Add(path, "{\"ruleSetId\":\"ard-tagesschau\"}");

        var response = await Client.GetAsync("/api/rulesets/ard-tagesschau/raw");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"ruleSetId\":\"ard-tagesschau\"}", body);
    }

    private sealed class FakeDataFiles : IDataFiles
    {
        private readonly Dictionary<string, string> _files = [];

        public void Add(string path, string content) => _files[path] = content;

        public bool Exists(string path) => _files.ContainsKey(path);

        public string ReadText(string path) => _files[path];

        public void CreateDirectory(string path)
        {
        }

        public void Remove(string path)
        {
        }

        public void Move(string source, string destination)
        {
        }

        public void ReplaceDirectory(string source, string target)
        {
        }

        public void WriteText(string path, string content)
        {
        }

        public void WriteAtomic(string path, string content)
        {
        }

        public string[] ListFiles(string directory, string pattern) => [];

        public bool CanWrite(string directory) => true;

        public IFileSystemWatcher Watch(string directory, string filter) => throw new NotSupportedException();
    }
}
