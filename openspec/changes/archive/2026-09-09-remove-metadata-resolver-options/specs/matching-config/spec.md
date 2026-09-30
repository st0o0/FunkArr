## MODIFIED Requirements

### Requirement: MatchingConfig record
The system SHALL define `MatchingConfig(string RuleSetId, float DefaultConfidence, MatchingRule[] Rules)` as a sealed record. This is the contract message sent from RuleSetWorker to MatchMagicManager.

#### Scenario: Config with multiple rules
- **WHEN** a MatchingConfig contains rules with different priorities
- **THEN** rules are evaluated in priority order (lowest first); first matching rule wins

#### Scenario: Default confidence applies
- **WHEN** a MatchingRule has null Confidence
- **THEN** the MatchingConfig's DefaultConfidence is used for that rule's match result
