## Context

FunkArr uses Akka.Persistence event-sourcing in `DownloadQueueActor`. Currently, domain events (`DownloadEvents.*`) are persisted directly to the journal. This couples the message schema to the persistence schema — any change to an event record breaks deserialization of existing journal entries.

The Njord project (same author, same tech stack) solves this with a proven 3-layer architecture:
- **Domain events** (freely changeable, actor messages)
- **Persistence DTOs** (versioned, stable, flat types)
- **Mapping** (bidirectional, co-located with DTOs)

## Goals / Non-Goals

**Goals:**
- Decouple domain events from journal schema
- Versioned DTOs for future schema evolution
- Compact journal entries via short JSON property names
- Consistency with the Njord pattern

**Non-Goals:**
- Migrating existing journals (journal wipe is acceptable)
- Snapshot persistence (not currently used, can be added later)
- Generic mapper interfaces or abstractions — each feature gets concrete types

## Decisions

### 1. A single `Persistence/` folder at project level

DTOs and mappings live in `src/FunkArr/Persistence/`, not alongside the feature folders.

**Why:** Njord pattern. All persistence concerns in one place. Makes it clearly visible what goes into the journal and what doesn't. Alternative would be DTOs next to the events (`DownloadClient/DownloadEventDtos.cs`) — but that mixes concerns at the folder level again.

### 2. One file per feature area: DTOs + mapping together

`DownloadEventDtos.cs` contains all download event DTOs AND the static mapping class `DownloadEventDtoMapping`.

**Why:** Njord pattern. Mapping belongs with the DTO, not with the domain event. Keeps related code together. Alternative would be separate mapper files — overkill for the current size (7 event types).

### 3. DTOs as `sealed class` with `[JsonProperty]`, not `record`

```
public sealed class DownloadEnqueuedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    ...
}
```

**Why:** Njord pattern. Classes with default constructor and setters are JSON-serializer-friendly (Newtonsoft.Json, which Akka.Persistence uses). Records with constructor parameters can cause issues when deserializing older versions (missing properties → exception instead of default). Version field enables future schema evolution in the mapper.

### 4. Short JSON property names

`[JsonProperty("nzo")]` instead of `NzoId`, `[JsonProperty("url")]` instead of `DownloadUrl`.

**Why:** More compact journal entries. With event-sourcing, entries accumulate. Njord pattern.

### 5. `DownloadProgressUpdated` gets no DTO

This event is never persisted (in-memory state update only). Remains a pure actor message.

**Why:** No Persist call exists, no Recover registered. A DTO would be dead code.

## Risks / Trade-offs

- **[Journal incompatibility]** → One-time journal wipe on deployment. Acceptable since no production data exists yet.
- **[Mapping boilerplate]** → Each event needs ToDto/ToDomain. Trade-off for decoupling. Manageable at 7 events.
- **[Newtonsoft.Json dependency]** → DTOs use `[JsonProperty]` (Newtonsoft). Akka.Persistence.Sql uses Newtonsoft as default serializer, so this is consistent.

## Open Questions

- Should a snapshot DTO for the full actor state (`Dictionary<string, DownloadJob>`) be prepared now, or only when snapshots are actually used?
