## ADDED Requirements

### Requirement: Export show ruleset as JSON
The system SHALL expose `GET /api/v1/rulesets/{tvdbId}/export` that returns the effective RuleSetFile as a downloadable JSON file.

#### Scenario: Export existing ruleset
- **WHEN** `GET /api/v1/rulesets/83214/export` is requested and ShowActor(83214) has effective rules
- **THEN** the response SHALL have `Content-Type: application/json`, `Content-Disposition: attachment; filename="{topic}.json"`, and the body SHALL be the serialized RuleSetFile

#### Scenario: Export nonexistent ruleset
- **WHEN** `GET /api/v1/rulesets/999999/export` is requested and no rules exist
- **THEN** the endpoint SHALL return HTTP 404

### Requirement: Export movie ruleset as JSON
The system SHALL expose `GET /api/v1/rulesets/movies/{imdbId}/export` with the same behavior for movie rulesets.

#### Scenario: Export movie ruleset
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/export` is requested and MovieActor("tt0082096") has effective rules
- **THEN** the response SHALL return the RuleSetFile as a downloadable JSON with the movie name as filename

### Requirement: UI export button
The RulesetDetail.vue page SHALL display an export button that triggers a JSON file download of the current ruleset.

#### Scenario: Export button triggers download
- **WHEN** the user clicks the export button on the ruleset detail page
- **THEN** the browser SHALL download the ruleset as a `.json` file

#### Scenario: Export button hidden when no ruleset
- **WHEN** the detail page shows a 404 state (no ruleset exists)
- **THEN** the export button SHALL NOT be visible
