## Why

All 11 Persistence enums currently start with a domain value at position 0
(e.g. `Show = 0`, `Queued = 0`). This means `default(T)` silently produces a
valid-looking value instead of a detectable sentinel. Messages-layer enums
already follow the `Unknown = 0` convention for newer enums, but 9 older
Messages enums and 1 Api enum still lack it. Since we are in v0.x, we can
break the persistence contract once and align everything.

## What Changes

- **BREAKING**: All 11 `Persisted*` enums gain `Unknown = 0`; existing members
  are renumbered starting at 1. Stored journal/snapshot data from before this
  change becomes invalid.
- **BREAKING**: `PersistedDownloadPriority` moves from semantic range
  (`Paused = -2 .. High = 1`) to sequential (`Unknown = 0, Paused = 1, ...,
  High = 4`).
- 9 Messages-layer enums (`DownloadPhase`, `DownloadStatus`, `FailureKind`,
  `WorkerStatus`, `DownloadPriority`, and 4 RuleSet failure-reason enums) gain
  `Unknown = 0` with members renumbered.
- 1 Api-layer enum (`QueuePriority`) gains `Unknown = 0` with members
  renumbered to match Messages `DownloadPriority`.
- All enum-to-enum mapping extensions updated to exhaustive `switch`
  expressions that throw for `Unknown` via the `_ => throw` arm.
- Tests updated for new numeric values where asserted.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `persistence-enum-values`: Existing members renumbered; `Unknown = 0` added
  to all Persistence enums including `PersistedDownloadPriority` (range to
  sequential).
- `domain-enums`: 9 Messages enums and 1 Api enum gain `Unknown = 0`.
- `enum-mapping-switch`: All mapping extensions verified/updated for new
  numbering and exhaustive `Unknown` handling.

## Impact

- **Persistence**: Journal and snapshot data written before this change will
  deserialize with shifted values. Requires data wipe or fresh start (acceptable
  at v0.x).
- **FunkArr.Persistence**: 11 enum files changed.
- **FunkArr.Messages**: 9 enum files changed.
- **FunkArr.Api**: 1 enum file changed (`QueuePriority`).
- **Mapping extensions**: All `*MappingExtensions.cs` in domain projects and
  `FunkArr.Api/Extensions/` touched to handle renumbered values and `Unknown`
  arm.
- **Tests**: Assertions on enum values, mapping round-trips, and persistence
  DTOs need updating.
- **No API contract change for external consumers**: Newznab/SABnzbd APIs
  (ArrApi) already have their own `Unknown = 0` enums; internal REST API enum
  values change (integer serialization), but the UI is internal.
