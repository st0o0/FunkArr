## Why

Actor states are inconsistently implemented: 12 use immutable records with `with`-expressions, 5 use mutable classes with void `Apply` methods. The record approach provides no real benefit in Akka (single-threaded mailbox already guarantees thread-safety) and causes problems: `FeedCrawlWorkerState` is a record that mutates `List<T>` fields in-place (shared-reference bug), `RuleSetManagerState` has no Apply methods (mutated inline with `with{}`), and several actors keep mutable domain state as actor fields instead of in the state object. Unifying on mutable classes gives honest semantics, zero-alloc state updates, and a single consistent pattern.

## What Changes

- **Convert all 12 record-based actor states to sealed classes** with mutable properties, void `Apply` methods, and a `public static Empty` factory returning a new instance.
- **Normalize Apply naming**: rename all `ApplyXxx`/`StartXxx`/`FireXxx` prefixed methods to overloaded `Apply(EventType)`. Remove `Init` methods where they exist (fold into constructor or `Apply`).
- **Move leaked state from actors into state objects**:
  - `MediathekViewWebManager._pending` → new `MediathekViewWebManagerState`
  - `RuleSetManager._summaries` → into `RuleSetManagerState`
  - `RuleSetWorker._merged, _paths` → new `RuleSetWorkerState`
  - `FeedCrawlManager._running, _resultReceived` → into `FeedCrawlManagerState`
  - `DownloadManager._maxConcurrent` → into `DownloadManagerState`
- **Fix FeedCrawlWorkerState correctness bug**: record-with-mutable-lists becomes a proper mutable class, eliminating shared-reference hazard.
- **Add Apply methods to RuleSetManagerState**: replace inline `_state = _state with { ... }` in actor with proper `Apply` calls.
- **Replace `ImmutableList`/`ImmutableDictionary` in states with mutable equivalents** (`List<T>`, `Dictionary<TKey, TValue>`) since state mutation is now explicit and Akka mailbox guarantees single-threaded access.
- **Update `actor-state-management` spec** to reflect class-based pattern, void Apply, and mutable collections.
- **Update CLAUDE.md** actor state pattern documentation to match new convention.

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `actor-state-management`: Change state type from sealed record to sealed class, Apply from pure-function-returning-new-state to void-mutating, collections from immutable to mutable. GetSnapshot/FromSnapshot/GetPersistenceState/FromPersistence patterns remain unchanged.

## Impact

- **All domain projects** with actors: Search, Download, RuleSet, Scoring, History, Feed, Enrichment
- **FunkArr.Persistence**: No change (persistence records stay as sealed records — they ARE data, not mutable state)
- **FunkArr.Messages**: No change
- **Test projects**: State setup in tests changes from `with`-expressions to property assignments or Apply calls
- **No API changes, no persistence migration, no behavioral changes** — purely internal refactor
