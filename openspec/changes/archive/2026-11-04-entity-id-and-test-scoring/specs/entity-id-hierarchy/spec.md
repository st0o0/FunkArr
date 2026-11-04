## ADDED Requirements

### Requirement: IWith*Id interfaces inherit Terp entity ID interfaces

Each `IWith*Id` interface in FunkArr.Messages SHALL inherit from `Terp.Akka.IWithEntityId`. Interfaces whose ID property is a `Guid` SHALL additionally inherit from `Terp.IWithId`. Both base interfaces SHALL be satisfied via explicit default interface implementations, keeping `EntityId` and `Id` invisible in normal usage.

#### Scenario: IWithDownloadId inherits IWithId and IWithEntityId

- **WHEN** `IWithDownloadId` is defined
- **THEN** it SHALL inherit `IWithId` and `IWithEntityId`, with `IWithId.Id => DownloadId` and `IWithEntityId.EntityId => DownloadId.ToString()`

#### Scenario: IWithSearchId inherits IWithId and IWithEntityId

- **WHEN** `IWithSearchId` is defined
- **THEN** it SHALL inherit `IWithId` and `IWithEntityId`, with `IWithId.Id => SearchId` and `IWithEntityId.EntityId => SearchId.ToString()`

#### Scenario: IWithRuleSetId inherits only IWithEntityId

- **WHEN** `IWithRuleSetId` is defined
- **THEN** it SHALL inherit `IWithEntityId` (not `IWithId`, since its ID is a string), with `IWithEntityId.EntityId => RuleSetId`

#### Scenario: EntityId and Id are explicit interface implementations

- **WHEN** a message record implementing `IWithDownloadId` is used in code
- **THEN** only `DownloadId` SHALL be visible in IntelliSense; `EntityId` and `Id` SHALL only be accessible via explicit cast to `IWithEntityId` or `IWithId`

#### Scenario: Message records require no changes

- **WHEN** an existing record like `InitDownload(Guid DownloadId, ...) : IWithDownloadId` is compiled after the interface change
- **THEN** it SHALL compile without modification, because the default implementations satisfy the base interfaces

### Requirement: One IWith*Id interface per entity region

Each entity region (shard or local) SHALL have exactly one dedicated `IWith*Id` routing interface. Messages targeting different regions SHALL NOT share the same `IWith*Id` interface, even when the underlying ID value is semantically the same.

#### Scenario: History messages use IWithHistoryId, not IWithRuleSetId

- **WHEN** `RecordHistory`, `QueryScoringHistory`, `QueryScoringStats`, or `QueryScoringDetail` are defined
- **THEN** they SHALL implement `IWithHistoryId` (not `IWithRuleSetId`)

#### Scenario: RuleSet messages continue using IWithRuleSetId

- **WHEN** messages targeting the RuleSet shard region are defined
- **THEN** they SHALL implement `IWithRuleSetId`

### Requirement: IWithHistoryId interface for History domain

FunkArr.Messages SHALL define `IWithHistoryId` inheriting from `IWithEntityId`. It SHALL expose a `string HistoryId` property and provide `IWithEntityId.EntityId => HistoryId` as a default implementation.

#### Scenario: IWithHistoryId structure

- **WHEN** `IWithHistoryId` is defined
- **THEN** it SHALL have a `string HistoryId` property and inherit `IWithEntityId` with `EntityId => HistoryId`

#### Scenario: History messages carry HistoryId

- **WHEN** `RecordHistory` is defined with a `string HistoryId` parameter
- **THEN** it SHALL implement `IWithHistoryId` and the `HistoryId` value SHALL be the ruleset identifier string

### Requirement: Domain-internal correlation exception

Within a single domain, one `IWith*Id` MAY span multiple shard regions when the ID is a correlation ID for a multi-actor flow. This exception SHALL NOT apply across domain boundaries.

#### Scenario: IWithSearchId spans Search domain regions

- **WHEN** `SearchSeries` targets `ITvSearchRegion` and `EvaluateSeries` targets `ITvEvaluationRegion`
- **THEN** both MAY implement `IWithSearchId` because they are within the Search domain and the ID correlates a single search operation

#### Scenario: Cross-domain sharing is prohibited

- **WHEN** two messages target regions in different domains (e.g., RuleSet and History)
- **THEN** they SHALL NOT share the same `IWith*Id` interface, even if the ID value is the same
