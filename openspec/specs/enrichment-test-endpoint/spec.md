# enrichment-test-endpoint

## Purpose

Extension of the test scoring endpoint to accept enrichment config and identity fields, running enrichment after scoring for matched items via the EnrichmentManager actor.

## Requirements

### Requirement: Test endpoint accepts enrichment config
The `POST /api/rulesets/test` endpoint SHALL accept optional enrichment config and identity fields (tvdbId, tmdbId, imdbId, mediaType) in the request body alongside the existing scoring config and candidates.

#### Scenario: Request without enrichment
- **WHEN** a test request is sent without enrichment config
- **THEN** scoring runs as before and no enrichment is performed
- **THEN** ItemTrace results have null EnrichmentTrace

#### Scenario: Request with enrichment config and identity
- **WHEN** a test request includes enrichment config and tvdbId
- **THEN** scoring runs first, then enrichment runs for matched items
- **THEN** ItemTrace results for matched items include EnrichmentTrace data

### Requirement: Enrichment runs after scoring for matched items
When enrichment config is provided and external IDs are present, the test endpoint SHALL run enrichment through the EnrichmentManager actor for all items that matched a scoring rule.

#### Scenario: Episode enrichment via TVDB
- **WHEN** mediaType is "show" and tvdbId is provided and enrichment is enabled
- **THEN** matched items are sent to EnrichmentManager as EnrichEpisodes with the provided enrichment config
- **THEN** enrichment results (season, episode, episodeName, confidence, method) are merged into ItemTraces

#### Scenario: Movie enrichment via TMDB
- **WHEN** mediaType is "movie" and tmdbId or imdbId is provided and enrichment is enabled
- **THEN** matched items are sent to EnrichmentManager as EnrichMovies with the provided enrichment config

#### Scenario: No external IDs provided
- **WHEN** enrichment config is provided but no tvdbId/tmdbId/imdbId is set
- **THEN** enrichment is skipped and ItemTrace results have null EnrichmentTrace

#### Scenario: Enrichment disabled in config
- **WHEN** enrichment config is provided with enabled=false
- **THEN** enrichment is skipped

### Requirement: Enrichment timeout
The enrichment step SHALL respect a timeout. If enrichment times out, the endpoint SHALL return scoring results without enrichment traces rather than failing the entire request.

#### Scenario: Enrichment timeout
- **WHEN** enrichment takes longer than the timeout
- **THEN** the response is returned with scoring results only
- **THEN** ItemTrace results have null EnrichmentTrace
