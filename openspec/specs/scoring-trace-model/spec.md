# scoring-trace-model

## Purpose

Defines the full trace data model for scoring: per-item traces, per-rule traces, filter group traces, filter node traces, identification traces, and supporting enums/records. These are the Message-layer records used during evaluation and returned in query responses.

## Requirements

### Requirement: ItemTrace captures per-item scoring result with full rule breakdown

The system SHALL represent the complete scoring trace for a single candidate item as an `ItemTrace` record containing the candidate data, overall result, matched rule identification, and per-rule traces.

#### Scenario: Matched item trace

- **WHEN** a candidate matches rule "season-episode" with score 0.95
- **THEN** the ItemTrace SHALL have Matched=true, Score=0.95, MatchedRuleId="season-episode", a populated Identification, and RuleTraces containing at least the winning rule's trace with Outcome=Matched

#### Scenario: Unmatched item trace

- **WHEN** a candidate matches no rule after trying rules "season-episode" and "title-fallback"
- **THEN** the ItemTrace SHALL have Matched=false, Score=0.0, MatchedRuleId=null, Identification=null, and RuleTraces containing one entry per tried rule with their respective failure outcomes

#### Scenario: Candidate data preserved in trace

- **WHEN** an ItemTrace is created for a ScoreCandidate with Title="Tatort: Die goldene Zeit", Topic="Tatort", Channel="ARD", Duration=5400, Quality=720, Description="Krimi", Timestamp=1719331200
- **THEN** the ItemTrace SHALL preserve all candidate field values for later display

### Requirement: RuleTrace captures per-rule evaluation outcome

The system SHALL represent the evaluation of a single rule against a candidate as a `RuleTrace` record containing the rule identity, outcome, filter trace, and identification trace.

#### Scenario: Rule matched

- **WHEN** a rule's filters pass and identification succeeds
- **THEN** the RuleTrace SHALL have Outcome=Matched, a FilterTrace showing all conditions passed, and an IdentificationTrace with Attempted=true

#### Scenario: Rule failed on filters

- **WHEN** a rule's filter group evaluation returns false
- **THEN** the RuleTrace SHALL have Outcome=FilterFailed, a FilterTrace showing which conditions failed, and IdentificationTrace with Attempted=false

#### Scenario: Rule failed on identification

- **WHEN** a rule's filters pass but the identification strategy returns no result
- **THEN** the RuleTrace SHALL have Outcome=IdentificationFailed, a FilterTrace showing all conditions passed, and an IdentificationTrace with Attempted=true and a Detail string describing the failure

#### Scenario: Rules after winner not traced

- **WHEN** a candidate matches rule at priority 0 and rules at priority 10 and 20 exist
- **THEN** the ItemTrace SHALL contain only one RuleTrace (for priority 0). Rules after the winner SHALL NOT be traced.

### Requirement: RuleOutcome enum

The system SHALL define a RuleOutcome enum with values: Matched, FilterFailed, IdentificationFailed.

#### Scenario: Outcome values

- **WHEN** a RuleTrace is created
- **THEN** its Outcome SHALL be exactly one of Matched, FilterFailed, or IdentificationFailed

### Requirement: FilterGroupTrace captures recursive filter evaluation

The system SHALL represent the evaluation of a FilterGroup (All/Any/Not) as a `FilterGroupTrace` record containing the operator type, overall pass/fail, and child node traces.

#### Scenario: All group fully evaluated

- **WHEN** a FilterGroup has operator All with 3 conditions and all pass
- **THEN** the FilterGroupTrace SHALL have Operator="All", Passed=true, and 3 child FilterNodeTraces all with Passed=true

#### Scenario: All group with short-circuit

- **WHEN** a FilterGroup has operator All with 3 conditions and the second one fails
- **THEN** the FilterGroupTrace SHALL have Operator="All", Passed=false, Nodes[0] with Passed=true, Nodes[1] with Passed=false, and Nodes[2] with Skipped=true

#### Scenario: Any group short-circuit on first pass

