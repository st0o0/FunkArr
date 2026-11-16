using System.IO.Abstractions;
using System.Net;
using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Core.Configuration;
using FunkArr.Core.Data;
using FunkArr.RuleSet.Updater;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet.Tests.Updater;

public sealed class RuleSetUpdaterDegradedTests : TestKit, IDisposable
{
    private readonly string _tempDir;
    private readonly DataPaths _dataPaths;
    private readonly TestDataFiles _dataFiles;

    public RuleSetUpdaterDegradedTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "community"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "local"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "temp"));

        _dataPaths = new DataPaths(
            Options.Create(new FunkArrOptions { DataPath = _tempDir }),
            Options.Create(new DownloadOptions()));
        _dataPaths.EnsureDirectories();
        _dataFiles = new TestDataFiles(new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance));
    }

    private IActorRef CreateUpdater(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler(statusCode);
        var factory = new StubHttpClientFactory(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        });

        return Sys.ActorOf(Props.Create(() => new RuleSetUpdater(
            factory,
            _dataFiles,
            _dataPaths,
            new OptionsMonitorStub<RuleSetUpdaterOptions>(new RuleSetUpdaterOptions
            {
                RefreshEnabled = true,
                Repository = "st0o0/funkarr",
                Version = "latest"
            }))));
    }

    private IActorRef CreateUpdaterThrowing()
    {
        var handler = new ThrowingHttpMessageHandler();
        var factory = new StubHttpClientFactory(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        });

        return Sys.ActorOf(Props.Create(() => new RuleSetUpdater(
            factory,
            _dataFiles,
            _dataPaths,
            new OptionsMonitorStub<RuleSetUpdaterOptions>(new RuleSetUpdaterOptions
            {
                RefreshEnabled = true,
                Repository = "st0o0/funkarr",
                Version = "latest"
            }))));
    }

    [Fact]
    public void Survives_github_500_error()
    {
        var updater = CreateUpdater(HttpStatusCode.InternalServerError);

        Watch(updater);
        ExpectNoMsg(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Survives_github_403_rate_limit()
    {
        var updater = CreateUpdater(HttpStatusCode.Forbidden);

        Watch(updater);
        ExpectNoMsg(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Survives_github_timeout()
    {
        var updater = CreateUpdaterThrowing();

        Watch(updater);
        ExpectNoMsg(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Disabled_refresh_does_not_crash()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK);
        var factory = new StubHttpClientFactory(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        });

        var updater = Sys.ActorOf(Props.Create(() => new RuleSetUpdater(
            factory,
            _dataFiles,
            _dataPaths,
            new OptionsMonitorStub<RuleSetUpdaterOptions>(new RuleSetUpdaterOptions
            {
                RefreshEnabled = false
            }))));

        Watch(updater);
        ExpectNoMsg(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
    }

    public new void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_tempDir, true); }
        catch { /* cleanup best effort */ }
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException("Request timed out");
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class OptionsMonitorStub<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
