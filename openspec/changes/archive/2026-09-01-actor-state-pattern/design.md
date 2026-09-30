## Context

FunkArr actors currently manage state as private nested records inside actor classes. The single persistent actor (MatchHistoryWorker) uses a DTO-based persistence pattern with explicit mapper classes, manual snapshot create/restore methods, and a manual event counter for snapshot intervals. This creates coupling between state logic, persistence plumbing, and the actor itself — making state logic untestable without actor infrastructure.

DrawTogether.NET (by Aaron Stannard / Petabridge) demonstrates a cleaner pattern: State lives in its own file with `Apply` and `ProcessCommand` extension methods, domain events are persisted directly (no DTO layer), and the state record itself is the snapshot. The actor becomes thin plumbing.

## Goals / Non-Goals

**Goals:**
- Extract state logic from all 7 stateful actors into dedicated state files with extension methods
- Replace persistence DTOs with domain event records for MatchHistoryWorker
- Eliminate ScoringRecordedMapper and manual snapshot mapping
- Establish a consistent, testable state pattern for all current and future actors
- Add custom JSON Akka serializer for event/snapshot versioning

**Non-Goals:**
- Switching serialization format (staying with JSON, not moving to Protobuf)
- Migrating existing journal data (0.x version, clean break)
- Changing actor lifecycle, supervision, or sharding configuration
- Extracting MatchMagicActor's scoring logic (stateless, no state to extract)
- Adding new persistence to non-persistent actors

## Decisions

### Decision: State files live alongside actors in domain projects

State records and their extension methods live in the same domain project as their actor, in a dedicated file named `<ActorName>State.cs`.

```
FunkArr.MatchMagic/
├── MatchHistoryWorker.cs        ← thin actor
├── MatchHistoryState.cs         ← State record + Apply + ProcessCommand + queries
├── MatchMagicManager.cs
└── MatchMagicManagerState.cs
```

