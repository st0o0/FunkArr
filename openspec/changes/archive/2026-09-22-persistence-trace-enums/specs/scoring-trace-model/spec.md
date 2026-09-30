## ADDED Requirements

### Requirement: PersistedFilterGroupOp enum

The system SHALL define a `PersistedFilterGroupOp` enum in `FunkArr.Persistence.Events.ScoringHistory` with members matching the order of `FilterGroupOp`: `All`, `Any`, `Not`. `PersistedFilterGroupTrace.Operator` SHALL use this enum instead of `string`.

#### Scenario: PersistedFilterGroupOp members match FilterGroupOp order
- **WHEN** `PersistedFilterGroupOp` and `FilterGroupOp` are compared
- **THEN** each member at index N in `PersistedFilterGroupOp` SHALL correspond to the member at index N in `FilterGroupOp`

#### Scenario: PersistedFilterGroupTrace uses enum
- **WHEN** `PersistedFilterGroupTrace` is inspected
- **THEN** its `Operator` field SHALL be of type `PersistedFilterGroupOp`, not `string`

### Requirement: PersistedIdentificationStrategy enum

The system SHALL define a `PersistedIdentificationStrategy` enum in `FunkArr.Persistence.Events.ScoringHistory` with members matching the order of `IdentificationStrategy`: `SeasonAndEpisodeNumber`, `AbsoluteEpisodeNumber`, `TitleExact`, `TitleIncludes`, `AirdateExtraction`. `PersistedIdentificationTrace.Strategy` SHALL use this nullable enum instead of `string?`.

#### Scenario: PersistedIdentificationStrategy members match IdentificationStrategy order
- **WHEN** `PersistedIdentificationStrategy` and `IdentificationStrategy` are compared
- **THEN** each member at index N SHALL correspond to the member at index N in the domain enum

#### Scenario: PersistedIdentificationTrace.Strategy uses enum
- **WHEN** `PersistedIdentificationTrace` is inspected
- **THEN** its `Strategy` field SHALL be of type `PersistedIdentificationStrategy?`, not `string?`

### Requirement: PersistedIdentificationFailureReason enum

The system SHALL define a `PersistedIdentificationFailureReason` enum in `FunkArr.Persistence.Events.ScoringHistory` with members matching the order of `IdentificationFailureReason`: `UnknownStrategy`, `SeasonPatternNotMatched`, `NoEpisodePatternConfigured`, `EpisodePatternNotMatched`, `NoTitlePartsConfigured`, `TitlePartRegexNotMatched`, `TitleDoesNotMatch`, `NoDateFoundInTitle`. `PersistedIdentificationTrace.Detail` SHALL use this nullable enum instead of `string?`.

#### Scenario: PersistedIdentificationFailureReason members match IdentificationFailureReason order
- **WHEN** `PersistedIdentificationFailureReason` and `IdentificationFailureReason` are compared
- **THEN** each member at index N SHALL correspond to the member at index N in the domain enum

#### Scenario: PersistedIdentificationTrace.Detail uses enum
- **WHEN** `PersistedIdentificationTrace` is inspected
- **THEN** its `Detail` field SHALL be of type `PersistedIdentificationFailureReason?`, not `string?`

### Requirement: PersistenceMapping uses int-cast pattern for trace enums

The `PersistenceMapping` SHALL convert between domain and persisted trace enums using `(int)` cast, consistent with `RuleOutcome`, `MatchMethod`, and `SearchSource`. It SHALL NOT use `Enum.Parse<>()` or `.ToString()` for these conversions.

#### Scenario: FilterGroupOp mapped via int cast
- **WHEN** a `FilterGroupTrace` with `Operator=FilterGroupOp.Any` is mapped to persistence
- **THEN** the result SHALL have `Operator=PersistedFilterGroupOp.Any` via `(PersistedFilterGroupOp)(int)` cast

#### Scenario: IdentificationStrategy mapped via int cast
- **WHEN** an `IdentificationTrace` with `Strategy=IdentificationStrategy.TitleExact` is mapped to persistence
- **THEN** the result SHALL have `Strategy=PersistedIdentificationStrategy.TitleExact` via `(PersistedIdentificationStrategy)(int)` cast

#### Scenario: Null strategy preserved
- **WHEN** an `IdentificationTrace` with `Strategy=null` is mapped to persistence
- **THEN** the result SHALL have `Strategy=null`

#### Scenario: IdentificationFailureReason mapped via int cast
- **WHEN** an `IdentificationTrace` with `Detail=IdentificationFailureReason.NoDateFoundInTitle` is mapped to persistence
- **THEN** the result SHALL have `Detail=PersistedIdentificationFailureReason.NoDateFoundInTitle` via `(PersistedIdentificationFailureReason)(int)` cast
