## ADDED Requirements

### Requirement: RulesetDetail response unwrapping
The RulesetDetail.vue frontend SHALL correctly unwrap the `RuleSetResponse` wrapper object when assigning ruleset data.

#### Scenario: Correct property access after fetch
- **WHEN** the detail page fetches a ruleset and receives `{ ruleSet: {...}, source: "local", matchQuality: {...} }`
- **THEN** the component SHALL access `response.ruleSet.media.name`, not `response.media.name`

#### Scenario: Null ruleset handling
- **WHEN** the API returns HTTP 404 (ruleSet is null)
- **THEN** the detail page SHALL show an appropriate not-found state instead of crashing

### Requirement: Export endpoint in OpenAPI spec
The OpenAPI spec `openapi/rulesets.yaml` SHALL include the export endpoints for shows and movies.

#### Scenario: Export endpoint documented
- **WHEN** the OpenAPI spec is examined
- **THEN** it SHALL contain `GET /api/v1/rulesets/{tvdbId}/export` and `GET /api/v1/rulesets/movies/{imdbId}/export` with response content type `application/json` and content-disposition header
