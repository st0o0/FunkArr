## Purpose

Movie search pipeline orchestration - the MovieSearchWorker coordinates Mediathek queries, RuleSet resolution, MatchMagic scoring, and movie enrichment using Become-based phases and Ask+PipeTo communication.

## Requirements

### Requirement: MovieSearchWorker is a sharded entity

The MovieSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search and responds. The worker SHALL be passivated automatically via `PassivateIdleEntityAfter` on the shard configuration - no manual Passivate calls.

#### Scenario: Worker creation and auto-passivation

- **WHEN** the MovieSearch ShardRegion receives a message with a new SearchId
- **THEN** a new MovieSearchWorker instance SHALL be created, process the search, respond, and be passivated automatically after 30 seconds of idle time

### Requirement: MovieSearchWorker orchestrates search pipeline

The MovieSearchWorker SHALL use Become-based phase transitions and Ask+PipeTo for all inter-actor communication. The worker SHALL NOT implement IWithTimers. The worker SHALL capture the original Sender as `ReplyTo` in state when the SearchMovie command arrives. All replies SHALL use `_state.ReplyTo.Tell(...)`. All outgoing messages SHALL use `Ask<XxxResponse>(request, timeout).PipeTo(Self, failure: ex => new XxxFailed(ex))`. The PipeTo failure lambda SHALL map Ask timeouts and transport errors to the domain-owned Failed record type. Each Become state SHALL handle exactly two message types: XxxCompleted and XxxFailed. The worker SHALL NOT define any private timeout record types. The worker SHALL NOT use Timers.StartSingleTimer or Timers.Cancel.

#### Scenario: Successful movie search with query text

- **WHEN** the worker receives a SearchMovie with Query="Das Boot"
- **THEN** it SHALL call _state.Init(cmd, Sender), call _state.TryGetMediathekQuery to get the query message, Ask the MediathekViewWebManager with a 15-second timeout, PipeTo Self with failure mapped to QueryMediathekFailed, and Become(Querying)

#### Scenario: ImdbId-only movie search

- **WHEN** the worker receives a SearchMovie with Query=null and ImdbId="tt0806910"
- **THEN** it SHALL call _state.Init(cmd, Sender), call _state.TryGetRuleSetRequest to get a ResolveRuleSet message, Ask the RuleSetResolver with a 5-second timeout, PipeTo Self with failure mapped to RuleSetFailed, and Become(ResolvingRuleSet)

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the RuleSetResolver responds with RuleSetFailed (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL Reply with _state.ToSearchCompleted()

#### Scenario: Become-based phase transitions

- **WHEN** the worker transitions between pipeline phases
- **THEN** it SHALL use Become to switch handler sets so that each phase handles exactly XxxCompleted and XxxFailed messages

#### Scenario: Scoring phase transition

- **WHEN** scoring is needed after RuleSet resolution or query completion
- **THEN** the worker SHALL call _state.TryGetScoringRequest, Ask the ScoringManager with a 10-second timeout, PipeTo Self with failure mapped to ScoringFailed, and Become(Scoring)

#### Scenario: Enrichment phase transition

- **WHEN** ScoreCompleted is received and _state.TryGetEnrichmentRequest returns true
- **THEN** the worker SHALL call _state.Apply(scored), Ask the EnrichmentManager with a 10-second timeout, PipeTo Self with failure mapped to EnrichMoviesFailed, and Become(Enriching)

#### Scenario: Scoring completes without enrichment needed

- **WHEN** ScoreCompleted is received and _state.TryGetEnrichmentRequest returns false
- **THEN** the worker SHALL call _state.Apply(scored), tell HistoryRegion with RecordHistory (scoring-only ItemTraces), and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment succeeds

- **WHEN** EnrichMoviesCompleted is received
- **THEN** the worker SHALL call _state.Apply(enriched), merge enrichment into ItemTraces, tell HistoryRegion with RecordHistory, and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment fails

- **WHEN** EnrichMoviesFailed is received (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL tell HistoryRegion with RecordHistory (scoring-only ItemTraces) and Reply with _state.ToSearchCompleted() using existing metadata

#### Scenario: MediathekViewWeb query fails

- **WHEN** QueryMediathekFailed is received (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL Reply with SearchMovieFailed(SearchId, cause)

#### Scenario: MatchMagic scoring fails

- **WHEN** ScoringFailed is received (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL Reply with SearchMovieFailed(SearchId, cause)

#### Scenario: No query and no IDs

- **WHEN** the worker receives a SearchMovie with Query=null and ImdbId=null and TmdbId=null
- **THEN** the worker SHALL Reply with SearchMovieFailed(SearchId, cause)

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a SearchMovie with Query="Das Boot" and ImdbId="tt1234567"
- **THEN** the state's BaseIdentity SHALL include ImdbId="tt1234567", which flows into all EnrichedItems and SearchResultItems

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

After receiving ScoreCompleted, the MovieSearchWorker SHALL call _state.Apply(scored) and then _state.TryGetEnrichmentRequest. The state SHALL decide if enrichment is needed based on whether matched items exist and ImdbId/TmdbId is available. The worker SHALL NOT contain enrichment decision logic.

#### Scenario: Movie resolution with TMDB ID

- **WHEN** scored items include matched entries and TmdbId=550 is set
- **THEN** _state.TryGetEnrichmentRequest SHALL return true with an EnrichMovies(ImdbId, TmdbId=550, candidates) message

#### Scenario: Movie resolution with IMDB ID

- **WHEN** scored items include matched entries and ImdbId="tt0806910" is set
- **THEN** _state.TryGetEnrichmentRequest SHALL return true with an EnrichMovies(ImdbId="tt0806910", TmdbId, candidates) message

#### Scenario: No movie ID available

- **WHEN** both ImdbId and TmdbId are null
- **THEN** _state.TryGetEnrichmentRequest SHALL return false

#### Scenario: No matched items

- **WHEN** scored items have no matched entries (all Matched=false)
- **THEN** _state.TryGetEnrichmentRequest SHALL return false

### Requirement: MovieSearchWorker records history after enrichment
MovieSearchWorker SHALL tell HistoryRegion with RecordHistory after enrichment completes (or after scoring if enrichment is skipped).

#### Scenario: Record after enrichment
- **WHEN** MovieSearchWorker receives MoviesEnriched
- **THEN** it merges enrichment into ItemTraces
- **THEN** it tells HistoryRegion with RecordHistory
- **THEN** it replies SearchMovieCompleted to the caller

#### Scenario: Record after scoring when enrichment skipped
- **WHEN** MovieSearchWorker completes scoring and enrichment is not applicable
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)

#### Scenario: Record after enrichment failure
- **WHEN** MovieSearchWorker receives EnrichMoviesFailed
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)

### Requirement: MovieSearchWorker uses IMetadataResolver

The MovieSearchWorker SHALL resolve the MetadataResolver singleton via `Context.GetActor<IMetadataResolver>()`.

#### Scenario: Actor resolution

- **WHEN** MovieSearchWorker is constructed
- **THEN** it SHALL resolve `IMetadataResolver` for movie resolution requests
