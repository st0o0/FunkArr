## ADDED Requirements

### Requirement: MovieSearchWorker is a sharded entity

The MovieSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search, responds, and passivates.

#### Scenario: Worker creation and passivation

- **WHEN** the MovieSearch ShardRegion receives a message with a new SearchId
- **THEN** a new MovieSearchWorker instance SHALL be created, process the search, and passivate after responding

### Requirement: MovieSearchWorker orchestrates search pipeline

The MovieSearchWorker SHALL chain MediathekViewWeb query and MatchMagic scoring using PipeTo, without blocking the actor thread.

#### Scenario: Successful movie search

- **WHEN** the worker receives a MovieSearchCommand
- **THEN** it SHALL build a MediathekQuery with title+topic search and duration minimum of 3600 seconds, Ask the MediathekViewWebManager, then Ask the MatchMagicManager for scoring, and Tell the Sender with a SearchCompleted containing scored items

#### Scenario: MediathekViewWeb query fails

- **WHEN** the MediathekViewWebManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

#### Scenario: MatchMagic scoring fails

- **WHEN** the MatchMagicManager Ask times out or returns an error
- **THEN** the worker SHALL Tell the Sender with a SearchFailed and passivate

### Requirement: MovieSearchWorker builds movie-specific queries

The worker SHALL construct MediathekQuery messages tailored for movie content.

#### Scenario: Query with title only

- **WHEN** a MovieSearchCommand has a Query
- **THEN** the MediathekQuery SHALL search the title and topic fields for the query string with duration_min=3600

#### Scenario: Query without title

- **WHEN** a MovieSearchCommand has no Query but has ImdbId or TmdbId
- **THEN** the worker SHALL respond with SearchFailed because MediathekViewWeb does not support ID-based lookup

### Requirement: MovieSearchWorker queries MediathekViewWeb with pagination

The MovieSearchWorker SHALL use the Limit and Offset values from the incoming MovieSearchCommand when constructing the MediathekQuery. If Limit is null, the worker SHALL use a default Size of 50. If Offset is null, the worker SHALL use 0.

#### Scenario: Search with explicit limit and offset

- **WHEN** a MovieSearchCommand with Limit=100 and Offset=20 is received
- **THEN** the MediathekQuery SHALL use Size=100 and Offset=20

#### Scenario: Search with null pagination (defaults)

- **WHEN** a MovieSearchCommand with Limit=null and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=50 and Offset=0

#### Scenario: Search with only limit specified

- **WHEN** a MovieSearchCommand with Limit=25 and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=25 and Offset=0