- **WHEN** a FilterGroup has operator Any with 3 conditions and the first one passes
- **THEN** the FilterGroupTrace SHALL have Operator="Any", Passed=true, Nodes[0] with Passed=true, and Nodes[1] and Nodes[2] with Skipped=true

#### Scenario: Not group with match

- **WHEN** a FilterGroup has operator Not with 2 conditions and the first one matches the item
- **THEN** the FilterGroupTrace SHALL have Operator="Not", Passed=false (because a Not-condition matched)

#### Scenario: Nested filter group

- **WHEN** a FilterGroup has operator All containing a condition and a nested Any group
- **THEN** the FilterGroupTrace SHALL contain a ConditionNode trace and a nested GroupNode trace (FilterGroupTrace)

### Requirement: FilterNodeTrace captures individual condition evaluation

The system SHALL represent a single filter condition evaluation as a `FilterNodeTrace` record. For condition nodes it SHALL contain the field, operator, expected value, actual resolved value, pass/fail, and skip status.

#### Scenario: Passing condition

- **WHEN** a condition `channel eq "ARD"` is evaluated against an item with channel "ARD"
- **THEN** the FilterNodeTrace SHALL have Field="Channel", Op="Eq", ExpectedValue="ARD", ActualValue="ARD", Passed=true, Skipped=false

#### Scenario: Failing condition

- **WHEN** a condition `duration > 30` is evaluated against an item with duration 900 seconds (15 minutes)
- **THEN** the FilterNodeTrace SHALL have Field="Duration", Op="GreaterThan", ExpectedValue="30", ActualValue="15", Passed=false, Skipped=false

#### Scenario: Skipped condition

- **WHEN** a condition is not evaluated due to short-circuit (prior condition in All group already failed)
- **THEN** the FilterNodeTrace SHALL have Skipped=true, Passed=false, and ActualValue=null

#### Scenario: Null field value

- **WHEN** a condition evaluates field "Description" and the candidate has Description=null
- **THEN** the FilterNodeTrace SHALL have ActualValue=null, Passed=false

### Requirement: IdentificationTrace captures strategy attempt result

The system SHALL represent the identification strategy evaluation as an `IdentificationTrace` record containing the strategy name, whether it was attempted, and a detail string on failure.

#### Scenario: Successful regex capture

- **WHEN** RegexCapture strategy extracts Season="01", Episode="05"
- **THEN** the IdentificationTrace SHALL have Strategy="RegexCapture", Attempted=true, Detail=null

#### Scenario: Failed regex capture

- **WHEN** RegexCapture strategy finds no match for EpisodePattern
- **THEN** the IdentificationTrace SHALL have Strategy="RegexCapture", Attempted=true, Detail="episode pattern did not match"

#### Scenario: Identification not attempted

- **WHEN** filters failed before identification was reached
- **THEN** the IdentificationTrace SHALL have Attempted=false, Strategy=null, Detail=null

#### Scenario: Failed title construction

- **WHEN** TitleConstruction strategy fails because a regex TitlePart did not match
- **THEN** the IdentificationTrace SHALL have Strategy="TitleConstruction", Attempted=true, Detail="title part regex did not match"

#### Scenario: Failed airdate extraction

- **WHEN** AirdateExtraction strategy finds no date in the title
- **THEN** the IdentificationTrace SHALL have Strategy="AirdateExtraction", Attempted=true, Detail="no date found in title"

### Requirement: TracedIdentification captures extracted values

The system SHALL represent a successful identification as a `TracedIdentification` record with Season (string?), Episode (string?), and Title (string?).

#### Scenario: Season and episode identified

- **WHEN** RegexCapture extracts season "02" and episode "14"
- **THEN** the TracedIdentification SHALL have Season="02", Episode="14", Title=null

#### Scenario: Title identified via construction

- **WHEN** TitleConstruction produces "Die goldene Zeit"
- **THEN** the TracedIdentification SHALL have Season=null, Episode=null, Title="Die goldene Zeit"

#### Scenario: Airdate identified

- **WHEN** AirdateExtraction produces "2024-10-24"
- **THEN** the TracedIdentification SHALL have Season=null, Episode=null, Title="2024-10-24"
