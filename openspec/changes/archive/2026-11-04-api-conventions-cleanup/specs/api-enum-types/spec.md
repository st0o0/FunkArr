## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: All API enums serialize as int
All API-layer enums SHALL serialize as their camelCase string representation using `JsonStringEnumConverter` configured on `JsonSerializerOptions`. No enum SHALL serialize as an integer in API responses.

#### Scenario: Enum fields serialize as camelCase strings
- **WHEN** the API serializes a response containing enum fields
- **THEN** all enum values SHALL appear as camelCase strings in the JSON output (e.g., `"processing"`, `"show"`, `"matched"`)

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
