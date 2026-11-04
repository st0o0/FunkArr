## Purpose

Organization of API model records into feature subfolders for physical file grouping, with shared types separated and enums co-located with their owning feature.

## Requirements

### Requirement: Models organized in feature subfolders
All API model records SHALL be organized in `Models/{Feature}/` subfolders corresponding to endpoint feature areas. The namespace SHALL be `FunkArr.Api.Models.{Feature}`.

#### Scenario: Downloads models in subfolder
- **WHEN** the `Models/Downloads/` directory is inspected
- **THEN** it SHALL contain `DownloadQueueResponse.cs`, `DownloadHistoryResponse.cs`, `DownloadHistoryRequest.cs`, `DownloadSettingsResponse.cs`, `MoveRequest.cs`, `PriorityRequest.cs`, `SwapRequest.cs`

#### Scenario: RuleSets models in subfolder
- **WHEN** the `Models/RuleSets/` directory is inspected
- **THEN** it SHALL contain `RuleSetListResponse.cs`, `RuleSetDetailResponse.cs`, `CreateRuleSetRequest.cs`, `UpdateRuleSetRequest.cs`, `RuleSetBody.cs`, `FilterModels.cs`, `TitleRuleModels.cs`, `EnrichmentModels.cs`, `CreatedRuleSetResponse.cs`

#### Scenario: Scoring models in subfolder
- **WHEN** the `Models/Scoring/` directory is inspected
- **THEN** it SHALL contain `TestScoreRequest.cs`, `TestScoreResponse.cs`, `ScoringDetailResponse.cs`, `ScoringHistoryResponse.cs`

#### Scenario: Feed models in subfolder
- **WHEN** the `Models/Feed/` directory is inspected
- **THEN** it SHALL contain `FeedOverviewResponse.cs`

#### Scenario: Mediathek models in subfolder
- **WHEN** the `Models/Mediathek/` directory is inspected
- **THEN** it SHALL contain `MediathekSearchRequest.cs`, `MediathekSearchResponse.cs`

#### Scenario: System models in subfolder
- **WHEN** the `Models/System/` directory is inspected
- **THEN** it SHALL contain `SetupHealthCheck.cs`, `VersionResponse.cs`, `StorageStatusResponse.cs`, `CacheStatsResponse.cs`, `RoutesResponse.cs`, `VariantsResponse.cs`

#### Scenario: Setup models in subfolder
- **WHEN** the `Models/Setup/` directory is inspected
- **THEN** it SHALL contain `CreateArrResourceRequest.cs`, `ArrPayloads.cs`

### Requirement: Shared types in Models/Shared
Types used across multiple features SHALL reside in `Models/Shared/` with namespace `FunkArr.Api.Models.Shared`.

#### Scenario: Shared folder contents
- **WHEN** the `Models/Shared/` directory is inspected
- **THEN** it SHALL contain `MediaType.cs`, `OperationResult.cs`, `ErrorResponse.cs`
- **AND** `MediathekVariant` and `VariantPolicy` enums SHALL be in `Models/Shared/`

### Requirement: Enums.cs eliminated
The central `Enums.cs` file SHALL be deleted. Each enum SHALL live in the feature subfolder that owns it.

#### Scenario: No central Enums.cs
- **WHEN** `Models/Enums.cs` is searched for
- **THEN** it SHALL NOT exist

#### Scenario: Feature-scoped enums
- **WHEN** `FilterField`, `FilterOp`, `IdentificationStrategy`, `TitlePartType` are searched for
- **THEN** they SHALL be defined in `Models/RuleSets/`

#### Scenario: Download enums in Downloads folder
- **WHEN** `QueueStatus`, `QueuePhase`, `QueuePriority`, `HistoryStatus` are searched for
- **THEN** they SHALL be defined in `Models/Downloads/`

#### Scenario: Scoring enums in Scoring folder
- **WHEN** `MatchMethod`, `RuleOutcome`, `SearchSource` are searched for
- **THEN** they SHALL be defined in `Models/Scoring/`

### Requirement: Files separate request and response records
A model file SHALL contain either request records or response records, not both. Exception: a matched request/response pair for a single endpoint (e.g., `MoveRequest` for a POST returning `OperationResult`) MAY share a file.

#### Scenario: DownloadQueue.cs split
- **WHEN** the current `DownloadQueue.cs` (which contains `QueueStatus` enum + `DownloadQueueResponse` + `DownloadQueueItem` + `MoveRequest` + `PriorityRequest` + `SwapRequest`) is inspected
- **THEN** response records SHALL be in one file and request records in separate files

### Requirement: Model organization documented in AGENTS.md
`AGENTS.md` SHALL document the model organization convention: feature subfolders, shared types location, enum placement, and request/response file separation.

#### Scenario: AGENTS.md has model organization rules
- **WHEN** `AGENTS.md` is inspected
- **THEN** it SHALL contain rules for `Models/{Feature}/` subfolders, `Models/Shared/` for cross-feature types, and per-feature enum placement
