## ADDED Requirements

### Requirement: Command messages use bare verb phrases

Command messages (trigger an action, expect a response) SHALL use bare verb phrases without a `Command` suffix. The verb SHALL clearly indicate the action. The only exception is `SearchCommand`, the public entry point, which keeps its suffix to distinguish from the generic concept.

#### Scenario: Command naming pattern

- **WHEN** a new command message is created
- **THEN** it SHALL be named as `VerbNoun` (e.g., `StartDownload`, `RegisterRuleSet`, `ScoreItems`, `ResolveMovie`)
- **AND** it SHALL NOT use a `Command` suffix unless it is the domain's public entry point and the bare name would be ambiguous

#### Scenario: Internal routing messages drop Command suffix

- **WHEN** an internal shard-routed message exists that previously used a `Command` suffix
- **THEN** it SHALL drop the suffix (e.g., `TvSearchCommand` → `TvSearch`, `MovieSearchCommand` → `MovieSearch`)

### Requirement: Query messages use Query prefix

Query messages (request data, always expect a response) SHALL use a `Query` prefix followed by the noun describing what is queried.

#### Scenario: Query naming pattern

- **WHEN** a new query message is created
- **THEN** it SHALL be named as `QueryNoun` (e.g., `QueryQueue`, `QueryHistory`, `QueryRuleSetDetail`, `QueryScoringHistory`, `QueryCacheStats`)

### Requirement: Command response messages use past-tense or Completed/Failed

Response messages for commands SHALL use either past-tense verbs or `*Completed`/`*Failed` suffixes. Failure responses SHALL always use the `*Failed` suffix and carry both `Reason` (string) and `Cause` (Exception?) properties.

#### Scenario: Success response naming

- **WHEN** a success response to a command is created
- **THEN** it SHALL use past-tense (`DownloadAdded`, `RuleSetResolved`) or `*Completed` (`SearchCompleted`, `ScoreCompleted`)

#### Scenario: Failure response naming

- **WHEN** a failure response to a command is created
- **THEN** it SHALL use the `*Failed` suffix (e.g., `SearchFailed`, `ScoringFailed`, `MediathekQueryFailed`)
- **AND** it SHALL carry `Reason` (string) and `Cause` (Exception?) properties

### Requirement: Query response messages use Result suffix

Response messages for queries SHALL use a `*Result` suffix.

#### Scenario: Query response naming

- **WHEN** a new query response message is created
- **THEN** it SHALL be named as `NounResult` (e.g., `QueueResult`, `HistoryResult`, `WorkerStatusResult`, `ScoringHistoryResult`, `CacheStatsResult`)

### Requirement: Internal signal messages use bare phrases

Internal fire-and-forget messages (no response expected) SHALL use bare verb or noun phrases without any suffix.

#### Scenario: Internal signal naming

- **WHEN** a fire-and-forget actor-to-actor message is created
- **THEN** it SHALL use a bare phrase (e.g., `SlotFree`, `RecordDownload`, `RemoveHistoryEntry`, `RecordScoringResult`)

### Requirement: Data carrier records use descriptive nouns

Data carrier records that are nested within messages (not protocol messages themselves) SHALL use descriptive noun phrases.

#### Scenario: Data carrier naming

- **WHEN** a data carrier record nested in a message is created
- **THEN** it SHALL use a descriptive noun (e.g., `MediathekItem`, `ScoreCandidate`, `ScoredItem`, `QueueItem`, `HistoryItem`, `SearchResultItem`)

### Requirement: Each domain has a response marker interface

Each domain namespace in FunkArr.Messages SHALL define a response marker interface `I{Domain}Response` that all response types for that domain implement.

#### Scenario: All domains have marker interfaces

- **WHEN** FunkArr.Messages is compiled
- **THEN** the following marker interfaces SHALL exist: `ISearchResponse`, `IRuleSetResponse`, `IMediathekResponse`, `IScoringResponse`, `IDownloadResponse`, `IMovieResolutionResponse`, `IEpisodeResolutionResponse`
