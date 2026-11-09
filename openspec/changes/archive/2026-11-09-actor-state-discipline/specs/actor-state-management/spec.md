## ADDED Requirements

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

## MODIFIED Requirements

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
