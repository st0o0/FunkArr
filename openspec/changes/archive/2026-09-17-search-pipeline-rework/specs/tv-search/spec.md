## MODIFIED Requirements

### Requirement: TvSearchWorker orchestrates search pipeline

The TvSearchWorker SHALL use Become-based phase transitions and Tell+Timer for all inter-actor communication. The worker SHALL implement IWithTimers. The worker SHALL capture the original Sender as `ReplyTo` in state when the TvSearch command arrives. All replies SHALL use `_state.ReplyTo.Tell(...)`. All outgoing messages SHALL use `Tell` with a corresponding timer via `Timers.StartSingleTimer`. The worker SHALL NOT use Ask or PipeTo.

#### Scenario: Successful TV search with query text

- **WHEN** the worker receives a TvSearch with Query="Tatort"
- **THEN** it SHALL call _state.Init(cmd, Sender), call _state.TryGetMediathekQuery to get the query message, Tell the MediathekViewWebManager, start a "mediathek-timeout" timer, and Become(Querying)

#### Scenario: ID-only TV search

- **WHEN** the worker receives a TvSearch with Query=null and TvdbId=83214
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

- **WHEN** the MetadataResolver responds with EpisodesEnriched
- **THEN** the worker SHALL cancel the enrich-timeout timer, call _state.Apply(enriched), and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment fails

- **WHEN** the MetadataResolver responds with EpisodeEnrichmentFailed
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

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a TvSearch with Query="Tatort" and TvdbId=83214
- **THEN** the state's BaseIdentity SHALL include TvdbId=83214, which flows into all EnrichedItems and SearchResultItems

### Requirement: TvSearchWorker is a sharded entity

The TvSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search and responds. The worker SHALL be passivated automatically via `PassivateIdleEntityAfter` on the shard configuration — no manual Passivate calls.

#### Scenario: Worker creation and auto-passivation

- **WHEN** the TvSearch ShardRegion receives a message with a new SearchId
- **THEN** a new TvSearchWorker instance SHALL be created, process the search, respond, and be passivated automatically after 30 seconds of idle time

### Requirement: TvSearchWorker episode resolution stage

After receiving ScoreCompleted, the TvSearchWorker SHALL call _state.Apply(scored) and then _state.TryGetEnrichmentRequest. The state SHALL decide if enrichment is needed based on whether unresolved items exist and TvdbId is available. The worker SHALL NOT contain enrichment decision logic — it only acts on the TryGet result.

#### Scenario: All items have season/episode from regex

- **WHEN** all scored items have Season and Episode in their MetadataSpec
- **THEN** _state.TryGetEnrichmentRequest SHALL return false and the worker SHALL Reply with _state.ToSearchCompleted()

#### Scenario: Some items lack season/episode

- **WHEN** scored items include entries with Season=null
- **THEN** _state.TryGetEnrichmentRequest SHALL return true with an EnrichEpisodes message

#### Scenario: No TvdbId available

- **WHEN** the search has no TvdbId
- **THEN** _state.TryGetEnrichmentRequest SHALL return false

### Requirement: TvSearchWorker uses IMetadataResolver

The TvSearchWorker SHALL resolve the MetadataResolver singleton via `Context.GetActor<IMetadataResolver>()`.

#### Scenario: Actor resolution

- **WHEN** TvSearchWorker is constructed
- **THEN** it SHALL resolve `IMetadataResolver` for episode resolution requests

### Requirement: TvSearchWorker constructs EpisodeCandidates from scored items

The state's TryGetEnrichmentRequest method SHALL build EpisodeCandidate records from EnrichedItems that lack Season/Episode by extracting: Index, Title from Source.Title, AiredAt from Source.AiredAt, Duration from Source.Duration.

#### Scenario: Candidate from unresolved item

- **WHEN** an EnrichedItem has Identity.Season=null and Identity.Episode=null, Source.AiredAt=2026-08-30
- **THEN** the EpisodeCandidate SHALL have ExistingSeason=null, ExistingEpisode=null, AiredAt=2026-08-30

### Requirement: TvSearchWorker merges resolved episodes into metadata

The state's Apply(EpisodesEnriched) method SHALL update EnrichedItems by patching Identity.Season/Episode and setting Match from each EnrichedEpisode. Unresolved items SHALL retain their existing Identity and Match=null.

#### Scenario: Merge resolved season/episode

- **WHEN** an EnrichedEpisode has Index=3, Season="2", Episode="9", Confidence=0.9, Method=TitleMatch
- **THEN** the EnrichedItem at Index=3 SHALL have Identity.Season="2", Identity.Episode="9", Match=new MatchInfo(0.9, TitleMatch)

#### Scenario: Unresolved items retain original identity

- **WHEN** an item at index 5 has no corresponding EnrichedEpisode
- **THEN** the EnrichedItem at index 5 SHALL keep its original Identity and Match=null

## REMOVED Requirements

### Requirement: ScoredResults stored in state

**Reason**: Replaced by EnrichedItem[] in state. Apply(ScoreCompleted) creates EnrichedItem[] directly — no intermediate ScoredResults storage needed.
**Migration**: State holds Items (EnrichedItem[]) after Apply(ScoreCompleted). TryGetEnrichmentRequest reads from Items.
