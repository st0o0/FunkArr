## Purpose

ToApi()/ToMessage() extension method pattern for all message-to-API and API-to-message model transformations, replacing scattered mapper logic in endpoint classes.

## Requirements

### Requirement: ToApi extension methods for scoring
The API SHALL provide extension methods on Messages scoring types to map to API models: `ItemTrace.ToApi()`, `RuleTrace.ToApi()`, `FilterGroupTrace.ToApi()`, `RuleOutcome.ToApi()`. These SHALL live in `Extensions/ScoringMappingExtensions.cs`.

#### Scenario: ItemTrace mapped via extension
- **WHEN** a Messages.ItemTrace is returned from scoring
- **THEN** calling `.ToApi()` produces an `Api.Models.ItemTrace` with all fields mapped

### Requirement: ToApi extension methods for downloads
The API SHALL provide extension methods on Messages download types: `QueueResult.ToApi()`, `QueueItem.ToApi()`, `HistoryResult.ToApi()`, `HistoryItem.ToApi()`. These SHALL live in `Extensions/DownloadMappingExtensions.cs`.

#### Scenario: QueueResult mapped via extension
- **WHEN** a Messages.QueueResult is returned from the download manager
- **THEN** calling `.ToApi()` produces an `Api.Models.DownloadQueueResponse`

### Requirement: ToApi extension methods for mediathek
The API SHALL provide an extension method `MediathekItem.ToApi()` returning `Api.Models.MediathekSearchResult`. This SHALL live in `Extensions/MediathekMappingExtensions.cs`.

#### Scenario: MediathekItem mapped via extension
- **WHEN** a MediathekItem is returned from search
- **THEN** calling `.ToApi()` produces an `Api.Models.MediathekSearchResult`

### Requirement: ToApi extension methods for rulesets
The API SHALL provide extension methods: `RuleSetDetailResult.ToApi()`, `ScoringHistoryResult.ToApi()`, `ScoringDetailResult.ToApi()`. These SHALL live in `Extensions/RuleSetMappingExtensions.cs`.

#### Scenario: RuleSetDetailResult mapped via extension
- **WHEN** a RuleSetDetailResult is returned from the manager
- **THEN** calling `.ToApi()` produces an `Api.Models.RuleSetDetail`

### Requirement: ToMessage extension methods for test scoring
The API SHALL provide extension methods on API request types: `TestScoreRequest.ToMessage()` returning `(MatchingConfig, ScoreCandidate[])`, `RuleInput.ToMessage()` returning `MatchingRule?`. These SHALL live in `Extensions/TestScoreMappingExtensions.cs`. The extensions SHALL use shared `RuleInput` types with less mapping code since field/op/type are already enums.

#### Scenario: TestScoreRequest mapped via extension
- **WHEN** a TestScoreRequest is received from the frontend
- **THEN** calling `.ToMessage()` produces a MatchingConfig and ScoreCandidate array

#### Scenario: RuleInput.Strategy maps directly
- **WHEN** a `RuleInput` with `Strategy = IdentificationStrategy.TitleIncludes` is mapped
- **THEN** the resulting `IdentificationSpec` has `Strategy = IdentificationStrategy.TitleIncludes` without string parsing

#### Scenario: FilterNodeInput.Field maps directly
- **WHEN** a `FilterNodeInput` with `Field = FilterField.Duration` is mapped
- **THEN** the resulting `FilterCondition` has `Field = FilterField.Duration` without string parsing

### Requirement: TestRuleMapper deleted
The `TestRuleMapper.cs` file SHALL be deleted. Its functionality is replaced by the ToMessage() extension methods.

#### Scenario: No TestRuleMapper class exists
- **WHEN** the codebase is searched for TestRuleMapper
- **THEN** no file or class by that name exists

### Requirement: Endpoint classes are thin
Endpoint classes SHALL contain only route definitions, parameter binding, and actor communication. All model mapping SHALL use ToApi()/ToMessage() extension methods. No private/internal static mapper methods SHALL exist in endpoint classes.

#### Scenario: RuleSetApiEndpoints has no mapper methods
- **WHEN** RuleSetApiEndpoints.cs is inspected
- **THEN** it contains no methods named ToXxxModel, MapXxx, or similar

### Requirement: Shared GatewayTimeout helper
A shared static method SHALL replace the duplicated `GatewayTimeout()` methods in each endpoint class.

#### Scenario: Single GatewayTimeout definition
- **WHEN** the codebase is searched for GatewayTimeout
- **THEN** only one definition exists, used by all endpoint classes

### Requirement: Disk serialization helper
The API SHALL provide a serialization helper that serializes Create/Update request models to JSON suitable for disk storage, using `DefaultIgnoreCondition = WhenWritingNull`, `CamelCase` naming, and `WriteIndented = true`.

#### Scenario: Null fields omitted
- **WHEN** a request with `Aliases = null` is serialized for disk
- **THEN** the JSON output does not contain an `aliases` key

#### Scenario: Enum fields as strings in JSON
- **WHEN** a request with `Strategy = IdentificationStrategy.SeasonAndEpisodeNumber` is serialized for disk
- **THEN** the JSON contains `"strategy": "seasonAndEpisodeNumber"` (via JsonStringEnumConverter on the Messages enum)