**Why not in Persistence?** State contains domain logic (ProcessCommand validates commands, Apply evolves state). Persistence holds what gets serialized to the journal — events and serializer. Putting domain logic in Persistence would violate the reference direction (Persistence shouldn't know about domain concepts beyond what it stores).

**Why not in a new shared project?** Each state is specific to its actor. No cross-domain state sharing exists or is planned.

### Decision: Domain event records in FunkArr.Persistence

Persistent events are defined as sealed records in `FunkArr.Persistence/Events/`. These are the types that Akka.Persistence serializes to the journal. Domain projects reference Persistence transitively through Core, so actors can use these event types.

```
FunkArr.Persistence/
├── Events/
│   └── MatchHistory/
│       └── ScoringRecordedEvent.cs    ← sealed record, replaces ScoringRecordedDto
└── Serialization/
    └── FunkArrJsonSerializer.cs       ← custom Akka serializer
```

Events use `sealed record` with positional parameters (not mutable DTOs with `{ get; init; }`). Since we're doing a clean break, no need for backwards compatibility with old DTO format.

**Why events in Persistence, not Messages?** Events represent what gets persisted — they're a persistence concern. Messages holds commands/queries/responses that flow between actors. Events flow into the journal. Different lifecycles, different concerns.

### Decision: Custom JSON Akka serializer for versioning

A single `FunkArrJsonSerializer` registered as a custom Akka serializer handles all event and snapshot types. It uses `System.Text.Json` for serialization and owns the versioning contract.

```csharp
public sealed class FunkArrJsonSerializer : SerializerWithStringManifest
{
    // Manifest string = type discriminator for journal
    // Version embedded in manifest or event record
    // Handles forward/backward compat for future changes
}
```

**Why custom serializer over default?** Default Akka serialization uses type names as manifests, which break on namespace/assembly refactoring. A custom serializer uses stable manifest strings decoupled from CLR types.

**Why SerializerWithStringManifest?** Gives us control over the manifest string stored in the journal. Each event type gets a stable manifest (e.g., `"scoring-recorded-v1"`). Future versions can add new manifests and convert old ones during deserialization.

### Decision: State IS the snapshot

For persistent actors, the State record is directly passed to `SaveSnapshot()`. No separate snapshot DTO or mapping. The same custom serializer handles snapshot serialization.

```csharp
// Save
if (LastSequenceNr % 20 == 0) SaveSnapshot(_state);

// Recover
Recover<SnapshotOffer>(offer => _state = (MatchHistoryState)offer.Snapshot);
```

**Why this works:** State records are immutable (`sealed record` with `ImmutableList`). They serialize cleanly with `System.Text.Json`. The serializer handles any future state shape changes.

### Decision: LastSequenceNr % N for snapshot interval

Replace the manual `_eventsSinceSnapshot` counter with Akka.Persistence's built-in `LastSequenceNr`.

```csharp
// Before: manual counter
_eventsSinceSnapshot++;
if (_eventsSinceSnapshot >= _snapshotInterval) { SaveSnapshot(...); _eventsSinceSnapshot = 0; }

// After: built-in
if (LastSequenceNr % snapshotInterval == 0) SaveSnapshot(_state);
```

Fewer moving parts, works correctly across recovery (LastSequenceNr is set by the journal).

### Decision: Extension methods on State records

State evolution is implemented as extension methods rather than instance methods. This keeps the record itself a pure data declaration and groups behavior separately.

```csharp
public sealed record MatchHistoryState(ImmutableList<ScoringSnapshot> Snapshots)
{
    public static readonly MatchHistoryState Empty = new([]);
}

public static class MatchHistoryStateExtensions
{
    public static MatchHistoryState Apply(this MatchHistoryState state, ScoringRecordedEvent evt) => ...;
    public static (MatchHistoryState, ScoringRecordedEvent) ProcessCommand(this MatchHistoryState state, RecordScoringResult cmd) => ...;
    public static ScoringHistoryResult QueryHistory(this MatchHistoryState state, QueryScoringHistory query) => ...;
}
```

**Why extension methods over methods on the record?** Follows the DrawTogether.NET pattern. Keeps the record focused on data shape. Extension methods are independently testable and can be organized by concern if needed.

### Decision: Non-persistent actors get the same State extraction

All stateful actors — persistent or not — get their state extracted to a dedicated file with extension methods. Non-persistent actors don't need `ProcessCommand` (no events to produce), but they get `Apply` for mutations and query methods for reads.

For non-persistent actors, `Apply` takes the command/message directly (no intermediate event):

```csharp
// Non-persistent: Apply takes the command
public static RuleSetResolverState Apply(this RuleSetResolverState state, RegisterRuleSet msg) => ...;

// Persistent: Apply takes the event
public static MatchHistoryState Apply(this MatchHistoryState state, ScoringRecordedEvent evt) => ...;
```

### Decision: Immutable state for RuleSetResolver

RuleSetResolver currently uses mutable `Dictionary` inside a State record (mutation through reference). Switch to `ImmutableDictionary` for consistency with the immutable state pattern:

```csharp
public sealed record RuleSetResolverState(
    ImmutableDictionary<string, string> LookupIndex,
    ImmutableDictionary<string, ImmutableHashSet<string>> EntriesByRuleSetId);
```

## Risks / Trade-offs

- **Performance of ImmutableDictionary for RuleSetResolver** → RuleSetResolver's dictionaries are small (tens of entries). Immutable collections are fine at this scale. If hot-path profiling shows issues, can switch to a builder pattern.
- **Custom serializer maintenance** → One more type to maintain. But it's a single class handling all persistence types, and it replaces per-type mapper classes — net reduction in code.
- **State files proliferate** → 7 new `*State.cs` files. Acceptable — each is focused and independently testable.
- **Breaking persistence** → Old journal data is incompatible. Acceptable at 0.x. On first run after upgrade, the SQLite database should be deleted or the service started fresh.
