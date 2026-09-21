## Requirements

### Requirement: FilterField enum
The system SHALL define a `FilterField` enum in FunkArr.Messages with values: Title, Topic, Channel, Description, Duration, Timestamp. All filter conditions MUST reference fields via this enum. The enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<FilterField>))]` with camelCase naming for automatic deserialization from JSON ruleset files.

#### Scenario: Filter condition uses enum field
- **WHEN** a FilterCondition is created with `FilterField.Duration`
- **THEN** the Field property is the enum value, not a string

#### Scenario: Deserialize filter field from JSON
- **WHEN** a ruleset JSON contains `"field": "title"`
- **THEN** it deserializes to `FilterField.Title` without manual switch logic

### Requirement: FilterOp enum in Messages
The system SHALL define a `FilterOp` enum in FunkArr.Messages with values: Eq, Contains, NotContains, GreaterThan, LessThan, Regex. The enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<FilterOp>))]` with camelCase naming for automatic deserialization from JSON ruleset files.

#### Scenario: FilterOp lives in Messages namespace
- **WHEN** FunkArr.Messages is compiled
- **THEN** it contains `FunkArr.Messages.Scoring.FilterOp` with all six values

#### Scenario: Deserialize filter op from JSON
- **WHEN** a ruleset JSON contains `"op": "greaterThan"`
- **THEN** it deserializes to `FilterOp.GreaterThan` without manual switch logic

### Requirement: IdentificationStrategy enum
The `IdentificationStrategy` enum SHALL have 5 members with `[JsonPropertyName]` attributes matching the JSON ruleset schema strings:
- `SeasonAndEpisodeNumber` (`"seasonAndEpisodeNumber"`)
- `AbsoluteEpisodeNumber` (`"byAbsoluteEpisodeNumber"`)
- `TitleExact` (`"itemTitleExact"`)
- `TitleIncludes` (`"itemTitleIncludes"`)
- `AirdateExtraction` (`"itemTitleEqualsAirdate"`)

The enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<IdentificationStrategy>))]` for automatic deserialization from JSON ruleset files.

#### Scenario: Deserialize strategy from JSON
- **WHEN** a ruleset JSON contains `"strategy": "seasonAndEpisodeNumber"`
- **THEN** it deserializes to `IdentificationStrategy.SeasonAndEpisodeNumber` without manual switch logic

#### Scenario: All 5 strategies have JsonPropertyName
- **WHEN** the enum is inspected
- **THEN** each member has a `[JsonPropertyName]` matching the JSON schema string

### Requirement: TitlePartType enum
The system SHALL define a `TitlePartType` enum with values: Static, Regex. The enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<TitlePartType>))]` with camelCase naming for automatic deserialization from JSON ruleset files.

#### Scenario: Static title part
- **WHEN** a TitlePart has `TitlePartType.Static`
- **THEN** its Value property provides the literal text

#### Scenario: Regex title part
- **WHEN** a TitlePart has `TitlePartType.Regex`
- **THEN** its Pattern and Field properties are used to capture text from the media item

#### Scenario: Deserialize title part type from JSON
- **WHEN** a ruleset JSON contains `"type": "regex"`
- **THEN** it deserializes to `TitlePartType.Regex` without manual switch logic

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
The system SHALL define `IdentificationSpec(IdentificationStrategy Strategy, string? SeasonPattern, string? EpisodePattern, int? CaptureGroup, TitlePart[]? TitleParts)` as a sealed record. The record SHALL NOT have a `MatchMode` parameter - the strategy enum alone determines matching behavior.

#### Scenario: SeasonAndEpisodeNumber spec
- **WHEN** Strategy is SeasonAndEpisodeNumber
- **THEN** SeasonPattern and EpisodePattern MUST be set; TitleParts are ignored

#### Scenario: TitleExact spec
- **WHEN** Strategy is TitleExact
- **THEN** TitleParts MUST be set; SeasonPattern and EpisodePattern are ignored

#### Scenario: AirdateExtraction spec
- **WHEN** Strategy is AirdateExtraction
- **THEN** no additional properties are required; the strategy uses hardcoded German date parsing

#### Scenario: IdentificationSpec has no MatchMode
- **WHEN** `IdentificationSpec` is inspected
- **THEN** it has parameters: Strategy, SeasonPattern, EpisodePattern, CaptureGroup, TitleParts - no MatchMode

