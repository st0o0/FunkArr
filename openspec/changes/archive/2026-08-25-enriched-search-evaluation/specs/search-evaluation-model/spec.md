## ADDED Requirements

### Requirement: SearchEvaluation record
The system SHALL define a `SearchEvaluation` record that replaces `MatchRecord` as the top-level container for a single search run's results. It SHALL contain: `Id` (string), `Timestamp` (DateTimeOffset), `SearchTopic` (string), `TvdbId` (int?), `Season` (int?), `Episode` (int?), `Source` (string), `TotalResults` (int), and `Items` (IReadOnlyList<ItemEvaluation>).

#### Scenario: SearchEvaluation contains all evaluated items
- **WHEN** the matching engine evaluates 200 Mediathek items
- **THEN** the resulting SearchEvaluation SHALL contain exactly 200 ItemEvaluation entries in the Items list

### Requirement: ItemEvaluation record
The system SHALL define a unified `ItemEvaluation` record carrying raw item data and the evaluation result. Fields: `ItemTitle` (string), `ItemTopic` (string), `ItemChannel` (string), `ItemDuration` (int, seconds), `Outcome` (EvaluationOutcome enum), `Season` (int?, present when Outcome=Matched), `Episode` (int?, present when Outcome=Matched), `EpisodeName` (string?, present when Outcome=Matched), `Confidence` (double?, present when Outcome=Matched), `WinnerRuleIndex` (int?, present when Outcome=Matched), `FilterReason` (string?, present when Outcome=Filtered), `RuleEvaluations` (IReadOnlyList<RuleEvaluation>).

#### Scenario: Matched item evaluation
- **WHEN** an item matches via rule #1 with confidence 1.0 to S08E1622
- **THEN** the ItemEvaluation SHALL have Outcome=Matched (0), Season=8, Episode=1622, EpisodeName="Folge 1622", Confidence=1.0, WinnerRuleIndex=1, and RuleEvaluations containing entries for all rules evaluated (including those that failed before rule #1)

#### Scenario: Filtered item evaluation
- **WHEN** an item is skipped by ContentFilter (e.g., Audiodeskription)
- **THEN** the ItemEvaluation SHALL have Outcome=Filtered (1), FilterReason set, match-result fields null, and RuleEvaluations empty

#### Scenario: Unmatched item evaluation
- **WHEN** an item fails all rules
- **THEN** the ItemEvaluation SHALL have Outcome=Unmatched (2), match-result fields null, and RuleEvaluations containing one entry per rule tested

### Requirement: EvaluationOutcome enum
The system SHALL define an int-backed `EvaluationOutcome` enum with values: Matched=0, Filtered=1, Unmatched=2. It SHALL be serialized as integer on the API wire format.

#### Scenario: Enum serialization
- **WHEN** an ItemEvaluation with Outcome=Matched is serialized to JSON
- **THEN** the outcome field SHALL be the integer 0

### Requirement: RuleEvaluation record
The system SHALL define a `RuleEvaluation` record capturing the full decision at one rule for one item. Fields: `RuleIndex` (int), `Priority` (int), `Strategy` (MatchingStrategy enum), `Outcome` (RuleOutcome enum), `FilterChecks` (IReadOnlyList<FilterCheck>), `StrategyDetail` (StrategyDetail?, null when Outcome=FilterFailed).

#### Scenario: Rule passes filter but strategy fails
- **WHEN** rule #0 has a duration>35 filter that passes (item is 49min) but the regex `Episode\s(\d+)` does not match the title "Sturm der Liebe (1622)"
- **THEN** the RuleEvaluation SHALL have Outcome=NoMatch (2), FilterChecks with one entry (passed=true), and StrategyDetail with regexPattern, regexInput, regexMatched=false

#### Scenario: Rule fails filter
- **WHEN** rule #0 has a duration>35 filter and the item is 12min
- **THEN** the RuleEvaluation SHALL have Outcome=FilterFailed (1), FilterChecks with one entry (field="duration", op=GreaterThan, value="35", actual="12", passed=false), and StrategyDetail=null

#### Scenario: Rule matches
- **WHEN** rule #1 passes its filter and the strategy matches
- **THEN** the RuleEvaluation SHALL have Outcome=Matched (0), FilterChecks all passed, and StrategyDetail with capture details

### Requirement: RuleOutcome enum
The system SHALL define an int-backed `RuleOutcome` enum with values: Matched=0, FilterFailed=1, NoMatch=2. It SHALL be serialized as integer on the API wire format.

#### Scenario: Enum serialization
- **WHEN** a RuleEvaluation with Outcome=FilterFailed is serialized to JSON
- **THEN** the outcome field SHALL be the integer 1

### Requirement: FilterCheck record
The system SHALL define a `FilterCheck` record with fields: `Field` (string), `Op` (FilterOp enum), `Value` (string), `Actual` (string), `Passed` (bool). FilterOp SHALL be serialized as integer.

#### Scenario: Duration filter check
- **WHEN** a filter evaluates duration>35 against an item with 2940 seconds (49 minutes)
- **THEN** the FilterCheck SHALL have Field="duration", Op=GreaterThan (0), Value="35", Actual="49", Passed=true

#### Scenario: Channel filter check
- **WHEN** a filter evaluates channel eq "ARD" against an item with channel "Das Erste"
- **THEN** the FilterCheck SHALL have Field="channel", Op=Eq (5), Value="ARD", Actual="Das Erste", Passed=false

### Requirement: StrategyDetail record
The system SHALL define a `StrategyDetail` record with nullable fields: `RegexPattern` (string?), `RegexInput` (string?), `RegexMatched` (bool?), `CapturedValue` (string?), `ConstructedTitle` (string?), `TvdbMatch` (string?). Only fields relevant to the strategy type SHALL be populated.

#### Scenario: Absolute episode number strategy detail
- **WHEN** the byAbsoluteEpisodeNumber strategy applies regex `\((\d+)\)` to title "Sturm der Liebe (1622)" and captures "1622", then finds TVDB episode "Folge 1622"
- **THEN** the StrategyDetail SHALL have RegexPattern=`\((\d+)\)`, RegexInput="Sturm der Liebe (1622)", RegexMatched=true, CapturedValue="1622", TvdbMatch="Folge 1622"

#### Scenario: Title exact strategy detail
- **WHEN** the itemTitleExact strategy constructs title "Die goldene Zeit" from title rules and matches TVDB episode "Die goldene Zeit"
- **THEN** the StrategyDetail SHALL have ConstructedTitle="Die goldene Zeit", TvdbMatch="Die goldene Zeit", regex fields null

#### Scenario: Failed regex strategy detail
- **WHEN** the byAbsoluteEpisodeNumber strategy applies regex `Episode\s(\d+)` to title "Sturm der Liebe (1622)" and it does not match
- **THEN** the StrategyDetail SHALL have RegexPattern=`Episode\s(\d+)`, RegexInput="Sturm der Liebe (1622)", RegexMatched=false, CapturedValue=null

#### Scenario: Movie title strategy detail
- **WHEN** the movieTitleMatch strategy compares movie title "Das Boot" against item topic "Das Boot"
- **THEN** the StrategyDetail SHALL have ConstructedTitle=null, TvdbMatch=null, regex fields null (movie strategies do not use regex or TVDB)
