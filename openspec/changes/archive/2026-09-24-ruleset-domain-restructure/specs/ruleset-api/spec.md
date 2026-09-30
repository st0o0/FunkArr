## MODIFIED Requirements

### Requirement: RuleSet detail endpoint
The system SHALL expose `GET /api/rulesets/{id}` that Asks the `IRuleSetRegion` (ShardRegion) directly with a `QueryRuleSetDetail` message implementing `IWithRuleSetId`. The endpoint SHALL NOT go through the `IRuleSetManager`.

#### Scenario: Detail routes to ShardRegion
- **WHEN** `GET /api/rulesets/tatort` is called
- **THEN** the endpoint SHALL Ask `IRuleSetRegion` directly and the RuleSetWorker SHALL respond

#### Scenario: Detail for unknown ruleset
- **WHEN** `GET /api/rulesets/nonexistent` is called
- **THEN** the response SHALL be 404

### Requirement: Validate on create
`POST /api/rulesets` SHALL convert the `CreateRuleSetRequest` to domain types via mapping extensions and Ask the `IRuleSetRegion` with a `CreateLocalRuleSet` message. The RuleSetWorker SHALL handle validation and disk writes.

#### Scenario: Valid create
- **WHEN** a valid `CreateRuleSetRequest` is posted
- **THEN** the endpoint SHALL Ask `IRuleSetRegion` and return 201 on `CreateLocalRuleSetCompleted`

#### Scenario: Duplicate
- **WHEN** the ruleSetId already exists as a local file
- **THEN** the response SHALL be 409

### Requirement: Validate on update
`PUT /api/rulesets/{id}` SHALL Ask the `IRuleSetRegion` with an `UpdateLocalRuleSet` message.

#### Scenario: Valid update
- **WHEN** a valid `UpdateRuleSetRequest` is put
- **THEN** the endpoint SHALL Ask `IRuleSetRegion` and return 200 on success

#### Scenario: Unknown ruleset
- **WHEN** put for nonexistent ruleSetId
- **THEN** the response SHALL be 404

### Requirement: Delete endpoint
`DELETE /api/rulesets/{id}` SHALL Ask the `IRuleSetRegion` with a `DeleteLocalRuleSet` message.

#### Scenario: Delete existing
- **WHEN** delete for a ruleSetId with a local file
- **THEN** the response SHALL be 200

### Requirement: Export endpoint
`GET /api/rulesets/{id}/export` SHALL Ask the `IRuleSetRegion` with an `ExportRuleSet` message.

#### Scenario: Export existing
- **WHEN** export for a ruleSetId with a local component
- **THEN** the response SHALL be 200 with flattened JSON

### Requirement: List endpoint stays with Manager
`GET /api/rulesets` SHALL continue to Ask the `IRuleSetManager` for the list. The Manager SHALL respond from its in-memory summaries cache.

#### Scenario: List rulesets
- **WHEN** `GET /api/rulesets` is called
- **THEN** the endpoint SHALL Ask `IRuleSetManager` and the Manager SHALL respond from its summaries cache without disk I/O

### Requirement: Endpoint file structure
All ruleset API endpoints SHALL be registered in `RuleSetApiEndpoints`. The class SHALL NOT contain `_diskJsonOptions`, `SerializeForDisk`, `JsonStringEnumMemberName`, or direct `IDataFiles`/`IRuleSetValidator` injections. Single-entity operations SHALL Ask `IRuleSetRegion`. List SHALL Ask `IRuleSetManager`.

#### Scenario: No disk concerns in API
- **WHEN** `RuleSetApiEndpoints.cs` is inspected
- **THEN** it SHALL NOT contain disk serialization, file paths, or validator calls
