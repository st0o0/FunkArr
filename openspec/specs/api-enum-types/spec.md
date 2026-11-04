## Purpose

Proper int-serialized enum types for all API model fields, replacing string-typed fields with type-safe enums.

## Requirements

### Requirement: QueueStatus enum
The API SHALL define a `QueueStatus` enum with members `Processing` (0) and `Queued` (1). `DownloadQueueItem.Status` SHALL use this enum instead of a string.

#### Scenario: Queue item status is int
- **WHEN** the API returns a download queue item with status Processing
- **THEN** the JSON field `status` SHALL be `0`

### Requirement: HistoryStatus enum
The API SHALL define a `HistoryStatus` enum with members `Completed` (0) and `Failed` (1). `DownloadHistoryItem.Status` SHALL use this enum instead of a string.

#### Scenario: History item status is int
- **WHEN** the API returns a completed history item
- **THEN** the JSON field `status` SHALL be `0`

### Requirement: SourceType enum
The API SHALL define a `SourceType` enum with members `Community` (0), `Local` (1), and `Merged` (2). `RuleSetListEntry.SourceType` SHALL use this enum instead of a string.

#### Scenario: Community ruleset source type
- **WHEN** the API returns a community-only ruleset
- **THEN** the JSON field `sourceType` SHALL be `0`

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
The API SHALL define a `CheckStatus` enum with members `Ok` (0), `Warn` (1), and `Fail` (2). `CheckResult.Status` SHALL use this enum instead of a string. The factory methods `CheckResult.Ok()`, `.Warn()`, `.Fail()` SHALL use the enum.

#### Scenario: Health check status is int
- **WHEN** the API returns a health check with status ok
- **THEN** the JSON field `status` SHALL be `0`

### Requirement: All API enums serialize as int
All API-layer enums SHALL serialize as their camelCase string representation using `JsonStringEnumConverter` configured on `JsonSerializerOptions`. No enum SHALL serialize as an integer in API responses.

#### Scenario: Enum fields serialize as camelCase strings
- **WHEN** the API serializes a response containing enum fields
- **THEN** all enum values SHALL appear as camelCase strings in the JSON output (e.g., `"processing"`, `"show"`, `"matched"`)

### Requirement: FilterOperator enum
The API SHALL define a `FilterOperator` enum with members `All`, `Any`, `Not`. `FilterGroupTrace.Operator` SHALL use this enum instead of a string.

#### Scenario: Filter group trace uses enum
- **WHEN** the API returns a scoring trace with a filter group
- **THEN** the `operator` field SHALL be the enum value, not a `.ToString()` string

### Requirement: DownloadTimeSlotResponse uses TimeOnly
`DownloadTimeSlotResponse.Start` and `DownloadTimeSlotResponse.End` SHALL be `TimeOnly` instead of `string`. System.Text.Json SHALL serialize `TimeOnly` natively.

#### Scenario: Time slot serialization
- **WHEN** the API returns download settings with schedule time slots
- **THEN** `start` and `end` SHALL be serialized as time strings by System.Text.Json's `TimeOnly` support
