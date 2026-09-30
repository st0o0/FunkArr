## MODIFIED Requirements

### Requirement: Persistence DTOs for all persisted events
The system MUST provide a separate journal type in `Persistence/*Journal.cs` (one file per actor domain) for every event persisted via Akka.Persistence. Journal types MUST be `sealed class` with default constructor and public setters. Every journal type MUST have a `[JsonProperty("v")] public int Version { get; set; } = 1;` field. Journal types SHALL NOT use a `Dto` suffix — the `FunkArr.Persistence` namespace provides the context.

#### Scenario: All persisted events have a corresponding journal type
- **WHEN** listing the persisted event types across all four actor domains (QueueCoordinator, DownloadCoordinator, DownloadRequestTracker, MatchQualityWorker)
- **THEN** a corresponding journal type (e.g. `QueueJobEnqueued`, `DcJobAccepted`, `RequestCreated`, `MatchRecorded`) SHALL exist in `Persistence/*Journal.cs`

#### Scenario: Journal types organized per actor domain
- **WHEN** examining the Persistence directory
- **THEN** journal types SHALL be organized in files named by actor domain: `QueueCoordinatorJournal.cs`, `DownloadCoordinatorJournal.cs`, `DownloadRequestTrackerJournal.cs`, `MatchQualityJournal.cs`

#### Scenario: Non-persisted events have no journal type
- **WHEN** an event is only used as an in-memory message (e.g. progress reports)
- **THEN** no journal type exists for it in `Persistence/`

### Requirement: Bidirectional mapping between domain events and DTOs
Extension methods in `FunkArr.Persistence` SHALL provide `ToJournal()` and `ToDomain()` conversions for each event type. The extension methods SHALL be defined in the same file as their journal types.

#### Scenario: Domain event to journal conversion
- **WHEN** `domainEvent.ToJournal()` is called on a domain event
- **THEN** a journal type with all fields correctly mapped SHALL be returned, including type conversions (e.g. `DateTimeOffset` → `long`)

#### Scenario: Journal to domain event conversion
- **WHEN** `journalType.ToDomain()` is called on a journal type
- **THEN** a domain event with all fields correctly reconstructed SHALL be returned, including type conversions (e.g. `long` → `DateTimeOffset`)

#### Scenario: Roundtrip consistency
- **WHEN** a domain event is converted to journal and back (`evt.ToJournal().ToDomain()`)
- **THEN** the result SHALL be identical to the original event

### Requirement: DownloadQueueActor persists DTOs instead of domain events
All persistent actors (QueueCoordinator, DownloadCoordinator, DownloadRequestTracker, MatchQualityWorker) MUST convert domain events via `ToJournal()` extension method before persisting and via `ToDomain()` extension method when recovering. Domain events MUST NOT be passed directly to `Persist()`.

#### Scenario: Persist writes journal types
- **WHEN** a domain event is persisted (e.g. `JobEnqueued`)
- **THEN** the actor calls `Persist(evt.ToJournal(), ...)`

#### Scenario: Recover reads journal types and converts to domain events
- **WHEN** the actor restores events from the journal at startup
- **THEN** it registers `Recover<QueueJobEnqueued>(j => ApplyEvent(j.ToDomain()))` for each journal type

#### Scenario: Apply methods continue to work with domain events
- **WHEN** an event is applied after recovery or persist
- **THEN** the existing apply methods are called unchanged (with the converted domain event)

## RENAMED Requirements

### Requirement: DTOs use primitive types and short JSON keys
- **FROM:** DTOs use primitive types and short JSON keys
- **TO:** Journal types use primitive types and short JSON keys
