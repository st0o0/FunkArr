# Persistence DTOs

## Purpose

Provide a dedicated DTO layer for Akka.Persistence events so that persisted journal entries use compact, version-aware data transfer objects rather than domain event types directly. This decouples the persistence schema from domain model evolution and enables short JSON keys, primitive-only storage, and explicit versioning.

## Requirements

### Requirement: Domain-scoped persistence journal files
The system MUST organize persistence DTOs into four domain-scoped files in `FunkArr.Persistence/`:

- `DownloadCoordinatorJournal.cs` — DTOs for `DownloadActor` (sharded download entity): `DcJobAccepted`, `DcStageEntered`, `DcJobCompleted`, `DcJobFailed`, `DcJobCancelled`. Extension methods in `DownloadActorJournalExtensions`.
- `DownloadRequestTrackerJournal.cs` — DTOs for `DownloadRequestActor` (sharded tracker entity): `RequestCreated`, `RequestStatusChanged`, `RequestCompleted`, `RequestFailed`. Extension methods in `DownloadRequestActorJournalExtensions`.
- `QueueCoordinatorJournal.cs` — DTOs for `QueueActor` (event-sourced queue): `QueueJobEnqueued`, `QueueJobStarted`, `QueueJobFinished`, `QueueJobRemoved`. Extension methods in `QueueActorJournalExtensions`.
- `MatchQualityJournal.cs` — DTOs for `MatchQualityActor` (event-sourced match cache): `MatchRecordedJournal`, `MatchesExpiredJournal`. Extension methods in `MatchQualityJournalExtensions`.

#### Scenario: All persisted events have a corresponding DTO
- **WHEN** listing the persisted domain events across all four actor types
- **THEN** each event has a corresponding DTO in the appropriate journal file

#### Scenario: Non-persisted events have no DTO
- **WHEN** an event is only used as an in-memory message (e.g. progress reports)
- **THEN** no DTO exists for it in `Persistence/`

### Requirement: DTOs use primitive types and short JSON keys
DTOs MUST be `sealed class` with default constructor and public setters. Every DTO MUST have a `[JsonProperty("v")] public int Version { get; set; } = 1;` field. DTOs MUST use primitive types (`string`, `long`, `int`) and `[JsonProperty("...")]` attributes with short keys (2-4 chars) for compact serialization.

#### Scenario: DateTimeOffset is persisted as long
- **WHEN** a domain event has a `DateTimeOffset` field (e.g. `EnqueuedAt`)
- **THEN** it is stored in the DTO as `long` UtcTicks with a short key (e.g. `[JsonProperty("ts")]`)

#### Scenario: String fields keep their type
- **WHEN** a domain event has a `string` field (e.g. `NzoId`)
- **THEN** it is stored in the DTO as `string` with a short key (e.g. `[JsonProperty("nzo")]`)

#### Scenario: Complex types serialized as JSON strings
- **WHEN** a domain event carries a complex type (e.g. `MatchRecord` in `MatchQualityActor.MatchRecorded`)
- **THEN** it is serialized to a JSON string field in the DTO (e.g. `[JsonProperty("r")] public string RecordJson`)

### Requirement: Bidirectional mapping via extension methods
Each journal file MUST provide `ToJournal()` and `ToDomain()` extension methods on domain events and DTOs respectively. These live in a static extensions class per file (e.g. `DownloadActorJournalExtensions`).

#### Scenario: Domain event to DTO conversion
- **WHEN** `evt.ToJournal()` is called on a domain event
- **THEN** a DTO with all fields correctly mapped is returned, including type conversions (e.g. `DateTimeOffset` -> `long` UtcTicks)

#### Scenario: DTO to domain event conversion
- **WHEN** `dto.ToDomain()` is called on a journal DTO
- **THEN** a domain event with all fields correctly reconstructed is returned, including type conversions (e.g. `long` UtcTicks -> `DateTimeOffset`)

#### Scenario: Roundtrip consistency
- **WHEN** a domain event is converted to DTO and back (`evt.ToJournal().ToDomain()`)
- **THEN** the result is semantically identical to the original event

### Requirement: Actors persist DTOs via extension methods
Persistent actors MUST convert domain events via `ToJournal()` before calling `Persist()` and via `ToDomain()` when recovering. Domain events MUST NOT be passed directly to `Persist()`.

#### Scenario: Persist writes DTOs to journal
- **WHEN** a domain event is persisted (e.g. `DownloadRequestActorEvents.RequestCreated`)
- **THEN** the actor calls `Persist(evt.ToJournal(), ...)`

#### Scenario: Recover reads DTOs and converts to domain events
- **WHEN** the actor restores events from the journal at startup
- **THEN** it registers `Recover<RequestCreated>(dto => Apply(dto.ToDomain()))` for each DTO type

### Requirement: Category field in persistence DTOs
Persistence DTOs for events that carry category information SHALL include a nullable `Category` field with short JSON key `"cat"`. This applies to `DcJobAccepted` (DownloadActor), `RequestCreated` (DownloadRequestActor), and `QueueJobEnqueued` (QueueActor).

#### Scenario: New event with category serialized
- **WHEN** a domain event with category `"tv"` is converted to DTO
- **THEN** the DTO SHALL include `[JsonProperty("cat")] public string? Category { get; set; }` with value `"tv"`

#### Scenario: Old event without category deserialized
- **WHEN** a persisted DTO from before category support (no `cat` key in JSON) is deserialized
- **THEN** the `Category` property SHALL be `null` (nullable default)
