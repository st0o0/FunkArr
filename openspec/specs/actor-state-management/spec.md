# actor-state-management

## Purpose

Defines the actor state pattern: state records in dedicated files, Empty factory, Apply extension methods, ProcessCommand for persistent actors, query methods on state, thin actors, immutable collections, state-as-snapshot persistence, LastSequenceNr-based snapshot intervals, and persistence records in FunkArr.Persistence.

## Requirements

### Requirement: State records live in dedicated files

Every actor with state SHALL have its state defined as a `sealed record` in a dedicated `<ActorName>State.cs` file in the same project and namespace as the actor. State records SHALL NOT be nested inside actor classes.

#### Scenario: MatchHistoryWorker state file
- **WHEN** the MatchHistoryWorker actor is examined
- **THEN** its state SHALL be defined in `MatchHistoryState.cs` in `FunkArr.MatchMagic`

#### Scenario: Non-persistent actor state file
- **WHEN** the RuleSetResolver actor is examined
- **THEN** its state SHALL be defined in `RuleSetResolverState.cs` in `FunkArr.RuleSet`

#### Scenario: No nested State records in actors
- **WHEN** any actor class is examined
- **THEN** it SHALL NOT contain a nested `record State` declaration

### Requirement: State records provide an Empty factory

Each state record SHALL expose a `public static readonly` `Empty` field returning the initial (zero) state.

#### Scenario: MatchHistoryState.Empty
- **WHEN** `MatchHistoryState.Empty` is accessed
- **THEN** it SHALL return a state with an empty `ImmutableList<ScoringSnapshot>`

#### Scenario: RuleSetResolverState.Empty
- **WHEN** `RuleSetResolverState.Empty` is accessed
- **THEN** it SHALL return a state with empty immutable dictionaries

### Requirement: State evolution via Apply extension methods

State transitions SHALL be implemented as `Apply` extension methods on the state record. Each `Apply` method SHALL be a pure function: take current state and an input, return new state. It SHALL NOT mutate the input state.

#### Scenario: Persistent actor Apply takes a persistence record
- **WHEN** `MatchHistoryState.Apply(ScoringRecorded)` is called
- **THEN** it SHALL return a new `MatchHistoryState` with the record applied, without modifying the original state

#### Scenario: Non-persistent actor Apply takes a command
- **WHEN** `RuleSetResolverState.Apply(RegisterRuleSet)` is called
- **THEN** it SHALL return a new `RuleSetResolverState` with the registration applied, without modifying the original state

### Requirement: ProcessCommand for persistent actors

Persistent actors SHALL implement a `ProcessCommand` extension method on the state record. `ProcessCommand` SHALL validate the command against current state and return both the new state and the persistence record.

#### Scenario: ProcessCommand produces persistence record
- **WHEN** `MatchHistoryState.ProcessCommand(RecordScoringResult)` is called with a valid command
- **THEN** it SHALL return a tuple of `(MatchHistoryState, ScoringRecorded)` containing the new state and the record to persist

### Requirement: Query methods on state

Read-only operations SHALL be implemented as extension methods on the state record. The actor SHALL delegate query handling to these methods.

#### Scenario: QueryHistory on state
- **WHEN** `MatchHistoryState.QueryHistory(QueryScoringHistory)` is called
- **THEN** it SHALL return a `ScoringHistoryResult` computed from the current state

#### Scenario: QueryDetail on state
- **WHEN** `MatchHistoryState.QueryDetail(QueryScoringDetail)` is called
- **THEN** it SHALL return either a `ScoringDetailResult` or `ScoringDetailNotFound`

### Requirement: Actors are thin plumbing

Actor classes SHALL contain only: message routing (`Receive<T>`/`Command<T>`), persistence calls (`Persist`, `SaveSnapshot`), recovery setup (`Recover<T>`), lifecycle management (passivation, timeouts), and DI constructor parameters. All state logic, validation, and query computation SHALL be delegated to state extension methods.

#### Scenario: Persistent actor command handling
- **WHEN** a MatchHistoryWorker receives a RecordScoringResult
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

### Requirement: State-as-snapshot for persistent actors

Persistent actors SHALL pass their state record directly to `SaveSnapshot()`. There SHALL be no separate snapshot DTO types or manual snapshot mapping methods (`CreateSnapshot`, `RestoreFromSnapshot`).

#### Scenario: Save snapshot
- **WHEN** the snapshot interval is reached
- **THEN** the actor SHALL call `SaveSnapshot(_state)` directly

#### Scenario: Recover from snapshot
- **WHEN** a `SnapshotOffer` is received during recovery
- **THEN** the actor SHALL cast `offer.Snapshot` to the state type and assign it directly

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
