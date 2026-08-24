# TvSearchActor

## Purpose

Shard entity (one entity per `tvdbId`) that executes the full TV search pipeline: parallel resolution, matching, quality expansion, and scoring. Supports request coalescing and per-caller episode filtering.

## Requirements

### Requirement: Shard entity identity

TvSearchActor SHALL be a Cluster Sharding entity with `tvdbId` as the entity key. Each unique `tvdbId` MUST map to exactly one entity instance. The `Search` message SHALL implement `IShardedMessage` with `EntityKey => TvdbId.ToString()`. The entity SHALL use `ShardedMessageExtractor` instead of a dedicated `TvSearchActorMessageExtractor`. The namespace SHALL be `FunkArr.Search`.

#### Scenario: Entity activation by tvdbId
- **WHEN** a `TvSearchActor.Search` message with `TvdbId = 12345` arrives
- **THEN** the shard system SHALL extract entity key `"12345"` via `IShardedMessage.EntityKey` and route to the correct entity

#### Scenario: Search implements IShardedMessage
- **WHEN** `TvSearchActor.Search` is created with `TvdbId = 42`
- **THEN** its `EntityKey` property SHALL return `"42"`

### Requirement: Parallel resolution stage

TvSearchActor SHALL Ask SeriesResolver (for TVDB show info and episodes) and RuleSetActor (for rules) in parallel as the first pipeline stage.

#### Scenario: Parallel resolution
- **WHEN** a pipeline execution starts
- **THEN** the entity SHALL send Ask messages to both SeriesResolver and RuleSetActor concurrently and wait for both responses before proceeding

### Requirement: Mediathek fetch stage

After parallel resolution completes, TvSearchActor SHALL Ask MediathekGateway for mediathek items using the resolved show information.

#### Scenario: Mediathek query after resolution
- **WHEN** both SeriesResolver and RuleSetActor have responded
- **THEN** the entity SHALL Ask MediathekGateway for matching items

### Requirement: Inline matching stage

TvSearchActor SHALL perform matching inline using MatchingPipeline and RuleSetMatchingEngine with the fetched items, resolved rules, and episodes.

#### Scenario: Inline match execution
- **WHEN** MediathekGateway returns items
- **THEN** the entity SHALL apply MatchingPipeline and RuleSetMatchingEngine inline to produce matched results

### Requirement: Inline quality expansion stage

TvSearchActor SHALL expand qualities inline using UrlPatternAnalyzer and EstimateSize after matching.

#### Scenario: Quality expansion
- **WHEN** matching completes
- **THEN** the entity SHALL invoke UrlPatternAnalyzer and EstimateSize inline to expand quality variants for each matched result

### Requirement: Inline scoring stage

TvSearchActor SHALL score results inline as the final pipeline stage.

#### Scenario: Scoring
- **WHEN** quality expansion completes
- **THEN** the entity SHALL score each result inline and produce the final ranked result set

### Requirement: Result caching

TvSearchActor SHALL cache pipeline results per entity with a 55-minute TTL. Subsequent requests within the TTL MUST return the cached result without re-executing the pipeline.

#### Scenario: Cache hit within TTL
- **WHEN** a request arrives for a `tvdbId` whose cached result is less than 55 minutes old
- **THEN** the entity SHALL return the cached result without executing the pipeline

#### Scenario: Cache expiry
- **WHEN** a request arrives for a `tvdbId` whose cached result is 55 minutes old or older
- **THEN** the entity SHALL re-execute the full pipeline and cache the new result

### Requirement: Request coalescing

When a pipeline execution is already in progress for a `tvdbId`, concurrent requests for the same `tvdbId` SHALL be queued and answered with the pipeline result once it completes.

#### Scenario: Concurrent request coalescing
- **WHEN** a second request arrives for a `tvdbId` while a pipeline execution is already in progress
- **THEN** the second request SHALL be queued and answered with the same result when the in-progress pipeline completes

### Requirement: Per-caller episode filtering

TvSearchActor SHALL apply per-caller episode filtering based on the season and episode parameters in the request, returning only the episodes matching the caller's criteria.

#### Scenario: Season and episode filtering
- **WHEN** a `TvSearchActor.Search` specifies a season and/or episode number
- **THEN** the response SHALL contain only results matching the requested season and/or episode

#### Scenario: No filtering when unspecified
- **WHEN** a `TvSearchActor.Search` does not specify season or episode
- **THEN** the response SHALL contain all matched results

### Requirement: Passivation

TvSearchActor entities SHALL passivate after 120 minutes of idle time (no incoming messages).

#### Scenario: Idle passivation
- **WHEN** a TvSearchActor entity receives no messages for 120 minutes
- **THEN** the shard system SHALL passivate the entity to free resources
