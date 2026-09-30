## Context

FunkArr has two existing test servers (`FunkArrTestServer` in Api.Tests and ArrApi.Tests) that start a `WebApplication` with `TestServer`, register `TestProbe` actors via `ActorRegistry`, and expose an `HttpClient`. Integration tests use `IAsyncLifetime` to create/destroy a server per test class. This works but creates many parallel ActorSystems and has no coordination between test classes.

## Goals / Non-Goals

**Goals:**
- Single dedicated project for all HTTP-level integration tests
- Collection-per-domain with `ICollectionFixture` for controlled parallelism
- Realistic test data through reusable builder methods
- Typed response deserialization for all assertions
- Every visible API endpoint has at least one happy-path and one error-path test
- Migrate and improve existing integration tests

**Non-Goals:**
- Testing actor internals (covered by domain unit tests)
- SSE streaming endpoints (queue/stream, logs/stream)
- Real external service calls (MediathekViewWeb, Sonarr, etc.)
- UI/browser tests

## Decisions

### D1: Collection-per-domain with ICollectionFixture

Each API domain gets its own xUnit collection with its own `FunkArrFixture` instance. Tests within a collection run sequentially (safe for TestProbe state). Collections run in parallel (each has its own server + probes).

Collections: `Downloads`, `RuleSets`, `System`, `Setup`, `Mediathek`, `Newznab`, `Sabnzbd`.

```csharp
[CollectionDefinition("Downloads")]
public sealed class DownloadsCollection : ICollectionFixture<FunkArrFixture>;

[Collection("Downloads")]
public sealed class DownloadQueueTests(FunkArrFixture fixture)
{
    // Tests share fixture, run sequentially
}
```

**Alternative**: Single collection with `DisableParallelism` -- rejected because collection-per-domain enables parallelism across domains while staying safe within a domain.

### D2: FunkArrFixture based on existing FunkArrTestServer

Evolve the existing `FunkArrTestServer` pattern into `FunkArrFixture` implementing `IAsyncLifetime`. Same setup: `WebApplication.CreateBuilder()` + `UseTestServer()` + `ActorRegistry` with `TestProbe` per actor interface. Expose `HttpClient` and `GetProbe<T>()`.

The fixture lives in the IntegrationTests project, not in Tests.Shared, because it depends on the full API surface (Api + ArrApi assemblies).

### D3: TestData static builder class

A `TestData` class with static factory methods that produce realistic domain objects. Each method has required parameters for the fields tests typically assert, and sensible defaults for the rest.

```csharp
static class TestData
{
    public static QueueItem QueueItem(string title, DownloadStatus status = DownloadStatus.Queued, ...) => new(...);
    public static HistoryItem HistoryItem(string title, DownloadStatus status = DownloadStatus.Completed, ...) => new(...);
    public static MediathekItem MediathekItem(string channel, string topic, string title, ...) => new(...);
}
```

### D4: Test structure per endpoint

Each test method follows the pattern:
1. Arrange: build realistic domain response using TestData
2. Act: send HTTP request, capture the TestProbe message, reply with arranged data
3. Assert: deserialize response body to typed API model, assert fields

```csharp
[Fact]
public async Task GetQueue_WithActiveDownload_ReturnsTypedResponse()
{
    var probe = _fixture.GetProbe<IDownloadManager>();
    var task = _fixture.Client.GetAsync("/api/downloads/queue");

    probe.ExpectMsg<QueryQueue>();
    probe.Reply(new QueueResult([
        TestData.QueueItem("Tatort.S01E05.720p", DownloadStatus.Processing, progress: 50),
    ], concurrentSlots: 3, activeCount: 1, isPaused: false, isScheduleActive: true, nextWindow: null));

    var response = await task;
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var queue = await response.Content.ReadFromJsonAsync<DownloadQueueResponse>();
    Assert.NotNull(queue);
    var item = Assert.Single(queue.Items);
    Assert.Equal("Tatort.S01E05.720p", item.Title);
}
```

### D5: Migration strategy

1. Create new project with fixture and collections
2. Write new tests for all endpoints
3. Delete old integration test files from Api.Tests and ArrApi.Tests
4. Verify old test projects still build (they keep their unit tests)

### D6: Project references

```
FunkArr.IntegrationTests
  ├── FunkArr.Api          (Minimal API endpoints)
  ├── FunkArr.ArrApi       (Controller endpoints)
  ├── FunkArr.RuleSet      (RuleSetStore, RuleSetValidator for DI)
  ├── FunkArr.Tests.Shared (TestOptionsMonitor, shared helpers)
  └── Packages: xunit.v3.mtp-v2, Microsoft.AspNetCore.Mvc.Testing, 
      Microsoft.Testing.Extensions.CodeCoverage
```

## Risks / Trade-offs

- **[7 parallel servers during test run]** -- Each collection starts its own WebApplication + ActorSystem. Acceptable for CI; local runs may spike memory briefly. Mitigated by fixtures being lightweight (no real persistence, no FFmpeg).
- **[TestProbe ordering sensitivity]** -- Tests must `ExpectMsg` before `Reply` in the right order. Sequential execution within a collection prevents cross-test interference.
- **[Maintenance of TestData builders]** -- When domain message shapes change, builders need updating. Accepted -- this is the same cost as any test fixture, and centralizing it in one place is better than scattered constructors.
