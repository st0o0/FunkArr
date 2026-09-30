## MODIFIED Requirements

### Requirement: CreateRuleSetRequest typed model
The `CreateRuleSetRequest` SHALL be a fully typed record with: `RuleSetId` (string, required), `Topic` (string, required), `Media` (MediaInput, required), `Rules` (RuleInput[], required), `Aliases` (string[]?), `Confidence` (float?), `Standalone` (bool?), `Disable` (string[]?). No `[JsonExtensionData]` SHALL be used.

#### Scenario: Typed deserialization
- **WHEN** the API receives a Create request with all fields
- **THEN** ASP.NET model binding deserializes into the typed record with enum fields resolved

#### Scenario: Missing required field returns 400
- **WHEN** the API receives a Create request without `topic`
- **THEN** model binding returns 400 Bad Request automatically

#### Scenario: Serialization for disk
- **WHEN** the typed request is serialized to JSON for disk storage
- **THEN** the output matches the ruleset JSON schema (camelCase, no nulls, indented)

#### Scenario: OpenAPI shows request schema
- **WHEN** the OpenAPI spec is generated
- **THEN** the Create endpoint shows the full request body schema with all fields and types

### Requirement: UpdateRuleSetRequest typed model
The `UpdateRuleSetRequest` SHALL be a fully typed record identical to `CreateRuleSetRequest` but without `RuleSetId` (the ID comes from the route parameter).

#### Scenario: Update uses route ID
- **WHEN** a PUT request to `/api/rulesets/{id}` is received
- **THEN** the body is deserialized as `UpdateRuleSetRequest` and the ID comes from the route

### Requirement: TestScoreRequest uses shared RuleInput
The `TestScoreRequest` SHALL use `RuleInput[]` directly instead of a separate `TestRule[]` type. The record SHALL have: `DefaultConfidence` (float), `Rules` (RuleInput[]), `Candidates` (TestCandidate[]).

#### Scenario: TestScore reuses RuleInput
- **WHEN** a test score request is received
- **THEN** the rules array deserializes using the same `RuleInput` type as Create/Update

### Requirement: No manual field extraction in HandleCreate
The `HandleCreate` endpoint SHALL NOT use `request.Body.TryGetValue(...)` for field extraction. Required field validation SHALL be handled by ASP.NET model binding.

#### Scenario: No TryGetValue in HandleCreate
- **WHEN** `HandleCreate` is inspected
- **THEN** no `TryGetValue` calls exist
