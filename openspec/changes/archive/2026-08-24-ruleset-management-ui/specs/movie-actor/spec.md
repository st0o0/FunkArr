## ADDED Requirements

### Requirement: GetRuleSet message
The `MovieActor` SHALL handle a `GetRuleSet` message and respond with a `RuleSetResponse` containing the full effective ruleset, source layer info, and match quality statistics.

#### Scenario: Movie with community rules
- **WHEN** a `GetRuleSet` message arrives and community rules exist
- **THEN** the actor SHALL respond with the effective merged ruleset, source="community", and current match quality stats

#### Scenario: Movie with no rules
- **WHEN** a `GetRuleSet` message arrives and no rules exist
- **THEN** the actor SHALL respond with a null ruleset response

### Requirement: TestRules message
The `MovieActor` SHALL handle a `TestRules` message by querying the Mediathek for the topic and evaluating provided rules against results, returning match traces.

#### Scenario: Test movie rules
- **WHEN** a `TestRules` message arrives with the actor's current rules
- **THEN** the actor SHALL query the Mediathek, evaluate movie rules with traces, and respond with matched/filtered/unmatched results

### Requirement: RemoveLocalOverride message
The `MovieActor` SHALL handle a `RemoveLocalOverride` message by clearing the local override layer and recomputing the effective ruleset.

#### Scenario: Remove existing override
- **WHEN** a `RemoveLocalOverride` message arrives and a local override exists
- **THEN** the actor SHALL persist a `LocalOverrideRemoved` event, clear the local layer, and recompute effective rules
