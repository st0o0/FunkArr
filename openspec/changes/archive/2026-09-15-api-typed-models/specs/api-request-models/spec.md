## ADDED Requirements

### Requirement: Create ruleset request model
The system SHALL accept `POST /api/rulesets` with a typed `CreateRuleSetRequest` record containing `RuleSetId` (string, required), `Topic` (string, required), and the remaining ruleset body fields. The endpoint SHALL bind the request body via STJ deserialization instead of `JsonElement`.

#### Scenario: Valid create request deserialization
- **WHEN** a JSON body with `ruleSetId`, `topic`, and valid ruleset fields is posted to `POST /api/rulesets`
- **THEN** STJ SHALL deserialize it into a `CreateRuleSetRequest` instance with all fields populated

#### Scenario: Missing required field
- **WHEN** a JSON body without `ruleSetId` is posted
- **THEN** the endpoint SHALL return 400 Bad Request

### Requirement: Update ruleset request model
The system SHALL accept `PUT /api/rulesets/{id}` with a typed `UpdateRuleSetRequest` record containing the ruleset body fields (without `ruleSetId`, since it comes from the route). The endpoint SHALL bind the request body via STJ deserialization instead of `JsonElement`.

#### Scenario: Valid update request deserialization
- **WHEN** a valid JSON body is put to `PUT /api/rulesets/{id}`
- **THEN** STJ SHALL deserialize it into an `UpdateRuleSetRequest` instance

#### Scenario: Schema validation still applies
- **WHEN** the deserialized body is re-serialized for `IRuleSetValidator.Validate()`
- **THEN** the validator SHALL receive the same JSON structure as before (field names, nesting)

### Requirement: Test score request model
The system SHALL accept `POST /api/rulesets/test` with a typed `TestScoreRequest` record containing `Config` (object with `DefaultConfidence` and `Rules` array) and `Candidates` (array of candidate objects). The endpoint SHALL bind the request body via STJ deserialization instead of `JsonElement`.

#### Scenario: Valid test request deserialization
- **WHEN** a JSON body with `config` (containing `defaultConfidence` and `rules` array) and `candidates` array is posted
- **THEN** STJ SHALL deserialize it into a `TestScoreRequest` with nested `TestRuleConfig`, `TestRule[]`, and `TestCandidate[]`

#### Scenario: Missing config or candidates
- **WHEN** a JSON body without `config` or `candidates` is posted
- **THEN** the endpoint SHALL return 400 Bad Request

### Requirement: Test rule model with strategy mapping
Each `TestRule` in the request SHALL contain `Id`, `Priority`, `Confidence`, `Strategy` (string matching frontend names), `Filters` (nullable), and identification fields (`SeasonRegex`, `EpisodeRegex`, `CaptureGroup`, `TitleRules`). The system SHALL map the `Strategy` string to domain `IdentificationStrategy` enum and construct the `IdentificationSpec`.

#### Scenario: RegexCapture strategy mapping
- **WHEN** a test rule has `strategy: "seasonAndEpisodeNumber"` with `seasonRegex` and `episodeRegex`
- **THEN** the system SHALL produce an `IdentificationSpec` with `IdentificationStrategy.RegexCapture` and both patterns set

#### Scenario: TitleConstruction strategy mapping
- **WHEN** a test rule has `strategy: "itemTitleExact"` with `titleRules` array
- **THEN** the system SHALL produce an `IdentificationSpec` with `IdentificationStrategy.TitleConstruction`, `MatchMode.Exact`, and parsed `TitlePart[]`

#### Scenario: AirdateExtraction strategy mapping
- **WHEN** a test rule has `strategy: "itemTitleEqualsAirdate"`
- **THEN** the system SHALL produce an `IdentificationSpec` with `IdentificationStrategy.AirdateExtraction`

#### Scenario: Unknown strategy
- **WHEN** a test rule has an unrecognized `strategy` value
- **THEN** the rule SHALL be skipped (not included in the scoring config)

### Requirement: FilterNode polymorphic deserialization
The `FilterSpec` model with `All`, `Any`, `Not` arrays of `FilterNode` SHALL deserialize correctly using a custom `JsonConverter<FilterNode>`. A JSON object containing `all`, `any`, or `not` properties SHALL deserialize as a `FilterNode.GroupNode`; otherwise it SHALL deserialize as a `FilterNode.ConditionNode`.

#### Scenario: Condition node deserialization
- **WHEN** a filter array element has `field`, `op`, and `value` properties
- **THEN** it SHALL deserialize as `FilterNode.ConditionNode` with a `FilterCondition`

#### Scenario: Group node deserialization
- **WHEN** a filter array element has `all`, `any`, or `not` properties
- **THEN** it SHALL deserialize as `FilterNode.GroupNode` with a nested `FilterSpec`

#### Scenario: Nested groups
- **WHEN** a filter contains groups nested 3 levels deep
- **THEN** the `JsonConverter` SHALL recursively deserialize all levels correctly
