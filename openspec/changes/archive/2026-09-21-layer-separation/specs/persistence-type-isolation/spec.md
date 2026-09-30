## ADDED Requirements

### Requirement: Persistence project has no reference to Messages
FunkArr.Persistence SHALL NOT have a ProjectReference to FunkArr.Messages. All types used in persistence events and snapshots MUST be defined within FunkArr.Persistence itself.

#### Scenario: No Messages dependency
- **WHEN** `FunkArr.Persistence.csproj` is inspected
- **THEN** it contains no ProjectReference to FunkArr.Messages

#### Scenario: Persistence compiles independently
- **WHEN** FunkArr.Persistence is built in isolation
- **THEN** it compiles without any transitive dependency on FunkArr.Messages types

### Requirement: Persisted enum copies for MediaType and SearchSource
FunkArr.Persistence SHALL define its own `PersistedMediaType` and `PersistedSearchSource` enums with identical integer values to the Messages versions. These MUST be used in all persistence event records instead of the Messages enums.

#### Scenario: PersistedMediaType used in Download events
- **WHEN** `DownloadInitialized` or `Download.HistoryRecorded` is stored in the journal
- **THEN** the `Category`/`MediaType` field uses `PersistedMediaType`, not `Messages.MediaType`

#### Scenario: PersistedSearchSource used in ScoringHistory events
- **WHEN** `ScoringHistory.HistoryRecorded` is stored in the journal
- **THEN** the `Source` field uses `PersistedSearchSource`, not `Messages.SearchSource`

#### Scenario: Enum integer values match Messages
- **WHEN** `PersistedMediaType` and `PersistedSearchSource` are compared to their Messages counterparts
- **THEN** each named value has the same integer backing value

### Requirement: Persisted ItemTrace type tree
FunkArr.Persistence SHALL define its own copy of the `ItemTrace` record tree used in `ScoringHistory.HistoryRecorded`. The tree includes: `PersistedItemTrace`, `PersistedRuleTrace`, `PersistedFilterGroupTrace`, `PersistedFilterNodeTrace`, `PersistedTracedIdentification`, `PersistedEnrichmentTrace`, and `PersistedRuleOutcome` enum.

#### Scenario: HistoryRecorded uses Persisted types
- **WHEN** `ScoringHistory.HistoryRecorded` event is defined
- **THEN** its `ItemTraces` field uses `PersistedItemTrace[]`, not `Messages.Scoring.History.ItemTrace[]`

#### Scenario: JSON backward compatibility
- **WHEN** an existing journal entry serialized from the old Messages-based types is deserialized into the new Persisted types
- **THEN** deserialization succeeds with all field values preserved (Newtonsoft.Json property names match)

### Requirement: Mapping methods between Messages and Persistence
Domain actors that persist events SHALL use explicit mapping methods to convert between Messages types and Persistence types. Mapping methods MUST be defined as extension methods.

#### Scenario: HistoryWorker maps to persistence
- **WHEN** `HistoryWorker` persists a `RecordHistory` command
- **THEN** it converts Messages types to Persistence types via `ToPersistence()` before calling `Persist()`

#### Scenario: HistoryWorker maps from persistence on recovery
- **WHEN** `HistoryWorker` recovers events from the journal
- **THEN** it converts Persistence types to domain types via `FromPersistence()` in the recovery handler

### Requirement: Messages enums have no serialization attributes
Messages enums SHALL NOT carry `[JsonStringEnumConverter]`, `[JsonStringEnumMemberName]`, or any other serialization-framework-specific attributes. Serialization is the concern of the layer that serializes (Api for System.Text.Json, Persistence for Newtonsoft.Json).

#### Scenario: Messages MediaType has no Json attributes
- **WHEN** `FunkArr.Messages.MediaType` is inspected
- **THEN** it has no `[JsonConverter]` or `[JsonStringEnumMemberName]` attributes
