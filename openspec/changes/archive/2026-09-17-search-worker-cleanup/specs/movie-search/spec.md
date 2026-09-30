## MODIFIED Requirements

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
- **THEN** the worker SHALL call _state.Apply(scored) and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment succeeds

- **WHEN** EnrichMoviesCompleted is received
- **THEN** the worker SHALL call _state.Apply(enriched) and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment fails

- **WHEN** EnrichMoviesFailed is received (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL Reply with _state.ToSearchCompleted() using existing metadata

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
