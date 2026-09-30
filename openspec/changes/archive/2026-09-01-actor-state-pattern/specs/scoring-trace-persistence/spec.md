## REMOVED Requirements

### Requirement: Persistence DTOs for scoring trace are separate from Messages
**Reason**: Replaced by domain event records. Persistence DTOs (`ScoringRecordedDto`, `ItemTraceDto`, `RuleTraceDto`, `FilterGroupTraceDto`, `FilterNodeTraceDto`, `IdentificationTraceDto`, `TracedIdentificationDto`) are replaced by a single `ScoringRecordedEvent` domain record and its nested records, defined in `FunkArr.Persistence/Events/MatchHistory/`.
**Migration**: All references to `ScoringRecordedDto` and related DTOs are replaced by `ScoringRecordedEvent` and its constituent records. The custom `FunkArrJsonSerializer` handles serialization.

### Requirement: Persistence DTOs use stable JSON property names
**Reason**: No longer applicable. Domain event records use `System.Text.Json` source generation or explicit options in the custom serializer. JSON property stability is owned by the serializer, not by attributes on each type.
**Migration**: The `FunkArrJsonSerializer` ensures stable JSON property names via its serialization configuration.

### Requirement: Persistence DTOs have version tracking
**Reason**: Versioning moves from a `Version` property on DTOs to the serializer manifest string. The serializer uses manifests like `"scoring-recorded-v1"` to identify versions, enabling deserialization routing for future schema evolution.
**Migration**: Version tracking is handled by `FunkArrJsonSerializer.Manifest()` returning stable version-tagged strings.

### Requirement: JSON snapshot tests verify serialization stability
**Reason**: Golden-file tests for DTOs are replaced by serializer roundtrip tests. The custom serializer is the new stability boundary.
**Migration**: Tests SHALL verify that `FunkArrJsonSerializer` can serialize and deserialize all event and snapshot types correctly (roundtrip tests on the serializer, not golden files on DTOs).

### Requirement: Mapping between Messages and Persistence DTOs
**Reason**: Mapper classes (`ScoringRecordedMapper`) are eliminated. The `ProcessCommand` extension method on `MatchHistoryState` directly produces a `ScoringRecordedEvent` from a `RecordScoringResult` command. No intermediate mapping step.
**Migration**: Remove `ScoringRecordedMapper`. State extension methods handle the command → event conversion.
