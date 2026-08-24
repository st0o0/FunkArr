# MovieSearchPipeline

Shard entity (one entity per `imdbId` or `query`) that executes the movie search pipeline: TMDB resolution, mediathek fetch, matching, quality expansion, and scoring. Supports fallback to original title.

## ADDED Requirements

### Requirement: Shard entity identity

MovieSearchPipeline SHALL be a Cluster Sharding entity with `imdbId` or `query` as the entity key. Each unique key MUST map to exactly one entity instance.

#### Scenario: Entity activation by imdbId
- **WHEN** a `MovieSearchRequest` arrives with an `imdbId` that has no active entity
- **THEN** the shard system SHALL activate a new MovieSearchPipeline entity with `imdbId` as the entity key

#### Scenario: Entity activation by query
- **WHEN** a `MovieSearchRequest` arrives without an `imdbId` but with a `query` that has no active entity
- **THEN** the shard system SHALL activate a new MovieSearchPipeline entity with `query` as the entity key

### Requirement: Movie resolution stage

MovieSearchPipeline SHALL Ask MovieResolver for TMDB movie information as the first pipeline stage.

#### Scenario: TMDB resolution
- **WHEN** a pipeline execution starts
- **THEN** the entity SHALL Ask MovieResolver for movie information and wait for the response before proceeding

### Requirement: Mediathek fetch stage

After movie resolution completes, MovieSearchPipeline SHALL Ask MediathekGateway for mediathek items using the resolved movie information.

#### Scenario: Mediathek query after resolution
- **WHEN** MovieResolver has responded with movie information
- **THEN** the entity SHALL Ask MediathekGateway for matching items

### Requirement: Inline matching stage

MovieSearchPipeline SHALL perform matching inline using the generic matching path without rules.

#### Scenario: Generic match execution
- **WHEN** MediathekGateway returns items
- **THEN** the entity SHALL apply matching inline using the generic path (no rules) to produce matched results

### Requirement: Inline quality expansion stage

MovieSearchPipeline SHALL expand qualities inline using UrlPatternAnalyzer and EstimateSize after matching.

#### Scenario: Quality expansion
- **WHEN** matching completes
- **THEN** the entity SHALL invoke UrlPatternAnalyzer and EstimateSize inline to expand quality variants

### Requirement: Inline scoring stage

MovieSearchPipeline SHALL score results inline as the final pipeline stage.

#### Scenario: Scoring
- **WHEN** quality expansion completes
- **THEN** the entity SHALL score each result inline and produce the final ranked result set

### Requirement: Original title fallback

When a TMDB-resolved title yields no results from MediathekGateway, MovieSearchPipeline SHALL retry the mediathek query using the movie's `originalTitle`.

#### Scenario: Fallback to original title
- **WHEN** the mediathek query using the TMDB-resolved title returns zero items and the movie has an `originalTitle` different from the resolved title
- **THEN** the entity SHALL retry the MediathekGateway query using `originalTitle`

#### Scenario: No fallback when titles match
- **WHEN** the mediathek query returns zero items but the `originalTitle` is identical to the resolved title
- **THEN** the entity SHALL NOT retry and SHALL return an empty result set

### Requirement: Result caching

MovieSearchPipeline SHALL cache pipeline results per entity with a 55-minute TTL. Subsequent requests within the TTL MUST return the cached result without re-executing the pipeline.

#### Scenario: Cache hit within TTL
- **WHEN** a request arrives for an entity whose cached result is less than 55 minutes old
- **THEN** the entity SHALL return the cached result without executing the pipeline

#### Scenario: Cache expiry
- **WHEN** a request arrives for an entity whose cached result is 55 minutes old or older
- **THEN** the entity SHALL re-execute the full pipeline and cache the new result

### Requirement: Passivation

MovieSearchPipeline entities SHALL passivate after 60 minutes of idle time (no incoming messages).

#### Scenario: Idle passivation
- **WHEN** a MovieSearchPipeline entity receives no messages for 60 minutes
- **THEN** the shard system SHALL passivate the entity to free resources
