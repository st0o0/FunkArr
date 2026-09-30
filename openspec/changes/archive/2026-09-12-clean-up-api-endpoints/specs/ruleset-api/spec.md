## MODIFIED Requirements

### Requirement: Endpoint file structure
All ruleset API endpoints (`GET /api/rulesets`, `GET /api/rulesets/{id}`, `GET /api/rulesets/{id}/history`, `GET /api/rulesets/{id}/history/{requestId}`, `POST /api/rulesets`, `PUT /api/rulesets/{id}`, `DELETE /api/rulesets/{id}`, `GET /api/rulesets/{id}/raw`, `POST /api/rulesets/test`) SHALL be registered in a single `RuleSetApiEndpoints` class with one `MapRuleSetApi()` extension method and one `MapGroup("/api/rulesets").WithTags("Rulesets")` call.

#### Scenario: Single route group registration
- **WHEN** the application starts
- **THEN** all `/api/rulesets` endpoints SHALL be registered through a single `MapGroup` call in `RuleSetApiEndpoints.MapRuleSetApi()`

### Requirement: Test request parsing extraction
The JSON parsing logic for the test scoring endpoint (`ParseTestRequest`, `ParseRules`, `ParseIdentification`, `ParseTitleRules`, `ParseFilterSpec`, `ParseFilterNodes`, `ParseFilterCondition`, `ParseFilterField`, `ParseFilterOp`, `ParseCandidates`) SHALL reside in a separate `RuleSetTestRequestParser` static class.

#### Scenario: Parser class independence
- **WHEN** the test endpoint receives a request body
- **THEN** it SHALL delegate JSON parsing to `RuleSetTestRequestParser.Parse()` and receive a parsed result or null
