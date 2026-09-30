## MODIFIED Requirements

### Requirement: MatchingConfig record
The system SHALL define `MatchingConfig(string RuleSetId, float DefaultConfidence, MatchingRule[] Rules, ResolutionConfig? Resolution)` as a sealed record. This is the contract message sent from RuleSetWorker to MatchMagicManager. The optional `Resolution` field carries per-RuleSet episode resolution configuration. When `Resolution` is null, default resolution behavior applies.

#### Scenario: Config with multiple rules
- **WHEN** a MatchingConfig contains rules with different priorities
- **THEN** rules are evaluated in priority order (lowest first); first matching rule wins

#### Scenario: Default confidence applies
- **WHEN** a MatchingRule has null Confidence
- **THEN** the MatchingConfig's DefaultConfidence is used for that rule's match result

#### Scenario: Config with resolution settings
- **WHEN** a RuleSet JSON includes a `"resolution"` block
- **THEN** the MatchingConfig SHALL have Resolution set to the parsed ResolutionConfig

#### Scenario: Config without resolution settings
- **WHEN** a RuleSet JSON has no `"resolution"` block
- **THEN** the MatchingConfig SHALL have Resolution=null and default resolution behavior applies
