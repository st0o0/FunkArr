## Purpose

REST API endpoints for creating, updating, and deleting local rulesets via JSON files on disk.

## Requirements

### Requirement: Create local ruleset endpoint
The system SHALL expose `POST /api/rulesets` that creates a new local ruleset JSON file. The request body SHALL be a JSON object with required fields `ruleSetId` (string, kebab-case), `topic` (string), and optional fields `aliases` (string[]), `media` (object with optional `tvdbId`, `imdbId`, `tmdbId`), `confidence` (float), and `rules` (array of rule objects). The endpoint SHALL validate that `ruleSetId` matches `^[a-z0-9]+(-[a-z0-9]+)*$`, that `topic` is non-empty, and that no local ruleset file already exists for the given `ruleSetId`. On success, the endpoint SHALL serialize the body to JSON and write it to `DataPaths.LocalRuleSets/{ruleSetId}.json` using `IDataFiles.WriteAtomic`. The file watcher on the local directory SHALL automatically trigger the RuleSetManager to load the new ruleset.

#### Scenario: Create new local ruleset
- **WHEN** `POST /api/rulesets` is called with `{ "ruleSetId": "my-show", "topic": "My Show", "confidence": 0.8, "rules": [] }`
- **THEN** the response is 201 with the ruleSetId in the body
- **AND** the file `DataPaths.LocalRuleSets/my-show.json` is created

#### Scenario: Create with full identity
- **WHEN** `POST /api/rulesets` is called with topic, aliases, and media IDs
- **THEN** the JSON file contains topic, aliases, and media block

#### Scenario: Create with rules
- **WHEN** `POST /api/rulesets` is called with a rules array containing a rule with strategy "seasonAndEpisodeNumber", seasonRegex, episodeRegex, priority, and filters
- **THEN** the JSON file contains the complete rules array with all fields preserved

#### Scenario: Duplicate ruleSetId
- **WHEN** `POST /api/rulesets` is called with a `ruleSetId` that already has a local file
- **THEN** the response is 409 Conflict

#### Scenario: Invalid ruleSetId format
- **WHEN** `POST /api/rulesets` is called with `ruleSetId` containing uppercase or special characters
- **THEN** the response is 400 with a validation error message

#### Scenario: Missing topic
- **WHEN** `POST /api/rulesets` is called without a `topic` field
- **THEN** the response is 400 with a validation error message

### Requirement: Update local ruleset endpoint
The system SHALL expose `PUT /api/rulesets/{id}` that updates an existing local ruleset JSON file. The request body SHALL have the same shape as the create endpoint (without `ruleSetId`). The endpoint SHALL write the updated JSON to `DataPaths.LocalRuleSets/{id}.json` using `IDataFiles.WriteAtomic`, overwriting any existing local file. If the ruleset has only a community file and no local file yet, this endpoint SHALL create the local file as an overlay. The file watcher SHALL automatically trigger reload.

#### Scenario: Update existing local ruleset
- **WHEN** `PUT /api/rulesets/my-show` is called with updated rules
- **THEN** the response is 200
- **AND** the file `DataPaths.LocalRuleSets/my-show.json` is overwritten with new content

#### Scenario: Create local overlay for community ruleset
- **WHEN** `PUT /api/rulesets/tatort` is called and only a community file exists for "tatort"
- **THEN** the response is 200
- **AND** a new local file `DataPaths.LocalRuleSets/tatort.json` is created
- **AND** the RuleSetManager merges community + local on next reload

#### Scenario: Update with standalone flag
- **WHEN** `PUT /api/rulesets/tatort` is called with `"standalone": true` in the body
- **THEN** the local file is written with `standalone: true`
- **AND** on reload the community base is ignored

#### Scenario: Update with disable list
- **WHEN** `PUT /api/rulesets/tatort` is called with `"disable": ["rule-1"]` in the body
- **THEN** the local file is written with the disable array
- **AND** on reload the community rule "rule-1" is excluded from merge

#### Scenario: Update unknown ruleset with no community file
- **WHEN** `PUT /api/rulesets/nonexistent` is called and no community or local file exists for "nonexistent"
- **THEN** the response is 404

### Requirement: Delete local ruleset endpoint
The system SHALL expose `DELETE /api/rulesets/{id}` that removes the local ruleset JSON file. The endpoint SHALL only delete the local file at `DataPaths.LocalRuleSets/{id}.json`. It SHALL NOT delete community files. If the ruleset has a community file, deleting the local file reverts the ruleset to community-only behavior. The file watcher SHALL automatically trigger reload or removal.

#### Scenario: Delete local-only ruleset
- **WHEN** `DELETE /api/rulesets/my-show` is called and only a local file exists
- **THEN** the response is 200
- **AND** the file is removed
- **AND** the RuleSetManager removes the ruleset on next flush

#### Scenario: Delete local overlay (community remains)
- **WHEN** `DELETE /api/rulesets/tatort` is called and both community and local files exist
- **THEN** the response is 200
- **AND** only the local file is removed
- **AND** the RuleSetManager reloads "tatort" from community-only on next flush

#### Scenario: Delete when no local file exists
- **WHEN** `DELETE /api/rulesets/tatort` is called and only a community file exists (no local file)
- **THEN** the response is 404

### Requirement: Write endpoint JSON serialization
The write endpoints SHALL serialize ruleset JSON using camelCase property naming and omit null fields. The JSON structure SHALL match the existing RawRuleSet format consumed by RuleSetMerger: `topic`, `aliases`, `media`, `confidence`, `standalone`, `disable`, `rules` at the top level, and within each rule: `id`, `priority`, `confidence`, `strategy`, `seasonRegex`, `episodeRegex`, `captureGroup`, `filters`, `titleRules`.

#### Scenario: Serialized JSON matches RawRuleSet format
- **WHEN** a ruleset is saved via POST or PUT
- **THEN** the JSON file can be deserialized by RuleSetMerger without errors

#### Scenario: Null fields omitted
- **WHEN** a ruleset with no aliases and no media is saved
- **THEN** the JSON file does not contain `aliases` or `media` keys

### Requirement: Write endpoints ensure local directory exists
Before writing a file, the write endpoints SHALL ensure the `DataPaths.LocalRuleSets` directory exists using `IDataFiles.CreateDirectory`. This handles first-run scenarios where the local directory has not been created yet.

#### Scenario: Directory does not exist
- **WHEN** a create or update request is processed and the local rulesets directory does not exist
- **THEN** the directory is created before writing the file
