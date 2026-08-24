# TextSearchPipeline

Shard entity (one entity per query string) that executes the simplest search pipeline: mediathek fetch, matching, quality expansion, and scoring. No show resolution, no rules. Used by RssFeedCoordinator for RSS feed population.

## ADDED Requirements

### Requirement: Shard entity identity

TextSearchPipeline SHALL be a Cluster Sharding entity with `query` as the entity key. Each unique query string MUST map to exactly one entity instance.

#### Scenario: Entity activation by query
- **WHEN** a `TextSearchRequest` arrives with a `query` that has no active entity
- **THEN** the shard system SHALL activate a new TextSearchPipeline entity with `query` as the entity key

### Requirement: Mediathek fetch stage

TextSearchPipeline SHALL Ask MediathekGateway for mediathek items using the query string as the first and only external call in the pipeline.

#### Scenario: Mediathek query
- **WHEN** a pipeline execution starts
- **THEN** the entity SHALL Ask MediathekGateway for items matching the query string

### Requirement: Inline matching stage

TextSearchPipeline SHALL perform matching inline using the generic matching path without rules and without show resolution.

#### Scenario: Generic match execution
- **WHEN** MediathekGateway returns items
- **THEN** the entity SHALL apply matching inline using the generic path (no rules, no show resolution) to produce matched results

### Requirement: Inline quality expansion stage

TextSearchPipeline SHALL expand qualities inline using UrlPatternAnalyzer and EstimateSize after matching.

#### Scenario: Quality expansion
- **WHEN** matching completes
- **THEN** the entity SHALL invoke UrlPatternAnalyzer and EstimateSize inline to expand quality variants

### Requirement: Inline scoring stage

TextSearchPipeline SHALL score results inline as the final pipeline stage.

#### Scenario: Scoring
- **WHEN** quality expansion completes
- **THEN** the entity SHALL score each result inline and produce the final ranked result set

### Requirement: Result caching

TextSearchPipeline SHALL cache pipeline results per entity with a 55-minute TTL. Subsequent requests within the TTL MUST return the cached result without re-executing the pipeline.

#### Scenario: Cache hit within TTL
- **WHEN** a request arrives for a query whose cached result is less than 55 minutes old
- **THEN** the entity SHALL return the cached result without executing the pipeline

#### Scenario: Cache expiry
- **WHEN** a request arrives for a query whose cached result is 55 minutes old or older
- **THEN** the entity SHALL re-execute the full pipeline and cache the new result

### Requirement: RSS feed support

TextSearchPipeline MUST be usable by RssFeedCoordinator for RSS feed population.

#### Scenario: RssFeedCoordinator usage
- **WHEN** RssFeedCoordinator sends a `TextSearchRequest` to populate an RSS feed
- **THEN** the TextSearchPipeline entity SHALL process the request identically to any other text search request

### Requirement: Passivation

TextSearchPipeline entities SHALL passivate after 60 minutes of idle time (no incoming messages).

#### Scenario: Idle passivation
- **WHEN** a TextSearchPipeline entity receives no messages for 60 minutes
- **THEN** the shard system SHALL passivate the entity to free resources
