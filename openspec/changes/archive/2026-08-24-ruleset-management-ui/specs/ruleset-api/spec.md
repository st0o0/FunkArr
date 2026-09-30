## MODIFIED Requirements

### Requirement: List all rulesets
The `GET /api/v1/rulesets` endpoint SHALL return metadata for all known show rulesets from the community catalog (via RuleSetRegistryActor) augmented with match quality from active ShowActors.

#### Scenario: List with community rulesets
- **WHEN** a `GET /api/v1/rulesets` request arrives
- **THEN** the endpoint SHALL return an array of `RulesetSummary` with topic, source, ruleCount, media, aliases, and matchRate for each known show ruleset

#### Scenario: List includes generated rulesets
- **WHEN** ShowActors have generated rulesets not in the community catalog
- **THEN** they SHALL appear in the list with source="generated"

### Requirement: Get show ruleset by TVDB ID
The `GET /api/v1/rulesets/{tvdbId}` endpoint SHALL return the full ruleset from the corresponding ShowActor.

#### Scenario: Ruleset exists
- **WHEN** `GET /api/v1/rulesets/329324` is requested and ShowActor(329324) has rules
- **THEN** the endpoint SHALL return the full `RuleSetFile` with match quality stats

#### Scenario: Ruleset not found
- **WHEN** `GET /api/v1/rulesets/999999` is requested and no rules exist
- **THEN** the endpoint SHALL return HTTP 404

### Requirement: Save show ruleset override
The `PUT /api/v1/rulesets/{tvdbId}` endpoint SHALL send `ApplyLocalOverride` to the ShowActor.

#### Scenario: Save override
- **WHEN** `PUT /api/v1/rulesets/329324` is requested with a `RuleSetFile` body
- **THEN** the endpoint SHALL send `ApplyLocalOverride(ruleSet)` to `ShowActor(329324)` and return success

### Requirement: Delete show ruleset override
The `DELETE /api/v1/rulesets/{tvdbId}` endpoint SHALL send `RemoveLocalOverride` to the ShowActor.

#### Scenario: Delete override
- **WHEN** `DELETE /api/v1/rulesets/329324` is requested
- **THEN** the endpoint SHALL send `RemoveLocalOverride` to `ShowActor(329324)` and return success

### Requirement: Test show rules
The `POST /api/v1/rulesets/{tvdbId}/test` endpoint SHALL trigger a test evaluation against live Mediathek data.

#### Scenario: Test rules
- **WHEN** `POST /api/v1/rulesets/329324/test` is requested
- **THEN** the endpoint SHALL send `TestRules` to `ShowActor(329324)` and return match/filter/unmatched traces

### Requirement: Movie ruleset endpoints
The system SHALL provide equivalent endpoints for movie rulesets under `/api/v1/rulesets/movies/`.

#### Scenario: List movie rulesets
- **WHEN** `GET /api/v1/rulesets/movies` is requested
- **THEN** the endpoint SHALL return all known movie rulesets from the community catalog and active MovieActors

#### Scenario: Get movie ruleset
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096` is requested
- **THEN** the endpoint SHALL return the ruleset from `MovieActor("tt0082096")`

#### Scenario: Save movie ruleset override
- **WHEN** `PUT /api/v1/rulesets/movies/tt0082096` is requested with a `RuleSetFile` body
- **THEN** the endpoint SHALL send `ApplyLocalOverride` to `MovieActor("tt0082096")`

#### Scenario: Test movie rules
- **WHEN** `POST /api/v1/rulesets/movies/tt0082096/test` is requested
- **THEN** the endpoint SHALL send `TestRules` to `MovieActor("tt0082096")` and return traces
