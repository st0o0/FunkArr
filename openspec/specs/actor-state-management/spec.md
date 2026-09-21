# actor-state-management

## Purpose

Defines the actor state pattern: state records in dedicated files, Empty factory, Apply extension methods, ProcessCommand for persistent actors, query methods on state, thin actors, immutable collections, Pathfinder snapshot pattern (GetSnapshot/FromSnapshot for queries, GetPersistenceState/FromPersistence for persistent actors), LastSequenceNr-based snapshot intervals, and persistence records in FunkArr.Persistence.

## Requirements

### Requirement: State records live in dedicated files

Every actor with state SHALL have its state defined as a `sealed record` in a dedicated `<ActorName>State.cs` file in the same project and namespace as the actor. State records SHALL NOT be nested inside actor classes.

#### Scenario: HistoryWorker state file
- **WHEN** the HistoryWorker actor is examined
- **THEN** its state SHALL be defined in `HistoryState.cs` in `FunkArr.History`

#### Scenario: Non-persistent actor state file
- **WHEN** the RuleSetResolver actor is examined
- **THEN** its state SHALL be defined in `RuleSetResolverState.cs` in `FunkArr.RuleSet`

#### Scenario: No nested State records in actors
- **WHEN** any actor class is examined
- **THEN** it SHALL NOT contain a nested `record State` declaration

### Requirement: State records provide an Empty factory

Each state record SHALL expose a `public static readonly` `Empty` field returning the initial (zero) state.

#### Scenario: HistoryState.Empty
- **WHEN** `HistoryState.Empty` is accessed
- **THEN** it SHALL return a state with an empty history records collection

#### Scenario: RuleSetResolverState.Empty
- **WHEN** `RuleSetResolverState.Empty` is accessed
- **THEN** it SHALL return a state with empty immutable dictionaries

### Requirement: State evolution via Apply extension methods

State transitions SHALL be implemented as `Apply` extension methods on the state record. Each `Apply` method SHALL be a pure function: take current state and an input, return new state. It SHALL NOT mutate the input state. All stateful actors SHALL use `Apply()` naming — ad-hoc names like `Increment`/`Decrement`, `AddPending`/`RemovePending` SHALL NOT be used.

#### Scenario: Persistent actor Apply takes a persistence record
- **WHEN** `HistoryState.Apply(HistoryRecorded)` is called
- **THEN** it SHALL return a new `HistoryState` with the record applied, without modifying the original state

#### Scenario: Non-persistent actor Apply takes a command
- **WHEN** `RuleSetResolverState.Apply(RegisterRuleSet)` is called
- **THEN** it SHALL return a new `RuleSetResolverState` with the registration applied, without modifying the original state

#### Scenario: No ad-hoc mutation names
- **WHEN** any state class is examined
- **THEN** all state transition methods SHALL be named `Apply`, not `Increment`, `Decrement`, `AddPending`, `RemovePending`, or other ad-hoc names

### Requirement: ProcessCommand for persistent actors

Persistent actors SHALL implement a `ProcessCommand` extension method on the state record. `ProcessCommand` SHALL validate the command against current state and return both the new state and the persistence record.

#### Scenario: ProcessCommand produces persistence record
- **WHEN** `HistoryState.ProcessCommand(RecordHistory)` is called with a valid command
- **THEN** it SHALL return a tuple of `(HistoryState, HistoryRecorded)` containing the new state and the record to persist

### Requirement: Query methods on state

Read-only operations SHALL be implemented as extension methods on the state record. The actor SHALL delegate query handling to these methods.

#### Scenario: QueryHistory on state
- **WHEN** `HistoryState.QueryHistory(QueryScoringHistory)` is called
- **THEN** it SHALL return a `ScoringHistoryResult` computed from the current state

#### Scenario: QueryDetail on state
- **WHEN** `HistoryState.QueryDetail(QueryScoringDetail)` is called
- **THEN** it SHALL return either a `ScoringDetailResult` or `ScoringDetailNotFound`

### Requirement: Actors are thin plumbing

