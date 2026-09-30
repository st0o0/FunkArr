## Approach

Custom `FunkArrTestServer` that builds a minimal `WebApplication` with only the services the API layer needs. No Servus AppBuilder, no Akka Clustering/Remoting/Persistence. A bare `ActorSystem.Create("test")` provides TestProbes via `ActorRegistry`.

## FunkArrTestServer

Lives in `FunkArr.Tests.Shared`. Encapsulates:

1. **WebApplication** built from scratch with:
   - `FunkArrOptions`, `DownloadOptions`, `RoutingOptions`, `ArrApiOptions`, `ScoringOptions`, `ScoringHistoryOptions`, `RuleSetUpdaterOptions` bound to test defaults
   - `DataPaths` + `IDataFiles` pointing to a temp directory
   - `IHttpClientFactory` (default registration)
   - `RuleSetStore`, `IRuleSetValidator`
   - `AddControllers().AddApplicationPart(ArrApi assembly)`
   - `AddOutputCache()` (endpoints use `IOutputCacheStore`)
   - ArrApi services via `AddArrApiServices()`

2. **Minimal ActorSystem** (no remoting, no clustering):
   - `ActorSystem.Create("test")` with default config
   - `ActorRegistry` with TestProbes registered for each actor key
   - Registry added to DI as `IActorRegistry`

3. **Endpoint registration**:
   - `app.MapSystemApi()`, `app.MapDownloadsApi()`, `app.MapRuleSetApi()`, `app.MapMediathekApi()`, `app.MapSetupArrApi()`
   - `app.MapControllers()` (for ArrApi)
   - `app.MapHealthChecks("/healthz")`

4. **Test API**:
   - `HttpClient Client` -- pre-configured with base address
   - `TestProbe GetProbe<TKey>()` -- access the TestProbe for an actor key
   - `IDisposable` / `IAsyncDisposable` -- shuts down ActorSystem + WebApplication

## Test Pattern

```csharp
public class DownloadQueueIntegrationTests : IAsyncLifetime
{
    private FunkArrTestServer _server = null!;

    public async Task InitializeAsync() => _server = await FunkArrTestServer.CreateAsync();
    public async Task DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task GetQueue_returns_empty_list()
    {
        var probe = _server.GetProbe<IDownloadManager>();

        var task = _server.Client.GetAsync("/api/downloads/queue");

        probe.ExpectMsg<QueryQueue>();
        probe.Reply(new QueueResult([]));

        var response = await task;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

The `Ask` pattern means the HTTP request blocks until the TestProbe replies. So the test fires the HTTP request, then synchronously handles the probe expectation, then awaits the response.

## ArrApi Tests

ArrApi controllers use `ApiKeyActionFilter`. Tests provide a valid API key via test config (`FunkArrOptions.ApiKey = "test-key"`) and pass it as query parameter. Separate tests verify rejection without key.

## Scope

Start with a focused set of tests covering the main patterns:

- **Downloads**: GET queue (empty), GET history (empty), POST pause/resume
- **System**: GET version, GET storage
- **ArrApi**: Newznab caps, SABnzbd version mode, API key rejection

This validates the test server works and covers the core HTTP patterns. More endpoints can be added incrementally.
