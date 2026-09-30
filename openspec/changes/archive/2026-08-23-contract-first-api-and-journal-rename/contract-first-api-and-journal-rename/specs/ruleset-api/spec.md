## MODIFIED Requirements

### Requirement: Get single ruleset
The system SHALL expose `GET /api/v1/rulesets/:topic` returning the full ruleset JSON for a single topic. The response type SHALL be a generated contract type from `FunkArr.Api.Contracts` rather than the domain `RuleSetFile` type directly. The JSON shape SHALL remain identical (camelCase, global serializer options).

#### Scenario: Topic exists
- **WHEN** a client requests `/api/v1/rulesets/tatort`
- **THEN** the response SHALL return the full ruleset converted to the generated contract type via `.ToContract()` extension method

#### Scenario: Topic not found
- **WHEN** a client requests `/api/v1/rulesets/nonexistent`
- **THEN** the response SHALL return 404

### Requirement: Save local override
The system SHALL expose `PUT /api/v1/rulesets/:topic` accepting a generated contract type as JSON body via `[FromBody]` model binding. The controller SHALL convert the contract type to the domain `RuleSetFile` via `.ToDomain()` extension method before passing to the actor.

#### Scenario: Save new local ruleset
- **WHEN** a client sends `PUT /api/v1/rulesets/heute-show` with a valid ruleset body
- **THEN** the system SHALL deserialize to the generated contract type, convert via `.ToDomain()`, and save

#### Scenario: Overwrite existing local ruleset
- **WHEN** a client sends `PUT /api/v1/rulesets/tatort` for a topic with an existing local override
- **THEN** the system SHALL overwrite the local file and reload

#### Scenario: Invalid body
- **WHEN** a client sends `PUT /api/v1/rulesets/test` with a malformed JSON body
- **THEN** the framework SHALL return 400 automatically via model binding validation

### Requirement: Test rules against Mediathek
The system SHALL expose `POST /api/v1/rulesets/test` accepting a generated contract type as request body. The request and response types SHALL be generated contract types from `FunkArr.Api.Contracts`, not hand-written types from `FunkArr.Api.Models`. Domain types (`Rule`, `MatchedTrace`, `FilteredTrace`, `UnmatchedTrace`) SHALL NOT appear in the API contract.

#### Scenario: Test with matches
- **WHEN** a client sends a test request for topic "Tatort" with TVDB ID and valid rules
- **THEN** the response SHALL use the generated contract type with matched, filtered, unmatched, and totalItems properties

#### Scenario: Test with no TVDB ID
- **WHEN** a client sends a test request without a TVDB ID
- **THEN** the system SHALL run matching without TVDB episode data

#### Scenario: Invalid body
- **WHEN** a client sends `POST /api/v1/rulesets/test` with a malformed JSON body
- **THEN** the framework SHALL return 400 automatically via model binding validation

### Requirement: Typed response models
All RulesetController endpoints SHALL return typed response records from `FunkArr.Api.Contracts` (generated) or `FunkArr.Api.Contracts.Sabnzbd` (hand-written). The models SHALL include `[ProducesResponseType]` attributes on all actions referencing generated contract types.

#### Scenario: OpenAPI schema completeness
- **WHEN** the OpenAPI spec is generated
- **THEN** all Ruleset API response schemas SHALL be fully typed (no `object` or `any` types)

#### Scenario: No domain types in response attributes
- **WHEN** examining `[ProducesResponseType]` attributes on RulesetController
- **THEN** no domain types from `FunkArr.RuleSet` SHALL be referenced
