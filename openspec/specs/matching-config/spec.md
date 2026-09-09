## Requirements

### Requirement: FilterField enum
The system SHALL define a `FilterField` enum in FunkArr.Messages with values: Title, Topic, Channel, Description, Duration, Timestamp. All filter conditions MUST reference fields via this enum.

#### Scenario: Filter condition uses enum field
- **WHEN** a FilterCondition is created with `FilterField.Duration`
- **THEN** the Field property is the enum value, not a string

### Requirement: FilterOp enum in Messages
The system SHALL define a `FilterOp` enum in FunkArr.Messages with values: Eq, Contains, NotContains, GreaterThan, LessThan, Regex. The existing FilterOp in FunkArr.MatchMagic SHALL be removed.

#### Scenario: FilterOp lives in Messages namespace
- **WHEN** FunkArr.Messages is compiled
- **THEN** it contains `FunkArr.Messages.Scoring.FilterOp` with all six values

### Requirement: IdentificationStrategy enum with three strategies
The system SHALL define an `IdentificationStrategy` enum with three values: RegexCapture, TitleConstruction, AirdateExtraction.

#### Scenario: RegexCapture covers season+episode and absolute episode
- **WHEN** a MatchingRule uses `IdentificationStrategy.RegexCapture` with only EpisodePattern set (SeasonPattern is null)
- **THEN** the rule identifies by absolute episode number only

#### Scenario: RegexCapture with both patterns
- **WHEN** a MatchingRule uses `IdentificationStrategy.RegexCapture` with both SeasonPattern and EpisodePattern set
- **THEN** the rule identifies by season and episode number

### Requirement: TitleMatchMode enum
The system SHALL define a `TitleMatchMode` enum with values: Exact, Contains. This enum MUST be used with `IdentificationStrategy.TitleConstruction` to specify the comparison mode.

#### Scenario: TitleConstruction with Exact mode
- **WHEN** a MatchingRule uses TitleConstruction with `TitleMatchMode.Exact`
- **THEN** the constructed title must equal the item title exactly (case-insensitive)

#### Scenario: TitleConstruction with Contains mode
- **WHEN** a MatchingRule uses TitleConstruction with `TitleMatchMode.Contains`
- **THEN** the constructed title must be contained within the item title (case-insensitive, umlaut-normalized)

### Requirement: TitlePartType enum
The system SHALL define a `TitlePartType` enum with values: Static, Regex.

#### Scenario: Static title part
- **WHEN** a TitlePart has `TitlePartType.Static`
- **THEN** its Value property provides the literal text

#### Scenario: Regex title part
- **WHEN** a TitlePart has `TitlePartType.Regex`
- **THEN** its Pattern and Field properties are used to capture text from the media item

### Requirement: FilterCondition record
The system SHALL define `FilterCondition(FilterField Field, FilterOp Op, string Value)` as a sealed record in FunkArr.Messages.

#### Scenario: Create a duration filter
- **WHEN** `new FilterCondition(FilterField.Duration, FilterOp.GreaterThan, "40")` is constructed
- **THEN** all properties are set to the provided enum and string values

### Requirement: FilterSpec with recursive nesting
The system SHALL define `FilterSpec(FilterNode[]? All, FilterNode[]? Any, FilterNode[]? Not)` where `FilterNode` is an abstract record with two implementations: `ConditionNode(FilterCondition)` and `GroupNode(FilterSpec)`.

#### Scenario: Flat filter with all-conditions
- **WHEN** a FilterSpec has `All` containing only ConditionNodes
- **THEN** all conditions MUST match for the filter to pass

#### Scenario: Nested filter groups
- **WHEN** a FilterSpec has `All` containing a ConditionNode and a GroupNode with an inner FilterSpec using `Any`
- **THEN** the outer All requires the condition AND at least one inner Any condition to match

#### Scenario: Not filter
- **WHEN** a FilterSpec has `Not` containing ConditionNodes
- **THEN** none of the conditions may match for the filter to pass

### Requirement: TitlePart record
The system SHALL define `TitlePart(TitlePartType Type, string? Value, string? Pattern, FilterField? Field, int? CaptureGroup)` as a sealed record.

#### Scenario: Static title part construction
- **WHEN** a TitlePart with Type=Static and Value="Folge " is evaluated
- **THEN** it contributes the literal string "Folge " to the constructed title

#### Scenario: Regex title part extraction
- **WHEN** a TitlePart with Type=Regex, Pattern, and Field is evaluated against a media item
- **THEN** it captures text from the specified field using the regex pattern and contributes it to the constructed title

### Requirement: IdentificationSpec record
The system SHALL define `IdentificationSpec(IdentificationStrategy Strategy, string? SeasonPattern, string? EpisodePattern, int? CaptureGroup, TitleMatchMode? MatchMode, TitlePart[]? TitleParts)` as a sealed record.

#### Scenario: RegexCapture spec
- **WHEN** Strategy is RegexCapture
- **THEN** SeasonPattern and/or EpisodePattern MUST be set; MatchMode and TitleParts are ignored

#### Scenario: TitleConstruction spec
- **WHEN** Strategy is TitleConstruction
- **THEN** MatchMode and TitleParts MUST be set; SeasonPattern and EpisodePattern are ignored

#### Scenario: AirdateExtraction spec
- **WHEN** Strategy is AirdateExtraction
- **THEN** no additional properties are required; the strategy uses hardcoded German date parsing

### Requirement: MatchingRule record
The system SHALL define `MatchingRule(string Id, int Priority, float? Confidence, FilterSpec? Filters, IdentificationSpec Identification)` as a sealed record.

#### Scenario: Rule with filters and identification
- **WHEN** a MatchingRule has Filters and Identification set
- **THEN** filters are evaluated first; identification runs only if filters pass

#### Scenario: Rule without filters
- **WHEN** a MatchingRule has null Filters
- **THEN** the rule applies to all items (filters implicitly pass)

### Requirement: MatchingConfig record
The system SHALL define `MatchingConfig(string RuleSetId, float DefaultConfidence, MatchingRule[] Rules)` as a sealed record. This is the contract message sent from RuleSetWorker to MatchMagicManager.

#### Scenario: Config with multiple rules
- **WHEN** a MatchingConfig contains rules with different priorities
- **THEN** rules are evaluated in priority order (lowest first); first matching rule wins

#### Scenario: Default confidence applies
- **WHEN** a MatchingRule has null Confidence
- **THEN** the MatchingConfig's DefaultConfidence is used for that rule's match result
