## MODIFIED Requirements

### Requirement: Save local override
The system SHALL expose `PUT /api/v1/rulesets/:topic` accepting a RuleSetFile JSON body via `[FromBody]` model binding and saving it to `data/rulesets/local/` via the RuleSetRegistryActor. The response SHALL use a typed `SuccessResponse` record.

#### Scenario: Save new local ruleset
- **WHEN** a client sends `PUT /api/v1/rulesets/heute-show` with a valid ruleset body
- **THEN** the system SHALL deserialize via `[FromBody]` model binding, write the file to `data/rulesets/local/heute-show.json`, and reload the registry

#### Scenario: Overwrite existing local ruleset
- **WHEN** a client sends `PUT /api/v1/rulesets/tatort` for a topic with an existing local override
- **THEN** the system SHALL overwrite the local file and reload

#### Scenario: Invalid body
- **WHEN** a client sends `PUT /api/v1/rulesets/test` with a malformed JSON body
- **THEN** the framework SHALL return 400 automatically via model binding validation

### Requirement: Delete local override
The system SHALL expose `DELETE /api/v1/rulesets/:topic` removing the local override file and reloading the registry to fall back to community/generated. The response SHALL use a typed `DeletedResponse` record.

#### Scenario: Delete existing override
- **WHEN** a client sends `DELETE /api/v1/rulesets/tatort` and a local override exists
- **THEN** the system SHALL delete the local file and reload, returning `{ "deleted": true }`

#### Scenario: Delete non-existent override
- **WHEN** a client sends `DELETE /api/v1/rulesets/tatort` and no local override exists
- **THEN** the response SHALL return 404

### Requirement: Test rules against Mediathek
The system SHALL expose `POST /api/v1/rulesets/test` accepting a `TestRulesRequest` via `[FromBody]` model binding. The `TestRulesRequest` SHALL be a public record in `FunkArr.Api.Models`. The response SHALL use a typed `TestRulesResponse` record.

#### Scenario: Test with matches
- **WHEN** a client sends a test request for topic "Tatort" with TVDB ID and valid rules
- **THEN** the response SHALL include a typed `TestRulesResponse` with matched, filtered, unmatched, and totalItems properties

#### Scenario: Test with no TVDB ID
- **WHEN** a client sends a test request without a TVDB ID
- **THEN** the system SHALL run matching without TVDB episode data (title-only strategies still work)

#### Scenario: Test with empty Mediathek results
- **WHEN** the Mediathek returns no results for the topic
- **THEN** the response SHALL include an empty result set with a message indicating no items found

#### Scenario: Invalid body
- **WHEN** a client sends `POST /api/v1/rulesets/test` with a malformed JSON body
- **THEN** the framework SHALL return 400 automatically via model binding validation

### Requirement: Reload rulesets
The system SHALL expose `POST /api/v1/rulesets/reload` triggering the RuleSetRegistryActor to reload all rulesets from disk. The response SHALL use a typed `ReloadedResponse` record.

#### Scenario: Reload success
- **WHEN** a client sends `POST /api/v1/rulesets/reload`
- **THEN** the system SHALL send a ReloadLocal message to the registry actor and return `{ "reloaded": true }`

### Requirement: Get single ruleset
The system SHALL expose `GET /api/v1/rulesets/:topic` returning the full RuleSetFile JSON for a single topic using the global JSON serializer options (camelCase, no indentation), not a custom `JsonResult` with `RuleSetJsonOptions`.

#### Scenario: Topic exists
- **WHEN** a client requests `/api/v1/rulesets/tatort`
- **THEN** the response SHALL return the full ruleset serialized with global JSON options (compact, camelCase)

#### Scenario: Topic not found
- **WHEN** a client requests `/api/v1/rulesets/nonexistent`
- **THEN** the response SHALL return 404

## ADDED Requirements

### Requirement: Typed response models
All RulesetController endpoints SHALL return typed response records from `FunkArr.Api.Models`. The models SHALL include `[ProducesResponseType]` attributes on all actions.

#### Scenario: OpenAPI schema completeness
- **WHEN** the OpenAPI spec is generated
- **THEN** all Ruleset API response schemas SHALL be fully typed (no `object` or `any` types)
