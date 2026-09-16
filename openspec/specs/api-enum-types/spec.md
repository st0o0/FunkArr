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
The API SHALL define a `MediaType` enum with members `Show` (0) and `Movie` (1). `RuleSetListEntry.MediaType` SHALL use this enum instead of a string.

#### Scenario: Show media type
- **WHEN** the API returns a ruleset with media type show
- **THEN** the JSON field `mediaType` SHALL be `0`

### Requirement: CheckStatus enum
The API SHALL define a `CheckStatus` enum with members `Ok` (0), `Warn` (1), and `Fail` (2). `CheckResult.Status` SHALL use this enum instead of a string. The factory methods `CheckResult.Ok()`, `.Warn()`, `.Fail()` SHALL use the enum.

#### Scenario: Health check status is int
- **WHEN** the API returns a health check with status ok
- **THEN** the JSON field `status` SHALL be `0`

### Requirement: All API enums serialize as int
All API-layer enums SHALL serialize as their integer values using the System.Text.Json default behavior. No `JsonStringEnumConverter` SHALL be applied to API model enums.

#### Scenario: No string enum converter on API models
- **WHEN** the API serializes a response containing enum fields
- **THEN** all enum values appear as integers in the JSON output
