---
description: "Write actor tests using TestKit with TestProbe registration, DataPaths setup, and temp directory lifecycle"
---

# FunkArr Actor Test Pattern

## Test class structure

All test classes extend `TestKit` from `Akka.TestKit.Xunit`. Two test styles exist — pick the right one for what you're testing.

### Actor integration tests

Test actor message handling, inter-actor communication, and side effects.

```csharp
using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Tests.Shared;

namespace FunkArr.<Domain>.Tests;

public sealed class <Name>Tests : TestKit
{
    // Only when the actor needs filesystem access:
    private readonly string _tempDir;
    private readonly TestDataFiles _dataFiles;
    private readonly DataPaths _dataPaths;

    public <Name>Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var funkArrOptions = new FunkArrOptions { DataPath = _tempDir };
        var downloadOptions = new DownloadOptions();
        _dataPaths = new DataPaths(funkArrOptions, downloadOptions);
        _dataPaths.EnsureDirectories();
        _dataFiles = new TestDataFiles(
            new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance));
    }

    [Fact]
    public void Test_case_name()
    {
        // 1. Create probes and register
        // 2. Create actor
        // 3. Send message, assert
        Cleanup();
    }

    private void Cleanup()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* ignored */ }
    }
}
```

### State-only unit tests

Test state extension methods (Apply, TryGet*, ToXxx) without actors. No TestKit boilerplate needed — plain xUnit class.

```csharp
namespace FunkArr.<Domain>.Tests;

public sealed class <Name>StateTests
{
    private static readonly IActorRef _noSender = ActorRefs.Nobody;

    [Fact]
    public void Apply_event_updates_state()
    {
        var state = new <Name>State();
        state.Init(command, _noSender);
        state.Apply(event);
        Assert.Equal(expected, state.SomeField);
    }

    // Helper factory methods at bottom
    private static <Name>State InitState(...) { ... }
    private static SomeItem MakeItem(...) => new(...);
}
```

## TestProbe registration

Every actor dependency gets a `TestProbe` registered via the `ActorRegistry`:

```csharp
var mediathekProbe = CreateTestProbe();
var scoringProbe = CreateTestProbe();
var resolverProbe = CreateTestProbe();

var registry = ActorRegistry.For(Sys);
registry.Register<IMediathekManager>(mediathekProbe);
registry.Register<IScoringManager>(scoringProbe);
registry.Register<IRuleSetResolver>(resolverProbe);
```

When many probes are needed, use a record + helper:

```csharp
private record TestProbes(
    Akka.TestKit.TestProbe Mediathek,
    Akka.TestKit.TestProbe Scoring,
    Akka.TestKit.TestProbe Resolver);

private TestProbes RegisterProbes()
{
    var probes = new TestProbes(
        CreateTestProbe(), CreateTestProbe(), CreateTestProbe());

    var registry = ActorRegistry.For(Sys);
    registry.Register<IMediathekManager>(probes.Mediathek);
    registry.Register<IScoringManager>(probes.Scoring);
    registry.Register<IRuleSetResolver>(probes.Resolver);

    return probes;
}
```

Register **all** marker interfaces the actor resolves via `Context.GetActor<T>()`, even if the test doesn't exercise that path — the actor will fail on startup otherwise.

## Actor creation

Always `Props.Create` with explicit constructor:

```csharp
var actor = Sys.ActorOf(Props.Create(() => new ScoringManager(ScoringOpts())));
```

For actors without DI deps:

```csharp
var actor = Sys.ActorOf(Props.Create<ScoringActor>());
```

Never use `resolver.Props<T>()` in tests — that's for production DI only.

## IOptionsMonitor in tests

Use `TestOptionsMonitor<T>` from shared infrastructure:

```csharp
private static IOptionsMonitor<ScoringOptions> ScoringOpts(int poolSize = 1) =>
    new TestOptionsMonitor<ScoringOptions>(new ScoringOptions { PoolSize = poolSize });
```

## Assertion patterns

**Expect message on TestKit** (when actor replies to sender = TestActor):

```csharp
actor.Tell(new QuerySomething("id"));
var result = ExpectMsg<SomethingResult>();
Assert.Equal("id", result.Id);
```

**Expect message on probe** (when actor sends to a dependency):

```csharp
var query = probe.ExpectMsg<QueryMediathek>();
Assert.Equal("Tatort", query.Fields[0].Query);
```

**Reply from probe** (simulate dependency response):

```csharp
probe.ExpectMsg<QueryMediathek>();
probe.Reply(new QueryMediathekCompleted(items, count));
```

**Expect no message** (verify actor does NOT send):

```csharp
probe.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
```

**Wait for async condition** (e.g. file watcher setup):

```csharp
AwaitCondition(() => _dataFiles.Watchers.Count >= 1);
```

## Multi-step interaction tests

For actors with conversation flows (ask dependency → process → ask next):

```csharp
[Fact]
public void Full_search_flow()
{
    var p = RegisterProbes();
    var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

    // 1. Send initial command
    worker.Tell(new SearchSeries(...), TestActor);

    // 2. First dependency interaction
    p.Mediathek.ExpectMsg<QueryMediathek>();
    p.Mediathek.Reply(new QueryMediathekCompleted(items, 1));

    // 3. Next dependency
    p.Resolver.ExpectMsg<ResolveRuleSet>();
    p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

    // 4. Final result back to caller
    var result = ExpectMsg<SearchSeriesCompleted>();
    Assert.Equal(expected, result.Items.Length);
}
```

## FileSystemWatcher testing

Use `TestDataFiles` which wraps `IDataFiles` and captures watchers:

```csharp
// Wait for watcher to be registered
AwaitCondition(() => _dataFiles.Watchers.Count >= 1);

// Simulate file events
var filePath = Path.Combine(_dataPaths.CommunityRuleSets, "new-show.json");
File.WriteAllText(filePath, json);
_dataFiles.Watchers[0].SimulateCreated(filePath);
// Also: SimulateDeleted, SimulateChanged
```

## Running tests

xUnit v3 on Microsoft.Testing.Platform — use `dotnet run`, **not** `dotnet test`:

```powershell
dotnet run --project src/FunkArr.<Domain>.Tests/FunkArr.<Domain>.Tests.csproj
```
