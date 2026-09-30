## ADDED Requirements

### Requirement: ApiKeyStatus contract
The API contracts SHALL include an `ApiKeyStatus` type with `tvdb` and `tmdb` sub-objects each containing `configured` (bool) and `valid` (bool).

#### Scenario: ApiKeyStatus serialization
- **WHEN** an `ApiKeyStatus` with `tvdb: { configured: true, valid: true }` is serialized
- **THEN** the JSON SHALL contain `{ "tvdb": { "configured": true, "valid": true }, "tmdb": { ... } }`

### Requirement: GeneratePreviewRequest contract
The API contracts SHALL include a `GeneratePreviewRequest` type with `type` ("show" or "movie"), `query` (string), and optional `tvdbId`/`imdbId` for direct lookup.

#### Scenario: Preview request for show search
- **WHEN** a preview request is sent with `{ "type": "show", "query": "Tatort" }`
- **THEN** the backend SHALL search TVDB by name

#### Scenario: Preview request with known TVDB ID
- **WHEN** a preview request is sent with `{ "type": "show", "tvdbId": 83214 }`
- **THEN** the backend SHALL look up TVDB by ID directly

### Requirement: GeneratePreviewResponse contract
The API contracts SHALL include a `GeneratePreviewResponse` type with `candidates` (search results), `showInfo`/`movieInfo`, `generatedRules` (RuleSetFile or null), `testTraces`, and `confidence`.

#### Scenario: Preview response with generated rules
- **WHEN** generation succeeds with confidence 0.85
- **THEN** the response SHALL contain the generated `RuleSetFile`, test traces, and `confidence: 0.85`

### Requirement: RuleSetDetail contract
The `RuleSetDetail` contract SHALL include the full `RuleSetFile` fields plus match quality summary (`matchRate`, `matchedCount`, `unmatchedCount`, `searchCount`).

#### Scenario: Detail with match quality
- **WHEN** a ruleset detail is returned for a show with 92% match rate
- **THEN** the response SHALL include `matchRate: 0.92` alongside the full ruleset

## MODIFIED Requirements

### Requirement: RuleStrategy enum
The `RuleStrategy` enum in contracts SHALL include `movieTitleMatch` and `movieOriginalTitleMatch` values alongside the existing TV strategies.

#### Scenario: Movie strategy in contract
- **WHEN** a ruleset with `movieTitleMatch` strategy is serialized
- **THEN** the JSON SHALL contain `"strategy": "movieTitleMatch"`