Actor classes SHALL contain only: message routing (`Receive<T>`/`Command<T>`), persistence calls (`Persist`, `SaveSnapshot`), recovery setup (`Recover<T>`), lifecycle management (passivation, timeouts), and DI constructor parameters. All state logic, validation, and query computation SHALL be delegated to state extension methods.

#### Scenario: Persistent actor command handling
- **WHEN** a HistoryWorker receives a RecordHistory
- **THEN** the actor SHALL call `_state.ProcessCommand(cmd)`, persist the returned record, and assign `_state` to the returned new state

#### Scenario: Non-persistent actor command handling
- **WHEN** a RuleSetResolver receives a RegisterRuleSet
- **THEN** the actor SHALL call `_state = _state.Apply(msg)` and nothing else for state management

#### Scenario: Actor query handling
- **WHEN** an actor receives a query message
- **THEN** the actor SHALL call the corresponding query method on state and `Sender.Tell()` the result

### Requirement: Immutable collections for all state

State records SHALL use immutable collection types (`ImmutableList<T>`, `ImmutableDictionary<TKey, TValue>`, `ImmutableHashSet<T>`) for all collection properties. Mutable collections inside state records SHALL NOT be used.

#### Scenario: RuleSetResolver uses immutable dictionaries
- **WHEN** the RuleSetResolverState record is examined
- **THEN** its LookupIndex SHALL be `ImmutableDictionary<string, string>` and EntriesByRuleSetId SHALL be `ImmutableDictionary<string, ImmutableHashSet<string>>`

### Requirement: State-as-snapshot replaced by Pathfinder snapshot pattern

Persistent actors SHALL NOT pass their state record directly to `SaveSnapshot()`. Instead, each persistent actor's state SHALL implement `GetPersistenceState()` returning a separate `Persisted*State` record, and a static `FromPersistence(Persisted*State)` factory method to reconstruct state. The persisted record SHALL be a flat, immutable record in `FunkArr.Persistence` containing only the data fields needed for reconstruction — no behavior, no computed properties, no trimming logic.

#### Scenario: Save snapshot via GetPersistenceState
- **WHEN** the snapshot interval is reached in a persistent actor
- **THEN** the actor SHALL call `SaveSnapshot(_state.GetPersistenceState())`

#### Scenario: Recover from snapshot via FromPersistence
- **WHEN** a `SnapshotOffer` is received during recovery
- **THEN** the actor SHALL cast `offer.Snapshot` to the `Persisted*State` type and call `State.FromPersistence(persisted)` to reconstruct the state

#### Scenario: Persisted record is flat data only
- **WHEN** any `Persisted*State` record is examined
- **THEN** it SHALL contain only primitive types and serializable collections — no methods, no computed properties, no logger fields

#### Scenario: Persisted records live in Persistence project
- **WHEN** all `Persisted*State` record types are located
- **THEN** they SHALL reside in the `FunkArr.Persistence` project

### Requirement: All stateful actors provide GetSnapshot and FromSnapshot

Every actor with a state class SHALL have `GetSnapshot()` on its state returning a purpose-built response record, and a static `FromSnapshot()` factory method to reconstruct state from that response. The actor SHALL use `GetSnapshot()` when responding to queries — it SHALL NOT send its internal state record directly to callers.

#### Scenario: GetSnapshot returns response record
- **WHEN** an actor receives a query for its state
- **THEN** the actor SHALL call `_state.GetSnapshot()` and tell the result to the sender

#### Scenario: FromSnapshot reconstructs state
- **WHEN** `State.FromSnapshot(snapshot)` is called with a snapshot record
- **THEN** it SHALL return a valid state instance equivalent to the state that produced the snapshot

#### Scenario: Internal state never sent to callers
- **WHEN** any actor's `Receive<T>` handlers are examined
- **THEN** no handler SHALL call `Sender.Tell(_state)` or `Sender.Tell(_state.SomeInternalCollection)` — only snapshot/response records SHALL be sent

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

Persistence records SHALL be defined in `FunkArr.Persistence/Events/` as `sealed record` types with positional parameters. They SHALL NOT use mutable DTO patterns (`{ get; init; }`). They SHALL NOT have Event, Dto, or Persisted suffixes — just the descriptive name (e.g., `ScoringRecorded`).

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
