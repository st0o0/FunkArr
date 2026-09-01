## ADDED Requirements

### Requirement: TvSearchWorker is a sharded entity

The TvSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search, responds, and passivates.

#### Scenario: Worker creation and passivation

- **WHEN** the TvSearch ShardRegion receives a message with a new SearchId
- **THEN** a new TvSearchWorker instance SHALL be created, process the search, and passivate after responding

### Requirement: TvSearchWorker orchestrates search pipeline

The TvSearchWorker SHALL chain MediathekViewWeb query and MatchMagic scoring using PipeTo, without blocking the actor thread.

#### Scenario: Successful TV search

- **WHEN** the worker receives a TvSearchCommand
- **THEN** it SHALL build a MediathekQuery with topic-based search and duration minimum of 300 seconds, Ask the MediathekViewWebManager, then Ask the MatchMagicManager for scoring, and Tell the Sender with a SearchCompleted containing scored items

#### Scenario: MediathekViewWeb query fails

- **WHEN** the MediathekViewWebManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

#### Scenario: MatchMagic scoring fails

- **WHEN** the MatchMagicManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

### Requirement: TvSearchWorker builds TV-specific queries

The worker SHALL construct MediathekQuery messages tailored for TV content.

#### Scenario: Query with show name only

- **WHEN** a TvSearchCommand has a Query but no Season or Episode
- **THEN** the MediathekQuery SHALL search the topic field for the query string with duration_min=300

#### Scenario: Query with season and episode

- **WHEN** a TvSearchCommand has Query, Season, and Episode
- **THEN** the MediathekQuery SHALL search the topic field for the query and the title field for episode-related patterns

#### Scenario: Query with only season

- **WHEN** a TvSearchCommand has Query and Season but no Episode
- **THEN** the MediathekQuery SHALL search the topic field for the query string

### Requirement: TvSearchWorker queries MediathekViewWeb with pagination

The TvSearchWorker SHALL use the Limit and Offset values from the incoming TvSearchCommand when constructing the MediathekQuery. If Limit is null, the worker SHALL use a default Size of 50. If Offset is null, the worker SHALL use 0.

#### Scenario: Search with explicit limit and offset

- **WHEN** a TvSearchCommand with Limit=100 and Offset=20 is received
- **THEN** the MediathekQuery SHALL use Size=100 and Offset=20

#### Scenario: Search with null pagination (defaults)

- **WHEN** a TvSearchCommand with Limit=null and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=50 and Offset=0

#### Scenario: Search with only limit specified

- **WHEN** a TvSearchCommand with Limit=25 and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=25 and Offset=0
