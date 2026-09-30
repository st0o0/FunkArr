## MODIFIED Requirements

### Requirement: Three-layer ruleset management
The `MediaRuleSetActor` SHALL manage three nullable `RuleSetFile?` slots: `CommunityRuleSet`, `GeneratedRuleSet`, `LocalOverrideRuleSet`. The effective ruleset SHALL be computed via `RuleSetMerger.Resolve(community, generated, local)` instead of `Local ?? Generated ?? Community`. When no layer has `Overrides`, behavior SHALL be identical to the previous winner-takes-all logic (backward compatible). Per-rule provenance SHALL be available from the resolved `EffectiveRuleSet`.

#### Scenario: Community rules applied transiently
- **WHEN** `ApplyCommunityRules(ruleSetFile)` is received
- **THEN** the actor SHALL update the community slot, call `RuleSetMerger.Resolve`, and NOT persist any event

#### Scenario: Local override with merge
- **WHEN** a local override with `Overrides { Base = Community, Add = [new-rule] }` is applied
- **THEN** the effective rules SHALL contain all community rules plus the new local rule, each with correct provenance

#### Scenario: Standalone local (no overrides, backward compat)
- **WHEN** a local override with no `Overrides` section is applied
- **THEN** the effective rules SHALL contain only the local rules (winner-takes-all)

#### Scenario: Validation on apply
- **WHEN** a local override with an invalid Replace ID is applied
- **THEN** the actor SHALL reject the override and respond with validation errors

#### Scenario: Recovery recomputes via merger
- **WHEN** the actor recovers from journal and community is pushed
- **THEN** `RecomputeEffectiveRules` SHALL call `RuleSetMerger.Resolve` with the recovered slots

### Requirement: GetRuleSet message
The `MediaRuleSetActor` SHALL handle `GetRuleSet` and respond with `RuleSetResponse` containing the effective ruleset, source layer info, and **per-rule provenance**.

#### Scenario: Rules with provenance
- **WHEN** `GetRuleSet` arrives and effective rules come from community (2 rules) and local add (1 rule)
- **THEN** the response SHALL include per-rule provenance showing which layer each rule originated from
