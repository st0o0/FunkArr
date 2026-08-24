# TextSearchActor

## Purpose

Shard entity (one entity per query string) that executes the simplest search pipeline: mediathek fetch, matching, quality expansion, and scoring. No show resolution, no rules. Used by BrowseActor for RSS/browse feed population.

## Requirements

### Requirement: Shard entity identity

TextSearchActor SHALL be a Cluster Sharding entity with `query` as the entity key. Each unique query string MUST map to exactly one entity instance. The `Search` message SHALL implement `IShardedMessage` with `EntityKey => Query`. The entity SHALL use `ShardedMessageExtractor` instead of a dedicated `TextSearchActorMessageExtractor`. The namespace SHALL be `FunkArr.Search`.

#### Scenario: Entity activation by query
- **WHEN** a `TextSearchActor.Search` message with `Query = "tagesschau"` arrives
- **THEN** the shard system SHALL extract entity key `"tagesschau"` via `IShardedMessage.EntityKey` and route to the correct entity

#### Scenario: Search implements IShardedMessage
- **WHEN** `TextSearchActor.Search` is created with `Query = "tatort"`
- **THEN** its `EntityKey` property SHALL return `"tatort"`

### Requirement: Mediathek fetch stage

TextSearchActor SHALL Ask MediathekGateway for mediathek items using the query string as the first and only external call in the pipeline.

#### Scenario: Mediathek query
- **WHEN** a pipeline execution starts
- **THEN** the entity SHALL Ask MediathekGateway for items matching the query string

### Requirement: Inline matching stage

TextSearchActor SHALL perform matching inline using the generic matching path without rules and without show resolution.

#### Scenario: Generic match execution
- **WHEN** MediathekGateway returns items
- **THEN** the entity SHALL apply matching inline using the generic path (no rules, no show resolution) to produce matched results

### Requirement: Inline quality expansion stage

TextSearchActor SHALL expand qualities inline using UrlPatternAnalyzer and EstimateSize after matching.

#### Scenario: Quality expansion
- **WHEN** matching completes
- **THEN** the entity SHALL invoke UrlPatternAnalyzer and EstimateSize inline to expand quality variants

### Requirement: Inline scoring stage

TextSearchActor SHALL score results inline as the final pipeline stage.

#### Scenario: Scoring
- **WHEN** quality expansion completes
- **THEN** the entity SHALL score each result inline and produce the final ranked result set

### Requirement: Result caching

TextSearchActor SHALL cache pipeline results per entity with a 55-minute TTL. Subsequent requests within the TTL MUST return the cached result without re-executing the pipeline.

#### Scenario: Cache hit within TTL
- **WHEN** a request arrives for a query whose cached result is less than 55 minutes old
- **THEN** the entity SHALL return the cached result without executing the pipeline

#### Scenario: Cache expiry
- **WHEN** a request arrives for a query whose cached result is 55 minutes old or older
- **THEN** the entity SHALL re-execute the full pipeline and cache the new result

### Requirement: RSS feed support

TextSearchActor SHALL handle empty query strings as valid search requests. An empty query (`""`) SHALL produce the latest MediathekViewWeb content (MediathekViewWeb returns newest entries across all broadcasters when called with `Queries = []`). The entity SHALL cache and passivate identically to any other query.

#### Scenario: Empty query returns latest content
- **WHEN** a `Search("")` message arrives at TextSearchActor
- **THEN** the entity SHALL ask MediathekGatewayActor with an empty search term, which sends `Queries = []` to MediathekViewWeb, returning the latest content sorted by timestamp descending

#### Scenario: Empty query caching
- **WHEN** multiple `Search("")` requests arrive within 55 minutes
- **THEN** the entity SHALL return the cached result from the first request without re-querying MediathekViewWeb

#### Scenario: Empty query sharding
- **WHEN** a `Search("")` message arrives
- **THEN** `IShardedMessage.EntityKey` SHALL return `""`, creating a single shared entity for all empty-query requests

### Requirement: Passivation

TextSearchActor entities SHALL passivate after 60 minutes of idle time (no incoming messages).

#### Scenario: Idle passivation
- **WHEN** a TextSearchActor entity receives no messages for 60 minutes
- **THEN** the shard system SHALL passivate the entity to free resources
