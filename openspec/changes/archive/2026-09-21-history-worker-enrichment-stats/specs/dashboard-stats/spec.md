## MODIFIED Requirements

### Requirement: Enrichment rate in ruleset list
The ruleset list API response SHALL include enrichmentRate alongside matchRate.

#### Scenario: List entry with enrichment stats
- **WHEN** a ruleset has history records with enrichment data
- **THEN** the list entry includes enrichmentRate as a double (0-1)

#### Scenario: List entry without enrichment stats
- **WHEN** a ruleset has no enrichment data (enrichment disabled or no external IDs)
- **THEN** the list entry has null enrichmentRate

### Requirement: Enrichment rate in UI
The RuleSetList view SHALL display enrichment rate alongside match rate.

#### Scenario: Show enrichment rate
- **WHEN** a ruleset entry has enrichmentRate
- **THEN** it is displayed as a percentage alongside the match rate
