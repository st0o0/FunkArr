## MODIFIED Requirements

### Requirement: SourceType enum
The API SHALL define a `SourceType` enum with `Unknown = 0` as first member, followed by `Community = 1`, `Local = 2`, `Merged = 3`. `RuleSetListEntry.SourceType` SHALL use this enum.

#### Scenario: SourceType Unknown is first member
- **WHEN** the `SourceType` enum is inspected
- **THEN** `Unknown` SHALL be the first member with value `0`
- **AND** `Community = 1`, `Local = 2`, `Merged = 3`

#### Scenario: Community ruleset source type
- **WHEN** the API returns a community-only ruleset
- **THEN** the JSON field `sourceType` SHALL be `"community"`

### Requirement: QueueStatus enum
The API SHALL define a `QueueStatus` enum with `Unknown = 0` as first member, followed by `Processing = 1` and `Queued = 2`.

#### Scenario: QueueStatus Unknown is first member
- **WHEN** the `QueueStatus` enum is inspected
- **THEN** `Unknown = 0`, `Processing = 1`, `Queued = 2`

### Requirement: HistoryStatus enum
The API SHALL define a `HistoryStatus` enum with `Unknown = 0` as first member, followed by `Completed = 1` and `Failed = 2`.

#### Scenario: HistoryStatus Unknown is first member
- **WHEN** the `HistoryStatus` enum is inspected
- **THEN** `Unknown = 0`, `Completed = 1`, `Failed = 2`

### Requirement: CheckStatus enum
The API SHALL define a `CheckStatus` enum with `Unknown = 0` as first member, followed by `Ok = 1`, `Warn = 2`, and `Fail = 3`. The factory methods `CheckResult.Ok()`, `.Warn()`, `.Fail()` SHALL use the enum.

#### Scenario: CheckStatus Unknown is first member
- **WHEN** the `CheckStatus` enum is inspected
- **THEN** `Unknown = 0`, `Ok = 1`, `Warn = 2`, `Fail = 3`

### Requirement: FilterOperator enum
The API SHALL define a `FilterOperator` enum with `Unknown = 0` as first member, followed by `All = 1`, `Any = 2`, `Not = 3`.

#### Scenario: FilterOperator Unknown is first member
- **WHEN** the `FilterOperator` enum is inspected
- **THEN** `Unknown = 0`, `All = 1`, `Any = 2`, `Not = 3`

## ADDED Requirements

### Requirement: All Api enums have Unknown = 0 and explicit int values
Every enum defined in `FunkArr.Api` SHALL have `Unknown = 0` as its first member and every member SHALL have an explicit int value. Exception: `QueuePriority` is exempt from `Unknown` due to semantic negative values (`Low = -1, Normal = 0, High = 1`).

#### Scenario: Api enum with Unknown
- **WHEN** any non-exempt enum in `FunkArr.Api` is inspected
- **THEN** its first member SHALL be `Unknown = 0`
- **AND** all subsequent members SHALL have explicit int values

#### Scenario: QueuePriority exemption
- **WHEN** `QueuePriority` in `FunkArr.Api` is inspected
- **THEN** it SHALL NOT have an `Unknown` member
- **AND** it SHALL have explicit values: `Low = -1, Normal = 0, High = 1`

#### Scenario: Remaining Api enums (MediathekVariant, VariantPolicy, MediaType, etc.)
- **WHEN** `MediathekVariant`, `VariantPolicy`, `MediaType`, `MatchMethod`, `RuleOutcome`, `SearchSource`, `TitlePartType`, `FilterField`, `FilterOp`, `EnrichmentMethod`, `RuntimeMode`, `IdentificationStrategy`, `QueuePhase` in `FunkArr.Api` are inspected
- **THEN** each SHALL have `Unknown = 0` as first member and explicit int values on all members
