## ADDED Requirements

### Requirement: Test ad-hoc rules endpoint
The `POST /api/v1/rulesets/test-adhoc` endpoint SHALL accept a `RuleSetFile` body and a `query` parameter, query MediathekViewWeb, evaluate the provided rules against the results, and return `ItemEvaluation[]` traces without requiring the rules to be persisted first. This supports the generate preview flow where rules are tested before saving.

#### Scenario: Test unsaved rules against Mediathek
- **WHEN** `POST /api/v1/rulesets/test-adhoc` is called with `{ "ruleSet": {...}, "query": "Tatort" }`
- **THEN** the endpoint SHALL query MediathekViewWeb for the topic, evaluate the rules using `RuleSetMatchingEngine.EvaluateRulesWithTraces`, and return the `ItemEvaluation[]` array

#### Scenario: No Mediathek results
- **WHEN** the Mediathek query returns no items for the given topic
- **THEN** the endpoint SHALL return HTTP 200 with an empty array

#### Scenario: Invalid rules
- **WHEN** the provided `RuleSetFile` has no rules
- **THEN** the endpoint SHALL return HTTP 200 with all items marked as `Unmatched`
