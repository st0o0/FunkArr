## Purpose

Proper int-serialized enum types for all API model fields, replacing string-typed fields with type-safe enums.

## Requirements

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

### Requirement: SourceType enum
The API SHALL define a `SourceType` enum with `Unknown = 0` as first member, followed by `Community = 1`, `Local = 2`, `Merged = 3`. `RuleSetListEntry.SourceType` SHALL use this enum.

#### Scenario: SourceType Unknown is first member
- **WHEN** the `SourceType` enum is inspected
- **THEN** `Unknown` SHALL be the first member with value `0`
- **AND** `Community = 1`, `Local = 2`, `Merged = 3`

#### Scenario: Community ruleset source type
- **WHEN** the API returns a community-only ruleset
- **THEN** the JSON field `sourceType` SHALL be `"community"`

### Requirement: MediaType enum
The API SHALL use the `MediaType` enum from `FunkArr.Api.Models.Shared` for all request and response models. `TestScoreRequest.MediaType`, `MediaInput.Type`, `DownloadHistoryRequest.Category`, and the feed GET `type` query parameter SHALL all use this enum type instead of `string`. ASP.NET Minimal API enum binding SHALL handle deserialization automatically for both query parameters and JSON body fields.

#### Scenario: Show media type
- **WHEN** the API returns a ruleset with media type show
- **THEN** the JSON field `mediaType` SHALL be `"show"`

#### Scenario: MediaType query parameter binding
- **WHEN** a GET request to `/api/downloads/history?category=show` is received
- **THEN** ASP.NET SHALL bind `category` to `MediaType.Show` automatically

#### Scenario: Invalid MediaType returns 400
- **WHEN** a GET request includes `category=invalid`
- **THEN** the API SHALL return 400 (ASP.NET enum binding failure)

#### Scenario: No ParseMediaType methods
- **WHEN** endpoint files are searched for `ParseMediaType`
- **THEN** no such method SHALL exist in any endpoint file

### Requirement: CheckStatus enum
The API SHALL define a `CheckStatus` enum with `Unknown = 0` as first member, followed by `Ok = 1`, `Warn = 2`, and `Fail = 3`. The factory methods `CheckResult.Ok()`, `.Warn()`, `.Fail()` SHALL use the enum.

#### Scenario: CheckStatus Unknown is first member
- **WHEN** the `CheckStatus` enum is inspected
- **THEN** `Unknown = 0`, `Ok = 1`, `Warn = 2`, `Fail = 3`

### Requirement: All API enums serialize as int
All API-layer enums SHALL serialize as their camelCase string representation using `JsonStringEnumConverter` configured on `JsonSerializerOptions`. No enum SHALL serialize as an integer in API responses.

#### Scenario: Enum fields serialize as camelCase strings
- **WHEN** the API serializes a response containing enum fields
- **THEN** all enum values SHALL appear as camelCase strings in the JSON output (e.g., `"processing"`, `"show"`, `"matched"`)

### Requirement: FilterOperator enum
The API SHALL define a `FilterOperator` enum with `Unknown = 0` as first member, followed by `All = 1`, `Any = 2`, `Not = 3`.

#### Scenario: FilterOperator Unknown is first member
- **WHEN** the `FilterOperator` enum is inspected
- **THEN** `Unknown = 0`, `All = 1`, `Any = 2`, `Not = 3`

### Requirement: DownloadTimeSlotResponse uses TimeOnly
`DownloadTimeSlotResponse.Start` and `DownloadTimeSlotResponse.End` SHALL be `TimeOnly` instead of `string`. System.Text.Json SHALL serialize `TimeOnly` natively.

#### Scenario: Time slot serialization
- **WHEN** the API returns download settings with schedule time slots
- **THEN** `start` and `end` SHALL be serialized as time strings by System.Text.Json's `TimeOnly` support

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
