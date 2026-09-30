# ruleset-query-messages

## MODIFIED Requirements

### Requirement: Per-query response types for RuleSet queries

Each RuleSet query SHALL have its own response type following the Result/Failed pattern. `RuleSetNotFound` SHALL be removed.

#### Scenario: QueryRuleSetDetail responses
- **WHEN** `QueryRuleSetDetail.cs` is examined
- **THEN** it SHALL contain `QueryRuleSetDetail`, `RuleSetDetailResponse`, `RuleSetDetailResult`, `RuleSetDetailFailed`

#### Scenario: Not-found uses Failed with exception
- **WHEN** a ruleset is not found
- **THEN** the actor SHALL return `RuleSetDetailFailed(new RuleSetNotFoundException(id))`

## REMOVED Requirements

### Requirement: RuleSetNotFound standalone type
**Reason**: Not-found is a failure variant, not a standalone type.
**Migration**: Use `RuleSetDetailFailed(new RuleSetNotFoundException(...))` instead.
