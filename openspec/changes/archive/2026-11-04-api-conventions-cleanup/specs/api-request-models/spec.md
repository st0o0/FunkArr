## MODIFIED Requirements

### Requirement: TestScoreRequest uses shared RuleInput
`TestScoreRequest` SHALL be a sealed record. `DefaultConfidence` SHALL have `[Range(0.0, 1.0)]`. `Rules` SHALL have `[Required]` and `[MinLength(1)]`. `Candidates` SHALL have `[Required]` and `[MinLength(1)]`. `MediaType` SHALL be `MediaType?` (enum), not `string?`.

#### Scenario: TestScore reuses RuleInput
- **WHEN** a test score request is received
- **THEN** the rules array deserializes using the same `RuleInput` type as Create/Update

#### Scenario: Empty candidates array
- **WHEN** a POST request to `/api/rulesets/test` includes an empty `Candidates` array
- **THEN** the API returns 400

#### Scenario: MediaType is typed enum
- **WHEN** a test score request includes `"mediaType": "show"`
- **THEN** `TestScoreRequest.MediaType` SHALL be `MediaType.Show`, not the string `"show"`

### Requirement: DownloadHistoryRequest typed request record
`DownloadHistoryRequest` SHALL be a sealed record with `[FromQuery]` attributes on all properties. The `DownloadsApiEndpoints` history endpoint SHALL bind this record via `[AsParameters]` instead of individual lambda parameters. The record SHALL include `[Range]` attributes: `Start` minimum 0, `Limit` range 1–1000. `Category` SHALL be `MediaType?` (enum), not `string?`.

#### Scenario: History endpoint binds via AsParameters
- **WHEN** a GET request to `/api/downloads/history?start=0&limit=50` is received
- **THEN** the endpoint handler receives a bound `DownloadHistoryRequest` instance

#### Scenario: Default values applied in handler
- **WHEN** Start and Limit are null
- **THEN** the handler SHALL apply defaults (Start=0, Limit=25) - defaulting stays in handler code

#### Scenario: Category binds as enum
- **WHEN** a GET request includes `category=movie`
- **THEN** `DownloadHistoryRequest.Category` SHALL be `MediaType.Movie`

### Requirement: MediaInput uses MediaType enum
`MediaInput.Type` SHALL be `MediaType` (enum), not `string`. ASP.NET JSON deserialization SHALL handle the conversion from the camelCase string in the JSON body.

#### Scenario: MediaInput type is enum
- **WHEN** a create ruleset request includes `"media": {"name": "...", "type": "show"}`
- **THEN** `MediaInput.Type` SHALL be `MediaType.Show`

#### Scenario: Invalid media type in request body
- **WHEN** a create request includes `"type": "podcast"`
- **THEN** the API SHALL return 400 (JSON deserialization failure for unknown enum value)

## REMOVED Requirements

### Requirement: RuleSetApiEndpoints list handler extracted
**Reason**: Replaced by the inline-lambda-only convention. All endpoint handlers are inline lambdas. The list handler's fan-out logic (parallel Ask to 3 actors) stays inline — it's endpoint plumbing (resolve, ask, map), not domain logic.
**Migration**: Inline the list handler body directly in the `MapGet("/", ...)` lambda.

### Requirement: Single SerializeForDisk method
**Reason**: The `SerializeForDisk` pattern was an interim step during the typed-models migration. Serialization for disk storage is now handled by the `ToCommand()` mapping extension which produces the domain message; the actor handles persistence. No endpoint-level serialization helper is needed.
**Migration**: Remove `SerializeForDisk` if it still exists; Create/Update flows use `request.ToCommand()` → actor → persistence.
