## MODIFIED Requirements

### Requirement: Save show ruleset override
The `PUT /api/v1/rulesets/{tvdbId}` endpoint SHALL send `SaveLocal` to the RuleSetRegistryActor using Ask and await confirmation before responding.

#### Scenario: Save override
- **WHEN** `PUT /api/v1/rulesets/329324` is requested with a `RuleSetFile` body
- **THEN** the endpoint SHALL send `SaveLocal(329324, "show", ruleSet)` to the RuleSetRegistryActor, await the persist confirmation, and return HTTP 200

#### Scenario: Save timeout
- **WHEN** `PUT /api/v1/rulesets/329324` is requested and the registry does not respond within 5 seconds
- **THEN** the endpoint SHALL return HTTP 503

### Requirement: Delete show ruleset override
The `DELETE /api/v1/rulesets/{tvdbId}` endpoint SHALL send `RemoveLocal` to the RuleSetRegistryActor using Ask and await confirmation before responding.

#### Scenario: Delete override
- **WHEN** `DELETE /api/v1/rulesets/329324` is requested
- **THEN** the endpoint SHALL send `RemoveLocal(329324)` to the RuleSetRegistryActor, await the persist confirmation, and return HTTP 200

### Requirement: Save movie ruleset override
The `PUT /api/v1/rulesets/movies/{imdbId}` endpoint SHALL send `SaveLocal` to the RuleSetRegistryActor using Ask and await confirmation.

#### Scenario: Save movie override
- **WHEN** `PUT /api/v1/rulesets/movies/tt0082096` is requested with a `RuleSetFile` body
- **THEN** the endpoint SHALL send `SaveLocal("tt0082096", "movie", ruleSet)` to the RuleSetRegistryActor, await confirmation, and return HTTP 200

### Requirement: Generate-apply registers with registry
The `POST /api/v1/generate/apply` endpoint SHALL send `SaveLocal` to the RuleSetRegistryActor in addition to applying the override to the media actor.

#### Scenario: Generate apply registers local
- **WHEN** `POST /api/v1/generate/apply` is called with a generated ruleset for TVDB ID 83214
- **THEN** the endpoint SHALL send `SaveLocal(83214, "show", ruleSet)` to the RuleSetRegistryActor

### Requirement: List all rulesets
The `GET /api/v1/rulesets` endpoint SHALL return metadata for all known show rulesets from the RuleSetRegistryActor catalog, including both community and local entries.

#### Scenario: List with community rulesets
- **WHEN** a `GET /api/v1/rulesets` request arrives
- **THEN** the endpoint SHALL return an array of `RulesetSummary` with topic, source, ruleCount, media, aliases, and matchRate for each known show ruleset

#### Scenario: List includes local overrides
- **WHEN** a local override exists for TVDB ID 83214
- **THEN** the catalog SHALL include it with `source = "local"`

### Requirement: API key authentication
All ruleset API endpoints SHALL require a valid `apikey` query parameter. Authentication SHALL be handled by the centralized `ApiKeyMiddleware`.

#### Scenario: Valid API key
- **WHEN** a request includes a valid apikey
- **THEN** the request SHALL be processed

#### Scenario: Missing API key
- **WHEN** a request has no apikey parameter
- **THEN** the `ApiKeyMiddleware` SHALL return 401
