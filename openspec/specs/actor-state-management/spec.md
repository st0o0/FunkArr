# actor-state-management

## Purpose

Defines the actor state pattern: state records in dedicated files, Empty factory, Apply extension methods, ProcessCommand for persistent actors, query methods on state, thin actors, immutable collections, Pathfinder snapshot pattern (GetSnapshot/FromSnapshot for queries, GetPersistenceState/FromPersistence for persistent actors), LastSequenceNr-based snapshot intervals, and persistence records in FunkArr.Persistence.
## Requirements
### Requirement: State records live in dedicated files

Every actor with state SHALL have its state defined as a `sealed class` in a dedicated `<ActorName>State.cs` file in the same project and namespace as the actor. State classes SHALL NOT be nested inside actor classes.

#### Scenario: HistoryWorker state file
- **WHEN** the HistoryWorker actor is examined
- **THEN** its state SHALL be defined as a `sealed class` in `HistoryState.cs` in `FunkArr.History`

#### Scenario: Non-persistent actor state file
- **WHEN** the RuleSetResolver actor is examined
- **THEN** its state SHALL be defined as a `sealed class` in `RuleSetResolverState.cs` in `FunkArr.RuleSet`

#### Scenario: No nested State records in actors
- **WHEN** any actor class is examined
- **THEN** it SHALL NOT contain a nested state type declaration

### Requirement: State records provide an Empty factory

Each state class SHALL expose a `public static readonly` `Empty` field returning a new default-initialized instance.

#### Scenario: HistoryState.Empty
- **WHEN** `HistoryState.Empty` is accessed
- **THEN** it SHALL return a state with an empty history records collection

#### Scenario: RuleSetResolverState.Empty
- **WHEN** `RuleSetResolverState.Empty` is accessed
- **THEN** it SHALL return a state with empty dictionaries

### Requirement: State evolution via Apply extension methods

State transitions SHALL be implemented as `Apply` instance methods on the state class. Each `Apply` method SHALL mutate the state in-place and return `void`. All Apply methods SHALL be named `Apply` (overloaded by parameter type) — prefixed names like `ApplyRuleSet`, `ApplyProbe`, `StartHarvesting` SHALL NOT be used. Ad-hoc names like `Increment`/`Decrement`, `AddPending`/`RemovePending` SHALL NOT be used.

#### Scenario: Persistent actor Apply takes a persistence record
- **WHEN** `historyState.Apply(historyRecorded)` is called
- **THEN** it SHALL mutate the state instance to reflect the applied record

#### Scenario: Non-persistent actor Apply takes a command
- **WHEN** `resolverState.Apply(registerRuleSet)` is called
- **THEN** it SHALL mutate the state instance to reflect the registration

#### Scenario: No ad-hoc mutation names
- **WHEN** any state class is examined
- **THEN** all state transition methods SHALL be named `Apply`, not `Increment`, `Decrement`, `AddPending`, `RemovePending`, `ApplyXxx`, `StartXxx`, `FireXxx`, or other ad-hoc or prefixed variants

#### Scenario: Actor uses void Apply pattern
- **WHEN** an actor handles a command that modifies state
- **THEN** the actor SHALL call `_state.Apply(msg)` (void, no reassignment needed)

### Requirement: ProcessCommand for persistent actors

Persistent actors SHALL implement a `ProcessCommand` instance method on the state class. `ProcessCommand` SHALL validate the command against current state and return the persistence record. The state SHALL NOT be mutated by ProcessCommand — mutation happens in the Apply call after persistence.

#### Scenario: ProcessCommand produces persistence record
- **WHEN** `historyState.ProcessCommand(recordHistory)` is called with a valid command
- **THEN** it SHALL return a `HistoryRecorded` persistence record without mutating the state

### Requirement: Query methods on state

Read-only operations SHALL be implemented as instance methods on the state class. The actor SHALL delegate query handling to these methods.

#### Scenario: QueryHistory on state
- **WHEN** `historyState.QueryHistory(queryScoringHistory)` is called
- **THEN** it SHALL return a `ScoringHistoryResult` computed from the current state

#### Scenario: QueryDetail on state
- **WHEN** `historyState.QueryDetail(queryScoringDetail)` is called
- **THEN** it SHALL return either a `ScoringDetailResult` or `ScoringDetailFailed`

### Requirement: Actors are thin plumbing

Actor classes SHALL contain only: message routing (`Receive<T>`/`Command<T>`), persistence calls (`Persist`, `SaveSnapshot`), recovery setup (`Recover<T>`), lifecycle management (passivation, timeouts), and DI constructor parameters. All state logic, validation, and query computation SHALL be delegated to state instance methods. All mutable domain state SHALL live in the state object — actors SHALL NOT keep mutable domain state as direct fields (dictionaries, flags, counters). Infrastructure references (actor refs, cancellation tokens, stopwatches) MAY remain on the actor. Transient lifecycle flags (running indicators, in-flight tracking) that are not persisted or snapshotted SHALL be actor fields, not state properties.

