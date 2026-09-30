## ADDED Requirements

### Requirement: Generate preview endpoint
The system SHALL provide `POST /api/v1/generate/preview` that searches for a show/movie in TVDB/TMDB, queries the Mediathek, runs RuleSetGenerator, and returns a preview with generated rules and test traces. This endpoint requires API keys.

#### Scenario: TV show preview
- **WHEN** a request arrives with `{ "type": "show", "query": "Feuer & Flamme" }`
- **THEN** the endpoint SHALL search TVDB for the show, query Mediathek by topic, generate rules with episode validation, and return `{ showInfo, generatedRules, testTraces, confidence }`

#### Scenario: Movie preview
- **WHEN** a request arrives with `{ "type": "movie", "query": "Das Boot" }`
- **THEN** the endpoint SHALL search TMDB for the movie, query Mediathek by title, generate movie rules, and return `{ movieInfo, generatedRules, testTraces, confidence }`

#### Scenario: API keys not configured
- **WHEN** a preview request arrives and TVDB/TMDB keys are not configured
- **THEN** the endpoint SHALL return HTTP 400 with `{ "error": "API keys required for auto-generation. Configure TVDB and TMDB keys in settings." }`

#### Scenario: Show not found in TVDB
- **WHEN** the TVDB search returns no results for the query
- **THEN** the endpoint SHALL return HTTP 404 with `{ "error": "Show not found in TVDB" }`

#### Scenario: No Mediathek items found
- **WHEN** the Mediathek query returns no items for the resolved topic
- **THEN** the endpoint SHALL return `{ showInfo, generatedRules: null, testTraces: [], confidence: 0 }` with a message indicating no items were found

### Requirement: Generate apply endpoint
The system SHALL provide `POST /api/v1/generate/apply` that saves a generated (or edited) ruleset to the corresponding ShowActor/MovieActor.

#### Scenario: Apply TV show ruleset
- **WHEN** a request arrives with `{ "tvdbId": 329324, "ruleSet": {...} }`
- **THEN** the endpoint SHALL send `ApplyLocalOverride(ruleSet)` to `ShowActor(329324)` and return success

#### Scenario: Apply movie ruleset
- **WHEN** a request arrives with `{ "imdbId": "tt0082096", "ruleSet": {...} }`
- **THEN** the endpoint SHALL send `ApplyLocalOverride(ruleSet)` to `MovieActor("tt0082096")` and return success

### Requirement: TVDB search in preview
The preview endpoint SHALL support searching TVDB by name and returning multiple candidates for the UI to select from.

#### Scenario: Multiple TVDB results
- **WHEN** a preview request with `{ "type": "show", "query": "Tatort" }` returns multiple TVDB matches
- **THEN** the response SHALL include a `candidates` array with `{ tvdbId, name, year, overview }` for each match