### Requirement: MatchingRule record
The system SHALL define `MatchingRule(string Id, int Priority, float? Confidence, FilterSpec? Filters, IdentificationSpec Identification)` as a sealed record.

#### Scenario: Rule with filters and identification
- **WHEN** a MatchingRule has Filters and Identification set
- **THEN** filters are evaluated first; identification runs only if filters pass

#### Scenario: Rule without filters
- **WHEN** a MatchingRule has null Filters
- **THEN** the rule applies to all items (filters implicitly pass)

### Requirement: MatchingConfig record
The system SHALL define `MatchingConfig(string RuleSetId, float DefaultConfidence, MatchingRule[] Rules)` as a sealed record. This is the contract message sent from RuleSetWorker to ScoringManager.

#### Scenario: Config with multiple rules
- **WHEN** a MatchingConfig contains rules with different priorities
- **THEN** rules are evaluated in priority order (lowest first); first matching rule wins

#### Scenario: Default confidence applies
- **WHEN** a MatchingRule has null Confidence
- **THEN** the MatchingConfig's DefaultConfidence is used for that rule's match result

### Requirement: RegisterRuleSet carries EnrichmentConfig
The `RegisterRuleSet` message SHALL include an optional `EnrichmentConfig? Enrichment` parameter (default null). `RuleSetWorker` SHALL extract the `EnrichmentConfig` from `RuleSetMerger.ExtractIdentity` and pass it to `RegisterRuleSet`.

#### Scenario: RegisterRuleSet with enrichment config
- **WHEN** a ruleset JSON has an `"enrichment"` section
- **THEN** `RegisterRuleSet` SHALL carry the parsed `EnrichmentConfig`

#### Scenario: RegisterRuleSet without enrichment config
- **WHEN** a ruleset JSON has no `"enrichment"` section
- **THEN** `RegisterRuleSet` SHALL carry the default `EnrichmentConfig` (Enabled=true, Methods=[Title, Airdate], etc.)

### Requirement: RuleSetResolverState stores EnrichmentConfig per ruleSetId
`RuleSetResolverState` SHALL maintain an `ImmutableDictionary<string, EnrichmentConfig>` keyed by ruleSetId. When a `RegisterRuleSet` is applied, the enrichment config SHALL be stored. When a `DeregisterRuleSet` is applied, it SHALL be removed.

#### Scenario: Store enrichment config on register
- **WHEN** `Apply(RegisterRuleSet)` is called with an `EnrichmentConfig`
- **THEN** the config SHALL be retrievable by ruleSetId

#### Scenario: Remove enrichment config on deregister
- **WHEN** `Apply(DeregisterRuleSet)` is called
- **THEN** the enrichment config for that ruleSetId SHALL be removed

### Requirement: RuleSetResolved includes EnrichmentConfig
The `RuleSetResolved` response SHALL include an `EnrichmentConfig? Enrichment` field. When the resolver resolves a ruleset, it SHALL include the stored `EnrichmentConfig` in the response.

#### Scenario: Resolve returns enrichment config
- **WHEN** a `ResolveRuleSet` is resolved to a ruleSetId that has an enrichment config
- **THEN** the `RuleSetResolved` response SHALL include the stored `EnrichmentConfig`

#### Scenario: Resolve without enrichment config
- **WHEN** a `ResolveRuleSet` is resolved to a ruleSetId with no enrichment config stored
- **THEN** the `RuleSetResolved.Enrichment` SHALL be null

### Requirement: RuleSetMerger uses typed enums
The `RuleSetMerger` internal classes (`RawRule`, `RawTitleRule`) SHALL use enum-typed properties instead of strings for `Strategy`, `Field`, `Op`, and `Type`. The manual `ParseFilterField`, `ParseFilterOp` switch methods SHALL be removed.

#### Scenario: RawRule.Strategy is enum
- **WHEN** `RawRule` is inspected
- **THEN** `Strategy` is `IdentificationStrategy?` not `string`

#### Scenario: No ParseFilterField method
- **WHEN** `RuleSetMerger` is inspected
- **THEN** no methods named ParseFilterField or ParseFilterOp exist