#### Scenario: Persistent actor command handling
- **WHEN** a HistoryWorker receives a RecordHistory
- **THEN** the actor SHALL call `_state.ProcessCommand(cmd)`, persist the returned record, and call `_state.Apply(record)` in the persist callback

#### Scenario: Non-persistent actor command handling
- **WHEN** a RuleSetResolver receives a RegisterRuleSet
- **THEN** the actor SHALL call `_state.Apply(msg)` and nothing else for state management

#### Scenario: Actor query handling
- **WHEN** an actor receives a query message
- **THEN** the actor SHALL call the corresponding query method on state and `Sender.Tell()` the result

#### Scenario: No leaked mutable state on actor
- **WHEN** any actor class is examined
- **THEN** it SHALL NOT have mutable domain-state fields (dictionaries, lists, counters, flags) outside of the `_state` object

#### Scenario: No direct state property assignment from actor
- **WHEN** any actor's message handlers are examined
- **THEN** no handler SHALL directly assign to `_state.PropertyName` — all mutation SHALL go through `_state.Apply(...)`

#### Scenario: Transient lifecycle flags on actor
- **WHEN** an actor needs to track whether an operation is currently in-flight
- **THEN** it SHALL use a private field on the actor class, not a property on the state

### Requirement: State properties use private setters

All properties on state classes SHALL use `{ get; private set; }`. Public setters (`{ get; set; }`) SHALL NOT be used on state class properties. This ensures that only the state class itself can mutate its properties, enforcing that all mutation flows through `Apply()` methods.

#### Scenario: State property access level
- **WHEN** any state class property is examined
- **THEN** it SHALL use `{ get; private set; }` or `{ get; }` (readonly), never `{ get; set; }`

#### Scenario: Actor cannot directly set state property
- **WHEN** an actor attempts to assign a value to `_state.SomeProperty`
- **THEN** the compiler SHALL reject the assignment because the setter is private to the state class

#### Scenario: Apply method can set state property
- **WHEN** an `Apply()` method on the state class assigns a value to a property
- **THEN** the assignment SHALL compile because private setters are accessible within the class

### Requirement: Infrastructure flags live on actors not state

Properties that track transient actor lifecycle (in-flight request flags, running indicators, cancellation tokens) and are NOT persisted, NOT snapshotted, and NOT meaningful for queries SHALL be fields on the actor class, not properties on the state class.

#### Scenario: FeedCrawlManager running flag
- **WHEN** `FeedCrawlManager` needs to track whether a crawl worker is currently running
- **THEN** it SHALL use a private field on the actor (e.g. `private bool _running`), not `_state.Running`

#### Scenario: Persisted state flag stays on state
- **WHEN** a flag is part of the persistent domain state (e.g. `Paused` on `DownloadManagerState`)
- **THEN** it SHALL remain on the state class with an `Apply()` method for mutation

### Requirement: Every state class has Apply methods

Every state class with mutable properties SHALL have at least one `Apply()` method. State classes SHALL NOT be pure property bags without mutation methods.

#### Scenario: RuleSetWorkerState has Apply
- **WHEN** `RuleSetWorkerState` is examined
- **THEN** it SHALL have `Apply()` methods covering all its mutable properties

#### Scenario: No property-bag states
- **WHEN** any state class with `{ get; private set; }` properties is examined
- **THEN** it SHALL have at least one `Apply()` method that mutates those properties

### Requirement: State-as-snapshot replaced by Pathfinder snapshot pattern

Persistent actors SHALL NOT pass their state class directly to `SaveSnapshot()`. Instead, each persistent actor's state SHALL implement `GetPersistenceState()` returning a separate `Persisted*State` record, and a static `FromPersistence(Persisted*State)` factory method to reconstruct state. The persisted record SHALL be a flat, immutable record in `FunkArr.Persistence` containing only the data fields needed for reconstruction - no behavior, no computed properties, no trimming logic.

#### Scenario: Save snapshot via GetPersistenceState
- **WHEN** the snapshot interval is reached in a persistent actor
- **THEN** the actor SHALL call `SaveSnapshot(_state.GetPersistenceState())`

#### Scenario: Recover from snapshot via FromPersistence
- **WHEN** a `SnapshotOffer` is received during recovery
- **THEN** the actor SHALL cast `offer.Snapshot` to the `Persisted*State` type and call `State.FromPersistence(persisted)` to reconstruct the state

