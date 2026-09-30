## Why

Actor tests use 46 `Task.Delay` calls (100–2000ms each) as blind synchronization, making them non-deterministic under CI load and adding ~5+ seconds of wasted wall-clock time. Additionally, test file naming is inconsistent with actor naming conventions, and persistence setup is copy-pasted HOCON instead of using the shared `TestPersistenceConfig` helper.

## What Changes

- Remove all 46 `Task.Delay` calls across 6 actor test files, replacing them with deterministic Akka TestKit synchronization patterns (mailbox ordering, `AwaitAssertAsync`, `ExpectMsg` on probes)
- Rename `DownloadCoordinatorTests.cs` → `DownloadActorTests.cs` and `DownloadRequestTrackerTests.cs` → `DownloadRequestActorTests.cs` to match actor naming
- Replace inline HOCON persistence config with `builder.AddTestPersistence()` in all actor test files

## Capabilities

### New Capabilities

_None — this is a test-only refactor._

### Modified Capabilities

_None — no requirement or spec-level behavior changes._

## Impact

- **Test files modified**: `MovieActorTests.cs`, `ShowActorTests.cs`, `DownloadCoordinatorTests.cs` (renamed), `DownloadRequestTrackerTests.cs` (renamed), `RecentMatchActorTests.cs`, `RuleSetRegistryActorTests.cs`
- **Shared infra**: `TestPersistenceConfig` already exists, no changes needed
- **Production code**: No changes
- **APIs**: No changes
- **Risk**: Low — tests may initially fail if in-memory journal timing assumptions are wrong, but `AwaitAssertAsync` provides safe fallback with retry semantics
