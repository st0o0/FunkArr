## Purpose

REST API for listing, reading, saving, deleting, testing, and reloading rulesets via the RuleSetRegistryActor.

## Requirements

### Requirement: List all rulesets
The system SHALL expose `GET /api/rulesets` returning all registered topics with metadata: topic name, source (community/generated/local), rule count, media reference, and match stats from the MatchLedger.

#### Scenario: List with stats
- **WHEN** a client sends `GET /api/rulesets?apikey=<valid>`
- **THEN** the response SHALL include all topics with their source, rule count, media name, TVDB ID, and match rate (if available from the ledger)

#### Scenario: No rulesets loaded
- **WHEN** no rulesets are loaded
- **THEN** the response SHALL return an empty array

### Requirement: Get single ruleset
The system SHALL expose `GET /api/rulesets/:topic` returning the full RuleSetFile JSON for a single topic, including resolved rules (after merge if applicable).

#### Scenario: Topic exists
- **WHEN** a client requests `/api/rulesets/tatort`
- **THEN** the response SHALL return the full ruleset with topic, media, source, rules, and aliases

#### Scenario: Topic not found
- **WHEN** a client requests `/api/rulesets/nonexistent`
- **THEN** the response SHALL return 404

### Requirement: Save local override
The system SHALL expose `PUT /api/rulesets/:topic` accepting a RuleSetFile JSON body and saving it to `data/rulesets/local/` via the RuleSetRegistryActor.

#### Scenario: Save new local ruleset
- **WHEN** a client sends `PUT /api/rulesets/heute-show` with a valid ruleset body
- **THEN** the system SHALL write the file to `data/rulesets/local/heute-show.json` and reload the registry

#### Scenario: Overwrite existing local ruleset
- **WHEN** a client sends `PUT /api/rulesets/tatort` for a topic with an existing local override
- **THEN** the system SHALL overwrite the local file and reload

### Requirement: Delete local override
The system SHALL expose `DELETE /api/rulesets/:topic` removing the local override file and reloading the registry to fall back to community/generated.

#### Scenario: Delete existing override
- **WHEN** a client sends `DELETE /api/rulesets/tatort` and a local override exists
- **THEN** the system SHALL delete the local file and reload, falling back to community/generated

#### Scenario: Delete non-existent override
- **WHEN** a client sends `DELETE /api/rulesets/tatort` and no local override exists
- **THEN** the response SHALL return 404

### Requirement: Test rules against Mediathek
The system SHALL expose `POST /api/rulesets/test` accepting a topic, optional TVDB ID, and array of rules. It SHALL search the Mediathek for the topic, fetch TVDB episodes if a TVDB ID is provided, run `RuleSetMatchingEngine.EvaluateRulesWithTraces`, and return the trace results.

#### Scenario: Test with matches
- **WHEN** a client sends a test request for topic "Tatort" with TVDB ID and valid rules
- **THEN** the response SHALL include arrays of matched, filtered, and unmatched traces with full detail

#### Scenario: Test with no TVDB ID
- **WHEN** a client sends a test request without a TVDB ID
- **THEN** the system SHALL run matching without TVDB episode data (title-only strategies still work)

#### Scenario: Test with empty Mediathek results
- **WHEN** the Mediathek returns no results for the topic
- **THEN** the response SHALL include an empty result set with a message indicating no items found

### Requirement: Reload rulesets
The system SHALL expose `POST /api/rulesets/reload` triggering the RuleSetRegistryActor to reload all rulesets from disk.

#### Scenario: Reload success
- **WHEN** a client sends `POST /api/rulesets/reload`
- **THEN** the system SHALL send a ReloadLocal message to the registry actor and return success

### Requirement: API key authentication
All ruleset API endpoints SHALL require a valid `apikey` query parameter, consistent with existing API authentication.

#### Scenario: Valid API key
- **WHEN** a request includes a valid apikey
- **THEN** the request SHALL be processed

#### Scenario: Missing API key
- **WHEN** a request has no apikey parameter
- **THEN** the response SHALL be 401
