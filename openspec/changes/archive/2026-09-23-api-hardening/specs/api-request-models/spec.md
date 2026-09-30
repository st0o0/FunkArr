## MODIFIED Requirements

### Requirement: MediathekSearchRequest typed request record
`MediathekSearchRequest` SHALL be a sealed record with `[FromQuery]` attributes on all properties. The `MediathekApiEndpoints` search endpoint SHALL bind this record via `[AsParameters]` instead of individual lambda parameters. The record SHALL include `[Range]` attributes: `Limit` range 1–100, `Offset` minimum 0, `DurationMin` minimum 0, `DurationMax` minimum 0.

#### Scenario: Search endpoint binds via AsParameters
- **WHEN** a GET request to `/api/mediathek/search?q=test&limit=10` is received
- **THEN** the endpoint handler receives a bound `MediathekSearchRequest` instance with `Q = "test"` and `Limit = 10`

#### Scenario: Out of range limit rejected
- **WHEN** a GET request includes `limit=-5`
- **THEN** the API returns 400 with a validation error on Limit

### Requirement: DownloadHistoryRequest typed request record
`DownloadHistoryRequest` SHALL be a sealed record with `[FromQuery]` attributes on all properties. The `DownloadsApiEndpoints` history endpoint SHALL bind this record via `[AsParameters]` instead of individual lambda parameters. The record SHALL include `[Range]` attributes: `Start` minimum 0, `Limit` range 1–1000.

#### Scenario: History endpoint binds via AsParameters
- **WHEN** a GET request to `/api/downloads/history?start=0&limit=50` is received
- **THEN** the endpoint handler receives a bound `DownloadHistoryRequest` instance

### Requirement: CreateRuleSetRequest typed model
`CreateRuleSetRequest` SHALL be a sealed record implementing `IRuleSetBody`. The record SHALL include DataAnnotation attributes: `[Required]` on `RuleSetId`, `Topic`; `[RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$")]` on `RuleSetId`; `[Required]` and `[MinLength(1)]` on `Rules`. `Confidence` SHALL have `[Range(0.0, 1.0)]` when provided.

#### Scenario: Missing required field
- **WHEN** a POST request omits `Topic`
- **THEN** the API returns 400 before the handler executes

#### Scenario: Invalid RuleSetId format
- **WHEN** a POST request includes `RuleSetId = "Not Kebab"`
- **THEN** the API returns 400 with a regex validation error

### Requirement: TestScoreRequest uses shared RuleInput
`TestScoreRequest` SHALL be a sealed record. `DefaultConfidence` SHALL have `[Range(0.0, 1.0)]`. `Rules` SHALL have `[Required]` and `[MinLength(1)]`. `Candidates` SHALL have `[Required]` and `[MinLength(1)]`.

#### Scenario: Empty candidates array
- **WHEN** a POST request to `/api/rulesets/test` includes an empty `Candidates` array
- **THEN** the API returns 400

### Requirement: CreateArrResourceRequest validated
`CreateArrResourceRequest` SHALL include `[Required]` on `Url` and `ApiKey`, and `[SafeUrl]` on `Url` to prevent SSRF.

#### Scenario: Missing API key in setup request
- **WHEN** a POST request to `/api/setup/sonarr/indexer` omits `ApiKey`
- **THEN** the API returns 400

#### Scenario: SSRF attempt blocked
- **WHEN** a POST request includes `Url = "http://169.254.169.254/metadata"`
- **THEN** the API returns 400 with SafeUrl validation error
