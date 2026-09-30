## MODIFIED Requirements

### Requirement: MovieSearchWorker orchestrates search pipeline

The MovieSearchWorker SHALL use Become-based phase transitions and Tell+Timer for all inter-actor communication. The worker SHALL implement IWithTimers. The worker SHALL capture the original Sender as `ReplyTo` in state when the MovieSearch command arrives. All replies SHALL use `_state.ReplyTo.Tell(...)`. All outgoing messages SHALL use `Tell` with a corresponding timer via `Timers.StartSingleTimer`. The worker SHALL NOT use Ask or PipeTo.

#### Scenario: Successful movie search with query text

- **WHEN** the worker receives a MovieSearch with Query="Das Boot"
- **THEN** it SHALL call _state.Init(cmd, Sender), call _state.TryGetMediathekQuery to get the query message, Tell the MediathekViewWebManager, start a "mediathek-timeout" timer, and Become(Querying)

#### Scenario: ImdbId-only movie search

- **WHEN** the worker receives a MovieSearch with Query=null and ImdbId="tt0806910"
- **THEN** it SHALL call _state.Init(cmd, Sender), call _state.TryGetRuleSetRequest to get a ResolveRuleSet message, Tell the RuleSetResolver, start a "ruleset-timeout" timer, and Become(ResolvingRuleSet)

#### Scenario: ID-only search with no matching ruleset

- **WHEN** the RuleSetResolver responds with RuleSetFailed or the ruleset-timeout fires
- **THEN** the worker SHALL cancel the timer, Reply with _state.ToSearchCompleted() (which returns SearchCompleted with empty items if Sources is empty)

#### Scenario: Become-based phase transitions

- **WHEN** the worker transitions between pipeline phases
- **THEN** it SHALL use Become to switch handler sets so that each phase only handles messages relevant to that phase, plus the corresponding timeout message

#### Scenario: Scoring phase transition

- **WHEN** scoring is needed after RuleSet resolution or query completion
- **THEN** the worker SHALL call _state.TryGetScoringRequest, Tell the MatchMagicManager, start a "scoring-timeout" timer, and Become(Scoring)

#### Scenario: Enrichment phase transition

- **WHEN** ScoreCompleted is received and _state.TryGetEnrichmentRequest returns true
- **THEN** the worker SHALL cancel the scoring timer, call _state.Apply(scored), Tell the MetadataResolver with the enrichment request, start an "enrich-timeout" timer, and Become(Enriching)

#### Scenario: Scoring completes without enrichment needed

- **WHEN** ScoreCompleted is received and _state.TryGetEnrichmentRequest returns false
- **THEN** the worker SHALL cancel the scoring timer, call _state.Apply(scored), and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment succeeds

- **WHEN** the MetadataResolver responds with MoviesEnriched
- **THEN** the worker SHALL cancel the enrich-timeout timer, call _state.Apply(enriched), and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment fails

- **WHEN** the MetadataResolver responds with MovieEnrichmentFailed
- **THEN** the worker SHALL cancel the enrich-timeout timer and Reply with _state.ToSearchCompleted() using existing metadata

#### Scenario: Enrichment times out

- **WHEN** the enrich-timeout fires
- **THEN** the worker SHALL Reply with _state.ToSearchCompleted() using existing metadata

#### Scenario: MediathekViewWeb query fails

- **WHEN** the MediathekViewWebManager responds with MediathekQueryFailed or the mediathek-timeout fires
- **THEN** the worker SHALL cancel the timer and Reply with SearchFailed(SearchId, cause)

#### Scenario: MatchMagic scoring fails

- **WHEN** the MatchMagicManager responds with ScoringFailed or the scoring-timeout fires
- **THEN** the worker SHALL cancel the timer and Reply with SearchFailed(SearchId, cause)

#### Scenario: No query and no IDs

- **WHEN** the worker receives a MovieSearch with Query=null and ImdbId=null and TmdbId=null
- **THEN** the worker SHALL Reply with SearchFailed(SearchId, cause)

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a MovieSearch with Query="Das Boot" and ImdbId="tt1234567"
- **THEN** the state's BaseIdentity SHALL include ImdbId="tt1234567", which flows into all EnrichedItems and SearchResultItems

### Requirement: MovieSearchWorker is a sharded entity

The MovieSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search and responds. The worker SHALL be passivated automatically via `PassivateIdleEntityAfter` on the shard configuration — no manual Passivate calls.

#### Scenario: Worker creation and auto-passivation

- **WHEN** the MovieSearch ShardRegion receives a message with a new SearchId
- **THEN** a new MovieSearchWorker instance SHALL be created, process the search, respond, and be passivated automatically after 30 seconds of idle time

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

### Requirement: MovieSearchWorker uses IMetadataResolver

The MovieSearchWorker SHALL resolve the MetadataResolver singleton via `Context.GetActor<IMetadataResolver>()`.

#### Scenario: Actor resolution

- **WHEN** MovieSearchWorker is constructed
- **THEN** it SHALL resolve `IMetadataResolver` for movie resolution requests

## REMOVED Requirements

### Requirement: ScoredResults stored in state

**Reason**: Replaced by EnrichedItem[] in state. Apply(ScoreCompleted) creates EnrichedItem[] directly.
**Migration**: State holds Items (EnrichedItem[]) after Apply(ScoreCompleted).
