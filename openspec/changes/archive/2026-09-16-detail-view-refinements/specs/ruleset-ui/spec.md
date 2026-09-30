## MODIFIED Requirements

### Requirement: RuleSet detail page
The collapsed rule card header SHALL show small metadata counts after the priority: number of filter conditions (if any) and number of title rules (if any), displayed as muted text like "2 Filter" or "1 Titelregel". The scoring history button SHALL be labeled "Scoring-Verlauf" instead of "Bewertungsverlauf". The Identity and Source sections SHALL be merged into a single card with the source badge and update timestamp shown inline with the identity data.

#### Scenario: Collapsed header with filter count
- **WHEN** a collapsed rule has 2 filter conditions and 1 title rule
- **THEN** the header shows `rule-id | Strategy | 1 Titelregel · 2 Filter | prio N`

#### Scenario: Collapsed header without metadata
- **WHEN** a collapsed rule has no filters and no title rules (e.g. airdate strategy)
- **THEN** the header shows only `rule-id | Strategy | prio N` without extra counts

#### Scenario: Scoring history button label
- **WHEN** the detail page renders
- **THEN** the scoring history button displays "Scoring-Verlauf" (DE) or "Scoring History" (EN)

#### Scenario: Merged identity and source
- **WHEN** a ruleset detail renders
- **THEN** identity data and source info appear in a single card section, not as two separate cards