#### Scenario: Persisted record is flat data only
- **WHEN** any `Persisted*State` record is examined
- **THEN** it SHALL contain only primitive types and serializable collections - no methods, no computed properties, no logger fields

#### Scenario: Persisted records live in Persistence project
- **WHEN** all `Persisted*State` record types are located
- **THEN** they SHALL reside in the `FunkArr.Persistence` project

### Requirement: All stateful actors provide GetSnapshot and FromSnapshot

Every actor with a state class SHALL have `GetSnapshot()` on its state returning a purpose-built response record, and a static `FromSnapshot()` factory method to reconstruct state from that response. The actor SHALL use `GetSnapshot()` when responding to queries - it SHALL NOT send its internal state directly to callers.

#### Scenario: GetSnapshot returns response record
- **WHEN** an actor receives a query for its state
- **THEN** the actor SHALL call `_state.GetSnapshot()` and tell the result to the sender

#### Scenario: FromSnapshot reconstructs state
- **WHEN** `State.FromSnapshot(snapshot)` is called with a snapshot record
- **THEN** it SHALL return a valid state instance equivalent to the state that produced the snapshot

#### Scenario: Internal state never sent to callers
- **WHEN** any actor's `Receive<T>` handlers are examined
- **THEN** no handler SHALL call `Sender.Tell(_state)` - only snapshot/response records SHALL be sent

### Requirement: Replay-only actors skip persistence layer

Actors that use event replay without snapshots (DownloadManager, DownloadHistoryManager, DownloadWorker) SHALL implement `GetSnapshot()`/`FromSnapshot()` for query responses but SHALL NOT implement `GetPersistenceState()`/`FromPersistence()`. They SHALL NOT call `SaveSnapshot()`.

#### Scenario: Download actor has GetSnapshot but no GetPersistenceState
- **WHEN** `DownloadManagerState` is examined
- **THEN** it SHALL have `GetSnapshot()` and `FromSnapshot()` methods
- **AND** it SHALL NOT have `GetPersistenceState()` or `FromPersistence()` methods

### Requirement: Snapshot interval via LastSequenceNr

Persistent actors SHALL use `LastSequenceNr % snapshotInterval == 0` to determine when to save snapshots. There SHALL be no manual event counter fields.

#### Scenario: Snapshot at interval
- **WHEN** `LastSequenceNr` is a multiple of the configured snapshot interval
- **THEN** a snapshot SHALL be saved

#### Scenario: No manual counter
- **WHEN** any persistent actor is examined
- **THEN** it SHALL NOT have a `_eventsSinceSnapshot` or equivalent counter field

### Requirement: Persistence records in FunkArr.Persistence

Persistence records SHALL be defined in `FunkArr.Persistence/Events/` as `sealed record` types with positional parameters. They SHALL NOT use mutable DTO patterns (`{ get; init; }`). They SHALL NOT have Event, Dto, or Persisted suffixes - just the descriptive name (e.g., `ScoringRecorded`).

#### Scenario: ScoringRecorded is a record
- **WHEN** the ScoringRecorded type is examined
- **THEN** it SHALL be a `sealed record` with positional parameters

#### Scenario: Persistence records live in Persistence project
- **WHEN** all persistence record types are located
- **THEN** they SHALL reside in the `FunkArr.Persistence` project under the `Events/` directory

### Requirement: Serialization uses Akka defaults

Persistence records and state snapshots SHALL use Akka's default serializer. No custom serializer SHALL be registered unless stable manifests are required (post-1.0). At 0.x, breaking changes to persistence are acceptable.

#### Scenario: No custom serializer registered
- **WHEN** the Akka actor system configuration is examined
- **THEN** it SHALL NOT contain a `WithCustomSerializer` registration for persistence types

### Requirement: Mutable collections for state

State classes SHALL use mutable collection types (`List<T>`, `Dictionary<TKey, TValue>`, `HashSet<T>`) for all collection properties. Akka's single-threaded mailbox guarantees safe mutation. Immutable collection types (`ImmutableList<T>`, `ImmutableDictionary<TKey, TValue>`) SHALL NOT be used in state classes.

#### Scenario: RuleSetResolver uses mutable dictionaries
- **WHEN** the RuleSetResolverState class is examined
- **THEN** its LookupIndex SHALL be `Dictionary<string, string>` and EntriesByRuleSetId SHALL be `Dictionary<string, HashSet<string>>`

#### Scenario: FeedCrawlWorkerState uses mutable lists
- **WHEN** FeedCrawlWorkerState is examined
- **THEN** its CollectedItems SHALL be `List<MediathekItem>` and AcceptedEntries SHALL be `List<FeedEntry>`

