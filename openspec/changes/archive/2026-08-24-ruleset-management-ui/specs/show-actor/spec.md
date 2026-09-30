## ADDED Requirements

### Requirement: GetRuleSet message
The `ShowActor` SHALL handle a `GetRuleSet` message and respond with a `RuleSetResponse` containing the full effective ruleset, source layer info, and match quality statistics.

#### Scenario: Show with community rules
- **WHEN** a `GetRuleSet` message arrives and community rules exist
- **THEN** the actor SHALL respond with the effective merged ruleset, source="community", and current match quality stats

#### Scenario: Show with no rules
- **WHEN** a `GetRuleSet` message arrives and no rules exist
- **THEN** the actor SHALL respond with a null ruleset response

### Requirement: TestRules message
The `ShowActor` SHALL handle a `TestRules` message by querying the Mediathek for the topic and evaluating the provided rules against the results, returning match traces.

#### Scenario: Test existing rules
- **WHEN** a `TestRules` message arrives with the actor's current rules
- **THEN** the actor SHALL query the Mediathek, evaluate rules with traces, and respond with matched/filtered/unmatched trace arrays and total item count

### Requirement: RemoveLocalOverride message
The `ShowActor` SHALL handle a `RemoveLocalOverride` message by clearing the local override layer and recomputing the effective ruleset.

#### Scenario: Remove existing override
- **WHEN** a `RemoveLocalOverride` message arrives and a local override exists
- **THEN** the actor SHALL persist a `LocalOverrideRemoved` event, clear the local layer, and recompute effective rules from community + generated layers

#### Scenario: Remove when no override exists
- **WHEN** a `RemoveLocalOverride` message arrives and no local override exists
- **THEN** the actor SHALL respond with success (idempotent, no-op)

## MODIFIED Requirements

### Requirement: Match message
The `ShowActor` SHALL handle a `Match(Season, Episode, Items[])` message by applying ruleset matching against items using regex extraction only, without requiring TVDB episode validation. TVDB episodes are used for enrichment (episode names in results) when available, but are not required for matching.

#### Scenario: Match with existing rules without TVDB episodes
- **WHEN** `Match(season=1, episode=null, items)` arrives and rules exist but no TVDB episodes are cached
- **THEN** the actor SHALL evaluate rules using regex extraction (S/E numbers from title patterns) and respond with matched results containing the extracted S/E numbers

#### Scenario: Match with existing rules with TVDB episodes
- **WHEN** `Match(items)` arrives and both rules and TVDB episodes are available
- **THEN** the actor SHALL evaluate rules and enrich matched results with TVDB episode names when possible

#### Scenario: Match without rules triggers inline generation
- **WHEN** `Match(items)` arrives and no rules exist
- **THEN** the actor SHALL call `RuleSetGenerator.Generate(items, tvdbId, showName, episodes)` to create rules, persist a `RulesGenerated` event, apply the new rules immediately, and respond with matched results in the same request

#### Scenario: Match with empty items
- **WHEN** `Match(items)` arrives with an empty items array
- **THEN** the actor SHALL respond with empty `MatchedResults`
