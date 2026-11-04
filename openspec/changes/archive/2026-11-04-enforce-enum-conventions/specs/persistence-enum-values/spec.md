## MODIFIED Requirements

### Requirement: All existing persistence enums comply
Every member of every enum declared in `FunkArr.Persistence` SHALL have an explicit integer value. This includes `PersistedSearchSource` which previously had partial explicit values.

#### Scenario: All existing persistence enums comply
- **WHEN** the enums in `FunkArr.Persistence` are inspected
- **THEN** `PersistedMediaType`, `PersistedFailureKind`, `PersistedMatchMethod`, `PersistedMediathekVariant`, `PersistedRuleOutcome`, `PersistedFilterGroupOp`, `PersistedIdentificationStrategy`, `PersistedIdentificationFailureReason`, `PersistedDownloadStatus`, `PersistedDownloadPriority`, and `PersistedSearchSource` each assign every member an explicit value

### Requirement: Persisted enum values are frozen
Once a persistence enum member has an assigned value, that value SHALL NOT be changed, reused for a different member, or reordered. New members SHALL receive a new, previously unused value.

#### Scenario: Existing ordinals preserved
- **WHEN** explicit values are added to a previously implicit persistence enum
- **THEN** each member's value equals its former implicit ordinal (first member `0`, then ascending)

#### Scenario: Gaps are preserved
- **WHEN** `PersistedSearchSource` is inspected
- **THEN** `Sonarr` is `0`, `Radarr` is `1`, `Prowlarr` is `2`, and `Feed` is `4`

#### Scenario: Member added later
- **WHEN** a new member is added to a persistence enum
- **THEN** it uses a value greater than every existing value and does not alter existing members

#### Scenario: PersistedSearchSource fully explicit
- **WHEN** `PersistedSearchSource` is inspected
- **THEN** all members SHALL have explicit values: `Sonarr = 0`, `Radarr = 1`, `Prowlarr = 2`, `Feed = 4`
