# Persistence DTOs

## Purpose

Provide a dedicated DTO layer for Akka.Persistence events so that persisted journal entries use compact, version-aware data transfer objects rather than domain event types directly. This decouples the persistence schema from domain model evolution and enables short JSON keys, primitive-only storage, and explicit versioning.

## Requirements

### Requirement: Persistence DTOs for all persisted events
The system MUST provide a separate DTO in `Persistence/DownloadEventDtos.cs` for every event persisted via Akka.Persistence. DTOs MUST be `sealed class` with default constructor and public setters. Every DTO MUST have a `[JsonProperty("v")] public int Version { get; set; } = 1;` field.

#### Scenario: All persisted events have a corresponding DTO
- **WHEN** listing the persisted event types (`DownloadEnqueued`, `DownloadStarted`, `DownloadCompleted`, `DownloadFailed`, `MuxingStarted`, `MuxingCompleted`, `MuxingFailed`)
- **THEN** a corresponding DTO (`DownloadEnqueuedDto`, `DownloadStartedDto`, etc.) exists in `Persistence/DownloadEventDtos.cs`

#### Scenario: Non-persisted events have no DTO
- **WHEN** an event is only used as an in-memory message (e.g. `DownloadProgressUpdated`)
- **THEN** no DTO exists for it in `Persistence/`

### Requirement: DTOs use primitive types and short JSON keys
DTOs MUST use primitive types (`string`, `long`, `int`) and `[JsonProperty("...")]` attributes with short keys (2-4 chars) for compact serialization.

#### Scenario: DateTimeOffset is persisted as long
- **WHEN** a domain event has a `DateTimeOffset` field (e.g. `DownloadEnqueued.EnqueuedAt`)
- **THEN** it is stored in the DTO as `long` (UtcTicks) with a short key (e.g. `[JsonProperty("ts")]`)

#### Scenario: String fields keep their type
- **WHEN** a domain event has a `string` field (e.g. `NzoId`)
- **THEN** it is stored in the DTO as `string` with a short key (e.g. `[JsonProperty("nzo")]`)

### Requirement: Bidirectional mapping between domain events and DTOs
A static class `DownloadEventDtoMapping` MUST provide `ToDto()` and `ToDomain()` methods for each event type. The mapping class MUST be defined in the same file as the DTOs (`Persistence/DownloadEventDtos.cs`).

#### Scenario: Domain event to DTO conversion
- **WHEN** `DownloadEventDtoMapping.ToDto(domainEvent)` is called
- **THEN** a DTO with all fields correctly mapped is returned, including type conversions (e.g. `DateTimeOffset` -> `long`)

#### Scenario: DTO to domain event conversion
- **WHEN** `DownloadEventDtoMapping.ToDomain(dto)` is called
- **THEN** a domain event with all fields correctly reconstructed is returned, including type conversions (e.g. `long` -> `DateTimeOffset`)

#### Scenario: Roundtrip consistency
- **WHEN** a domain event is converted to DTO and back (`ToDomain(ToDto(evt))`)
- **THEN** the result is identical to the original event

### Requirement: DownloadQueueActor persists DTOs instead of domain events
The `DownloadQueueActor` MUST convert domain events via `ToDto()` before persisting and via `ToDomain()` when recovering. Domain events MUST NOT be passed directly to `Persist()`.

#### Scenario: Persist writes DTOs to journal
- **WHEN** a domain event is persisted (e.g. `DownloadEnqueued`)
- **THEN** the actor calls `Persist(DownloadEventDtoMapping.ToDto(evt), ...)`

#### Scenario: Recover reads DTOs and converts to domain events
- **WHEN** the actor restores events from the journal at startup
- **THEN** it registers `Recover<DownloadEnqueuedDto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)))` for each DTO type

#### Scenario: ApplyEvent methods continue to work with domain events
- **WHEN** an event is applied after recovery or persist
- **THEN** the existing `ApplyEvent(DownloadEvents.*)` methods are called unchanged (with the converted domain event)
