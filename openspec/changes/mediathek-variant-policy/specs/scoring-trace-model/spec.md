## MODIFIED Requirements

### Requirement: ItemTrace captures per-item scoring result with full rule breakdown

The system SHALL represent the complete scoring trace for a single candidate item as an `ItemTrace` record containing the candidate data, overall result, matched rule identification, per-rule traces, and the excluded variant when the candidate was excluded by the variant policy.

#### Scenario: Matched item trace

- **WHEN** a candidate matches rule "season-episode" with score 0.95
- **THEN** the ItemTrace SHALL have Matched=true, Score=0.95, MatchedRuleId="season-episode", a populated Identification, and RuleTraces containing at least the winning rule's trace with Outcome=Matched

#### Scenario: Unmatched item trace

- **WHEN** a candidate matches no rule after trying rules "season-episode" and "title-fallback"
- **THEN** the ItemTrace SHALL have Matched=false, Score=0.0, MatchedRuleId=null, Identification=null, and RuleTraces containing one entry per tried rule with their respective failure outcomes

#### Scenario: Candidate data preserved in trace

- **WHEN** an ItemTrace is created for a ScoreCandidate with Title="Tatort: Die goldene Zeit", Topic="Tatort", Channel="ARD", Duration=5400, Quality=720, Description="Krimi", Timestamp=1719331200
- **THEN** the ItemTrace SHALL preserve all candidate field values for later display

#### Scenario: ItemTrace without enrichment
- **WHEN** a test is run without enrichment config
- **THEN** ItemTrace.EnrichmentTrace is null

#### Scenario: ItemTrace with successful enrichment
- **WHEN** a test is run with enrichment config and the item matched and enrichment resolved it
- **THEN** ItemTrace.EnrichmentTrace contains the enrichment result with enriched=true

#### Scenario: ItemTrace with failed enrichment
- **WHEN** a test is run with enrichment config and the item matched but enrichment could not resolve it
- **THEN** ItemTrace.EnrichmentTrace contains enriched=false with a detail string explaining why

#### Scenario: Unmatched item has no enrichment trace
- **WHEN** a test is run with enrichment config but the item did not match any scoring rule
- **THEN** ItemTrace.EnrichmentTrace is null (enrichment only applies to scored matches)


#### Scenario: Excluded item trace
- **WHEN** a candidate is excluded by the variant policy as AudioDescription
- **THEN** the ItemTrace SHALL have Matched=false, Score=0.0, MatchedRuleId=null, Identification=null, empty RuleTraces and ExcludedVariant=AudioDescription

#### Scenario: Not excluded item trace
- **WHEN** a candidate is not excluded by the variant policy
- **THEN** the ItemTrace SHALL have ExcludedVariant=null

#### Scenario: Persisted traces without exclusion data
- **WHEN** a scoring history entry persisted before this change is read
- **THEN** its item traces SHALL have ExcludedVariant=null
