## Purpose

Typed request models for the internal REST API, replacing raw `JsonElement` parsing with STJ-deserialized records for create, update, and test endpoints.

## Requirements

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

#### Scenario: Schema validation still applies
- **WHEN** the deserialized body is re-serialized for `IRuleSetValidator.Validate()`
- **THEN** the validator SHALL receive the same JSON structure as before (field names, nesting)

### Requirement: TestScoreRequest uses shared RuleInput
The `TestScoreRequest` SHALL use `RuleInput[]` directly instead of a separate `TestRule[]` type. The record SHALL have: `DefaultConfidence` (float), `Rules` (RuleInput[]), `Candidates` (TestCandidate[]).

#### Scenario: TestScore reuses RuleInput
- **WHEN** a test score request is received
- **THEN** the rules array deserializes using the same `RuleInput` type as Create/Update

#### Scenario: Missing config or candidates
- **WHEN** a JSON body without `rules` or `candidates` is posted
- **THEN** the endpoint SHALL return 400 Bad Request

### Requirement: No manual field extraction in HandleCreate
The `HandleCreate` endpoint SHALL NOT use `request.Body.TryGetValue(...)` for field extraction. Required field validation SHALL be handled by ASP.NET model binding.

#### Scenario: No TryGetValue in HandleCreate
- **WHEN** `HandleCreate` is inspected
- **THEN** no `TryGetValue` calls exist

### Requirement: FilterNode polymorphic deserialization
The `FilterSpec` model with `All`, `Any`, `Not` arrays of `FilterNode` SHALL deserialize correctly using a custom `JsonConverter<FilterNode>`. A JSON object containing `all`, `any`, or `not` properties SHALL deserialize as a `FilterNode.GroupNode`; otherwise it SHALL deserialize as a `FilterNode.ConditionNode`.

#### Scenario: Condition node deserialization
- **WHEN** a filter array element has `field`, `op`, and `value` properties
- **THEN** it SHALL deserialize as `FilterNode.ConditionNode` with a `FilterCondition`

#### Scenario: Group node deserialization
- **WHEN** a filter array element has `all`, `any`, or `not` properties
- **THEN** it SHALL deserialize as `FilterNode.GroupNode` with a nested `FilterSpec`

#### Scenario: Nested groups
- **WHEN** a filter contains groups nested 3 levels deep
- **THEN** the `JsonConverter` SHALL recursively deserialize all levels correctly
