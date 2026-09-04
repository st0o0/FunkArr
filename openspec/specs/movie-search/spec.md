## ADDED Requirements

### Requirement: MovieSearchWorker is a sharded entity

The MovieSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search, responds, and passivates.

#### Scenario: Worker creation and passivation

- **WHEN** the MovieSearch ShardRegion receives a message with a new SearchId
- **THEN** a new MovieSearchWorker instance SHALL be created, process the search, and passivate after responding

### Requirement: MovieSearchWorker orchestrates search pipeline

The MovieSearchWorker SHALL chain MediathekViewWeb query and MatchMagic scoring using PipeTo, without blocking the actor thread. When only IDs are provided (no query text), the worker SHALL resolve the topic from the RuleSet domain first.

#### Scenario: Successful movie search with query text

- **WHEN** the worker receives a MovieSearchCommand with Query="Das Boot"
- **THEN** it SHALL build a MediathekQuery with title+topic search and duration minimum of 3600 seconds, Ask the MediathekViewWebManager, then Ask the MatchMagicManager for scoring, and Tell the Sender with a SearchCompleted containing scored items

#### Scenario: ImdbId-only movie search

- **WHEN** the worker receives a MovieSearchCommand with Query=null and ImdbId="tt0806910"
- **THEN** it SHALL Ask the RuleSetResolver with ResolveRuleSet(null, ImdbId: "tt0806910"), use the resolved topic to build a MediathekQuery with title+topic search and duration minimum 3600, then proceed with standard scoring

#### Scenario: TmdbId-only movie search

- **WHEN** the worker receives a MovieSearchCommand with Query=null and TmdbId=550
- **THEN** it SHALL Ask the RuleSetResolver with ResolveRuleSet(null, TmdbId: 550), use the resolved topic to query MVW

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the worker receives a MovieSearchCommand with Query=null and ImdbId="tt9999999" and no ruleset maps that ID
- **THEN** the worker SHALL Tell the Sender with SearchCompleted(SearchId, Items: [], Total: 0)

#### Scenario: No query and no IDs

- **WHEN** the worker receives a MovieSearchCommand with Query=null and ImdbId=null and TmdbId=null
- **THEN** the worker SHALL Tell the Sender with SearchFailed(SearchId, "Movie search requires a query or media ID")

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a MovieSearchCommand with Query="Das Boot" and ImdbId="tt1234567"
- **THEN** the worker SHALL use the text-based flow and set ImdbId="tt1234567" on each SearchResultItem

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

#### Scenario: Query without title but with IDs

- **WHEN** a MovieSearchCommand has no Query but has ImdbId or TmdbId
- **THEN** the worker SHALL resolve the topic via ID-based lookup in the RuleSetResolver, then query MVW with the resolved topic

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

### Requirement: MovieSearchWorker movie resolution stage

After receiving ScoreCompleted, the MovieSearchWorker SHALL check if the search has an ImdbId or TmdbId. If so, the worker SHALL construct MovieCandidates from the matched scored items and Ask the MetadataResolver to resolve the movie identity. On success, the worker SHALL enrich the SearchResultItems with validated title, year, and resolution metadata before calling ToScoredResult.

#### Scenario: Movie resolution with TMDB ID

- **WHEN** a movie search with TmdbId=550 produces matched items
- **THEN** the worker SHALL construct MovieCandidates and Ask MetadataResolver with ResolveMovie(TmdbId=550, ...)

#### Scenario: Movie resolution with IMDB ID

- **WHEN** a movie search with ImdbId="tt0806910" produces matched items
- **THEN** the worker SHALL construct MovieCandidates and Ask MetadataResolver with ResolveMovie(ImdbId="tt0806910", ...)

#### Scenario: No movie ID available

- **WHEN** a movie search has no ImdbId and no TmdbId (query-only search)
- **THEN** the worker SHALL skip movie resolution

#### Scenario: Resolution succeeds

- **WHEN** the MetadataResolver responds with MoviesResolved
- **THEN** the worker SHALL enrich the release title with validated year and title, and set ResolutionConfidence/ResolutionStrategy on the SearchResultItem

#### Scenario: Resolution fails

- **WHEN** the MetadataResolver responds with MovieResolutionFailed
- **THEN** the worker SHALL proceed with ToScoredResult using existing metadata (graceful fallback)

#### Scenario: Resolution times out

- **WHEN** the MetadataResolver Ask times out
- **THEN** the worker SHALL proceed with ToScoredResult using existing metadata

### Requirement: MovieSearchWorker uses IMetadataResolver

The MovieSearchWorker SHALL resolve the MetadataResolver singleton via `Context.GetActor<IMetadataResolver>()`.

#### Scenario: Actor resolution

- **WHEN** MovieSearchWorker is constructed
- **THEN** it SHALL resolve `IMetadataResolver` for movie resolution requests
