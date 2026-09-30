## Why

`DownloadEvents.*` records serve as both actor messages AND Akka.Persistence journal entries. Any change to an event record (renaming fields, changing types, adding properties) breaks deserialization of existing journals. The Njord project solves this with a clean 3-layer separation: Domain Events → Persistence DTOs → Mapping. FunkArr needs the same pattern before the journal contains production data.

## What Changes

- New `Persistence/` folder with versioned DTOs for all persisted events (`DownloadEnqueuedDto`, `DownloadStartedDto`, etc.)
- Each DTO gets a `int Version = 1` field for future schema evolution
- DTOs use primitive types and `[JsonProperty("...")]` with short keys for compact serialization
- Static mapping class `DownloadEventDtoMapping` with `ToDto()` / `ToDomain()` per event type
- `DownloadQueueActor` switched to `Persist(ToDto(evt), ...)` instead of `Persist(evt, ...)`
- `Recover<Dto>(dto => ApplyEvent(ToDomain(dto)))` instead of `Recover<Event>(ApplyEvent)`
- `DownloadEvents.cs` remains as pure domain messages (freely refactorable)

## Capabilities

### New Capabilities
- `persistence-dtos`: Versioned persistence DTOs with bidirectional mapping between domain events and journal format, following the Njord pattern (`{Feature}Dtos.cs` + `{Feature}DtoMapping`)

### Modified Capabilities

## Impact

- `DownloadClient/DownloadQueueActor.cs`: Persist/Recover calls change (DTO instead of event)
- New folder `Persistence/` with `DownloadEventDtos.cs`
- **BREAKING**: Existing journals are incompatible after the change (journal wipe required — no production data yet)
- No API changes, no external behavior changes
