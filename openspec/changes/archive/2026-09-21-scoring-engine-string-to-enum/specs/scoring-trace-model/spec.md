## ADDED Requirements

### Requirement: FilterGroupOp enum

The system SHALL define a `FilterGroupOp` enum in `FunkArr.Messages.Scoring` with values: `All`, `Any`, `Not`.

#### Scenario: FilterGroupOp enum members
- **WHEN** the `FilterGroupOp` enum is inspected
- **THEN** it SHALL contain exactly three members: `All`, `Any`, `Not`

### Requirement: IdentificationFailureReason enum

The system SHALL define an `IdentificationFailureReason` enum in `FunkArr.Messages.Scoring.History` with values representing all fixed failure reasons produced by the scoring engine.

#### Scenario: IdentificationFailureReason enum members
- **WHEN** the `IdentificationFailureReason` enum is inspected
- **THEN** it SHALL contain members: `UnknownStrategy`, `SeasonPatternNotMatched`, `NoEpisodePatternConfigured`, `EpisodePatternNotMatched`, `NoTitlePartsConfigured`, `TitlePartRegexNotMatched`, `TitleDoesNotMatch`, `NoDateFoundInTitle`

## MODIFIED Requirements

### Requirement: FilterGroupTrace captures recursive filter evaluation

The system SHALL represent the evaluation of a FilterGroup (All/Any/Not) as a `FilterGroupTrace` record containing the operator as `FilterGroupOp` enum, overall pass/fail, and child node traces.

#### Scenario: All group fully evaluated
- **WHEN** a FilterGroup has operator All with 3 conditions and all pass
- **THEN** the FilterGroupTrace SHALL have Operator=FilterGroupOp.All, Passed=true, and 3 child FilterNodeTraces all with Passed=true

#### Scenario: All group with short-circuit
- **WHEN** a FilterGroup has operator All with 3 conditions and the second one fails
- **THEN** the FilterGroupTrace SHALL have Operator=FilterGroupOp.All, Passed=false, Nodes[0] with Passed=true, Nodes[1] with Passed=false, and Nodes[2] with Skipped=true

#### Scenario: Any group short-circuit on first pass
- **WHEN** a FilterGroup has operator Any with 3 conditions and the first one passes
- **THEN** the FilterGroupTrace SHALL have Operator=FilterGroupOp.Any, Passed=true, Nodes[0] with Passed=true, and Nodes[1] and Nodes[2] with Skipped=true

#### Scenario: Not group with match
- **WHEN** a FilterGroup has operator Not with 2 conditions and the first one matches the item
- **THEN** the FilterGroupTrace SHALL have Operator=FilterGroupOp.Not, Passed=false (because a Not-condition matched)

#### Scenario: Nested filter group
- **WHEN** a FilterGroup has operator All containing a condition and a nested Any group
- **THEN** the FilterGroupTrace SHALL contain a ConditionNode trace and a nested GroupNode trace (FilterGroupTrace)

### Requirement: IdentificationTrace captures strategy attempt result

The system SHALL represent the identification strategy evaluation as an `IdentificationTrace` record containing the strategy as `IdentificationStrategy?` enum, whether it was attempted, and a `IdentificationFailureReason?` enum on failure.

#### Scenario: Successful regex capture
- **WHEN** SeasonAndEpisodeNumber strategy extracts Season="01", Episode="05"
- **THEN** the IdentificationTrace SHALL have Strategy=IdentificationStrategy.SeasonAndEpisodeNumber, Attempted=true, Detail=null

#### Scenario: Failed regex capture
- **WHEN** SeasonAndEpisodeNumber strategy finds no match for EpisodePattern
- **THEN** the IdentificationTrace SHALL have Strategy=IdentificationStrategy.SeasonAndEpisodeNumber, Attempted=true, Detail=IdentificationFailureReason.EpisodePatternNotMatched

#### Scenario: Identification not attempted
- **WHEN** filters failed before identification was reached
- **THEN** the IdentificationTrace SHALL have Attempted=false, Strategy=null, Detail=null

#### Scenario: Failed title construction
- **WHEN** TitleExact strategy fails because a regex TitlePart did not match
- **THEN** the IdentificationTrace SHALL have Strategy=IdentificationStrategy.TitleExact, Attempted=true, Detail=IdentificationFailureReason.TitlePartRegexNotMatched

#### Scenario: Failed airdate extraction
- **WHEN** AirdateExtraction strategy finds no date in the title
- **THEN** the IdentificationTrace SHALL have Strategy=IdentificationStrategy.AirdateExtraction, Attempted=true, Detail=IdentificationFailureReason.NoDateFoundInTitle
