## MODIFIED Requirements

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

## REMOVED Requirements

### Requirement: State-as-snapshot for persistent actors
**Reason**: Replaced by Pathfinder snapshot pattern (separate `Persisted*State` records). Passing state directly to `SaveSnapshot()` couples internal state evolution to the persistence format.
**Migration**: Each persistent actor's state gets `GetPersistenceState()` → `Persisted*State` record, and `FromPersistence()` factory. `SaveSnapshot(_state)` becomes `SaveSnapshot(_state.GetPersistenceState())`.
