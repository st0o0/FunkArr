## MODIFIED Requirements

### Requirement: Generate apply endpoint
The system SHALL provide `POST /api/v1/generate/apply` that saves a generated (or edited) ruleset to the corresponding ShowActor/MovieActor. The request SHALL accept `TmdbId` in addition to `TvdbId` and `ImdbId`, and SHALL accept an explicit `Type` field (`"show"` or `"movie"`) to determine the media type. If `Type` is omitted, the system SHALL infer: `TvdbId` present → `"show"`, `ImdbId` or `TmdbId` present → `"movie"`.

#### Scenario: Apply TV show ruleset
- **WHEN** a request arrives with `{ "type": "show", "tvdbId": 329324, "ruleSet": {...} }`
- **THEN** the endpoint SHALL send `SaveLocal("329324", "show", ruleSet)` to the RuleSetRegistryActor and return success

#### Scenario: Apply movie ruleset by TMDB ID
- **WHEN** a request arrives with `{ "type": "movie", "tmdbId": 12345, "ruleSet": {...} }`
- **THEN** the endpoint SHALL send `SaveLocal("12345", "movie", ruleSet)` to the RuleSetRegistryActor and return success

#### Scenario: Apply movie ruleset by IMDB ID
- **WHEN** a request arrives with `{ "type": "movie", "imdbId": "tt0082096", "ruleSet": {...} }`
- **THEN** the endpoint SHALL send `SaveLocal("tt0082096", "movie", ruleSet)` to the RuleSetRegistryActor and return success

#### Scenario: Type inference from TvdbId
- **WHEN** a request arrives with `{ "tvdbId": 329324, "ruleSet": {...} }` and no `type` field
- **THEN** the endpoint SHALL infer type as `"show"` and process accordingly

#### Scenario: Type inference from TmdbId
- **WHEN** a request arrives with `{ "tmdbId": 12345, "ruleSet": {...} }` and no `type` field
- **THEN** the endpoint SHALL infer type as `"movie"` and process accordingly

#### Scenario: Missing ruleset body
- **WHEN** a request arrives without a `ruleSet` in the body
- **THEN** the endpoint SHALL return HTTP 400 with `{ "error": "Provide either tvdbId or imdbId/tmdbId with a ruleSet" }`

#### Scenario: Registry timeout
- **WHEN** the RuleSetRegistryActor does not respond within 5 seconds
- **THEN** the endpoint SHALL return HTTP 503 with `{ "error": "Registry did not respond in time" }`
