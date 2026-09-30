## Context

The test suite contains 6 actor test files that use `Task.Delay` for synchronization. All actor tests inherit from `Akka.Hosting.TestKit.TestKit` and use in-memory persistence (journal + snapshot store). The Akka.NET TestKit provides deterministic synchronization primitives (`ExpectMsg`, `AwaitAssertAsync`, `AwaitConditionAsync`) that these tests don't leverage.

Current actor tests also copy-paste HOCON persistence configuration instead of using the existing `TestPersistenceConfig.AddTestPersistence()` extension method in `FunkArr.Tests.Shared`.

## Goals / Non-Goals

**Goals:**
- Eliminate all 46 `Task.Delay` calls from actor tests
- Make all actor tests deterministic (no timing-dependent behavior)
- Unify persistence setup across all actor test files
- Align test file names with actor naming conventions

**Non-Goals:**
- Changing production actor code
- Adding new test coverage
- Restructuring test folder hierarchy (Unit/ vs Actors/ split)
- Introducing new test base classes or abstractions

## Decisions

### 1. Delay removal strategy is pattern-dependent

Four distinct patterns exist, each with its own deterministic replacement:

**Pattern A — Synchronous Tell → Ask (30 occurrences)**
Files: `MovieActorTests`, `ShowActorTests`, `DownloadRequestActorTests`, `RecentMatchActorTests`

`ApplyCommunityRules` handlers update in-memory state without calling `Persist()`. For persistent commands (`TrackDownload`, `ReportProgress`, etc.), the in-memory journal completes synchronously. In both cases, Akka's mailbox ordering guarantee means the subsequent `Ask` always sees the updated state.

**Action**: Remove `Task.Delay` entirely. The `Ask` call is the barrier.

**Alternative considered**: Adding explicit `Ack` responses to Tell-based commands. Rejected because it requires production code changes for no functional benefit — mailbox ordering already provides the guarantee with in-memory persistence.

**Pattern B — Bulk Tell → Query (3 occurrences)**
File: `RecentMatchActorTests` (eviction test with 105 messages, limit test with 10 messages)

Multiple `Tell` calls followed by a single `Ask`. Each `Tell` triggers `Persist()`. While individual persistence is synchronous, 105 sequential persist-and-apply cycles may not complete before the `Ask` is dequeued.

**Action**: Replace `Task.Delay` with `AwaitAssertAsync`:
```csharp
await AwaitAssertAsync(async () =>
{
    var response = await actor.Ask<RecentResponse>(new GetRecent(200), TimeSpan.FromSeconds(3));
    Assert.Equal(100, response.Records.Count);
}, TimeSpan.FromSeconds(5));
```

This polls at 100ms intervals and succeeds as soon as the condition is met — typically in <500ms instead of the current fixed 2000ms.

**Alternative considered**: Sending all messages then using a single `Ask` as barrier (Pattern A). Rejected because 105 `Persist` calls with stashing makes ordering guarantees less clear — `AwaitAssertAsync` is safer and self-documenting.

**Pattern C — Actor init wait (6 occurrences)**
Files: `RecentMatchActorTests`, `RuleSetRegistryActorTests`

`Task.Delay` after actor creation to wait for `RecoveryCompleted` and `Become(Ready)`. With an empty in-memory journal, recovery is instant — no events to replay.

**Action**: Remove `Task.Delay` entirely. The first `Ask` or `ExpectMsg` after creation serves as the init barrier (the actor can't process queries until recovery completes and `Become(Ready)` is called).

For `RuleSetRegistryActorTests.SaveLocal`/`RemoveLocal`: the init includes `DiffAndLoadFromDisk()` and `PushAllToMediaActors()`, both synchronous. The tests that start with no community files have nothing to push. The `Ask<SaveLocalResult>` blocks until the actor is ready.

**Pattern D — State machine with side-effects (11 occurrences)**
File: `DownloadActorTests` (currently `DownloadCoordinatorTests`)

The `DownloadActor` progresses through states (WaitingForJob → Fetching → AcquiringSubtitle → Muxing → Completed). Each transition persists an event and spawns a child actor. The test manually sends stage-completion messages (`VideoFetched`, `VideoRemuxed`, etc.) but needs to wait for the actor to reach the target state first.

The actor calls `NotifyTracker(status)` on each transition, which sends `DownloadRequestActor.ReportProgress` to the tracker shard — already registered as a `TestProbe` in the test setup.

**Action**: Use `_trackerProbe.ExpectMsg<DownloadRequestActor.ReportProgress>()` as deterministic state-transition barrier. After `Tell(StartDownload)`, wait for the probe to receive `ReportProgress("Downloading")` before sending `VideoFetched`.

For the `QueueProbe`: the actor also sends `QueueActor.NotifyJobFinished` on completion/failure/cancel, providing a second synchronization point for terminal state verification.

### 2. Use `AddTestPersistence()` everywhere

All 5 actor test files that configure persistence inline will be updated to use:
```csharp
builder.AddTestPersistence();
```

This is functionally equivalent but eliminates duplication and ensures consistency if the shared config evolves.

### 3. Rename files to match actor names

| Current name | New name | Reason |
|---|---|---|
| `DownloadCoordinatorTests.cs` | `DownloadActorTests.cs` | Actor is `DownloadActor` |
| `DownloadRequestTrackerTests.cs` | `DownloadRequestActorTests.cs` | Actor is `DownloadRequestActor` |

## Risks / Trade-offs

**[Mailbox ordering assumption may not hold for all patterns]** → Mitigated by using `AwaitAssertAsync` for bulk-Tell patterns where ordering is less certain. For single Tell→Ask, the guarantee is well-documented in Akka.NET and verified by the in-memory journal's synchronous behavior.

**[Tests may become slower if AwaitAssertAsync polls multiple times]** → Unlikely. In-memory persistence is fast enough that the first poll attempt should succeed. Worst case, 100ms polling overhead vs. current 500-2000ms fixed delay.

**[DownloadActor probe-based sync depends on NotifyTracker being called]** → This is a stable part of the actor's design (tracker notification on every state transition). If it changes, the tests should break — that's a feature, not a risk.
