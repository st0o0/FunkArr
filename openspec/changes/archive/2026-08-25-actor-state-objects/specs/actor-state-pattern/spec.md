## ADDED Requirements

### Requirement: Dedicated state class per persistent actor

Each persistent actor SHALL have a dedicated `*State` class in a sibling file (`*State.cs`) within the same namespace. The state class SHALL be `sealed class` with `private set` properties. The actor SHALL hold a single `private readonly *State _state` field.

#### Scenario: State class exists for each persistent actor
- **WHEN** a persistent actor class exists (inheriting `ReceivePersistentActor`)
- **THEN** a corresponding `*State.cs` file SHALL exist in the same directory with a `sealed class` containing all mutable domain state

#### Scenario: Actor contains no mutable domain state fields
- **WHEN** state has been extracted to a state class
- **THEN** the actor class SHALL contain no private mutable fields for domain state (only `_state`, `_log`, and infrastructure refs like `_refreshActor`)

### Requirement: State mutation via Apply methods

All state mutations SHALL occur through `Apply(TEvent)` methods on the state class. The actor SHALL NOT mutate state properties directly.

#### Scenario: Event replay during recovery
- **WHEN** the actor recovers persisted events
- **THEN** each event SHALL be applied via `_state.Apply(event.ToDomain())` to reconstruct state

#### Scenario: Event application after persist
- **WHEN** the actor persists a new domain event
- **THEN** the persist callback SHALL call `_state.Apply(event)` to update runtime state

### Requirement: Guard properties on state

Domain-level guard conditions SHALL be expressed as named properties on the state class rather than inline field checks on the actor.

#### Scenario: Idempotency guard
- **WHEN** an actor needs to check whether it has already been initialized
- **THEN** the check SHALL be a property on state (e.g. `_state.IsInitialized`) not an inline field check (e.g. `!string.IsNullOrEmpty(_title)`)

### Requirement: Snapshot conversion on state

For actors that use snapshots, the state class SHALL provide a `ToSnapshot()` method returning the snapshot record and a static `FromSnapshot(TSnapshot)` factory method to reconstruct state from a snapshot.

#### Scenario: Saving a snapshot
- **WHEN** the actor triggers a snapshot save
- **THEN** it SHALL call `SaveSnapshot(_state.ToSnapshot())`

#### Scenario: Recovering from a snapshot
- **WHEN** a `SnapshotOffer` is received during recovery
- **THEN** the actor SHALL reconstruct state via the state class factory method (e.g. `_state = ShowActorState.FromSnapshot(snapshot)`)

### Requirement: No partial classes

Actor classes SHALL NOT use the `partial` keyword. Each actor SHALL be contained in a single `.cs` file. Nested message records, Become behaviors, recovery handlers, and command handlers SHALL all reside in that single file.

#### Scenario: ShowActor consolidation
- **WHEN** ShowActor is refactored
- **THEN** the content from `ShowActor.Events.cs` and `ShowActor.Messages.cs` SHALL be consolidated — messages inline on the actor, events in a standalone `ShowActorEvents.cs` static class

#### Scenario: Events remain in separate file
- **WHEN** domain event records exist for a persistent actor
- **THEN** events SHALL remain in a separate `*Events.cs` file as a static class (not partial, not nested) since they are referenced by persistence DTO mappers in another namespace

### Requirement: Non-persistent actors unchanged

Non-persistent actors (inheriting `ReceiveActor`) SHALL NOT be refactored to use state objects.

#### Scenario: Simple actor left as-is
- **WHEN** an actor like BrowseActor, MediathekGatewayActor, or a pipeline worker actor uses only simple state fields
- **THEN** it SHALL remain unchanged with state as direct private fields
