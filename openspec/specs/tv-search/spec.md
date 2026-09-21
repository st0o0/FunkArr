## Purpose

TV search pipeline orchestration — the TvSearchWorker coordinates Mediathek queries, RuleSet resolution, MatchMagic scoring, and episode enrichment using Become-based phases and Ask+PipeTo communication.

## Requirements

### Requirement: TvSearchWorker is a sharded entity

The TvSearchWorker SHALL be a sharded entity using SearchId (Guid) as the shard key. Each search request creates a new worker instance that processes the search and responds. The worker SHALL be passivated automatically via `PassivateIdleEntityAfter` on the shard configuration — no manual Passivate calls.

#### Scenario: Worker creation and auto-passivation

- **WHEN** the TvSearch ShardRegion receives a message with a new SearchId
- **THEN** a new TvSearchWorker instance SHALL be created, process the search, respond, and be passivated automatically after 30 seconds of idle time

### Requirement: TvSearchWorker orchestrates search pipeline

The TvSearchWorker SHALL use Become-based phase transitions and Ask+PipeTo for all inter-actor communication. The worker SHALL NOT implement IWithTimers. The worker SHALL capture the original Sender as `ReplyTo` in state when the SearchSeries command arrives. All replies SHALL use `_state.ReplyTo.Tell(...)`. All outgoing messages SHALL use `Ask<XxxResponse>(request, timeout).PipeTo(Self, failure: ex => new XxxFailed(ex))`. The PipeTo failure lambda SHALL map Ask timeouts and transport errors to the domain-owned Failed record type. Each Become state SHALL handle exactly two message types: XxxCompleted and XxxFailed. The worker SHALL NOT define any private timeout record types. The worker SHALL NOT use Timers.StartSingleTimer or Timers.Cancel.

#### Scenario: Successful TV search with query text

- **WHEN** the worker receives a SearchSeries with Query="Tatort"
- **THEN** it SHALL call _state.Init(cmd, Sender), call _state.TryGetMediathekQuery to get the query message, Ask the MediathekViewWebManager with a 15-second timeout, PipeTo Self with failure mapped to QueryMediathekFailed, and Become(Querying)

#### Scenario: ID-only TV search

- **WHEN** the worker receives a SearchSeries with Query=null and TvdbId=83214
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
- **THEN** the worker SHALL call _state.Apply(scored), Ask the EnrichmentManager with a 10-second timeout, PipeTo Self with failure mapped to EnrichEpisodesFailed, and Become(Enriching)

#### Scenario: Scoring completes without enrichment needed

- **WHEN** ScoreCompleted is received and _state.TryGetEnrichmentRequest returns false
- **THEN** the worker SHALL call _state.Apply(scored), tell HistoryRegion with RecordHistory (scoring-only ItemTraces), and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment succeeds

- **WHEN** EnrichEpisodesCompleted is received
- **THEN** the worker SHALL call _state.Apply(enriched), merge enrichment into ItemTraces, tell HistoryRegion with RecordHistory, and Reply with _state.ToSearchCompleted()

#### Scenario: Enrichment fails

- **WHEN** EnrichEpisodesFailed is received (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL tell HistoryRegion with RecordHistory (scoring-only ItemTraces) and Reply with _state.ToSearchCompleted() using existing metadata

#### Scenario: MediathekViewWeb query fails

- **WHEN** QueryMediathekFailed is received (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL Reply with SearchSeriesFailed(SearchId, cause)

#### Scenario: MatchMagic scoring fails

- **WHEN** ScoringFailed is received (including Ask timeout mapped via PipeTo failure)
- **THEN** the worker SHALL Reply with SearchSeriesFailed(SearchId, cause)

#### Scenario: Text search carries IDs through to results

- **WHEN** the worker receives a SearchSeries with Query="Tatort" and TvdbId=83214
- **THEN** the state's BaseIdentity SHALL include TvdbId=83214, which flows into all EnrichedItems and SearchResultItems

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

### Requirement: TvSearchWorker records history after enrichment
TvSearchWorker SHALL tell HistoryRegion with RecordHistory after enrichment completes (or after scoring if enrichment is skipped).

#### Scenario: Record after enrichment
- **WHEN** TvSearchWorker receives EpisodesEnriched
- **THEN** it merges enrichment into ItemTraces
- **THEN** it tells HistoryRegion with RecordHistory
- **THEN** it replies SearchSeriesCompleted to the caller

#### Scenario: Record after scoring when enrichment skipped
- **WHEN** TvSearchWorker completes scoring and enrichment is not applicable
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)
- **THEN** it replies SearchSeriesCompleted

#### Scenario: Record after enrichment failure
- **WHEN** TvSearchWorker receives EnrichEpisodesFailed
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)
- **THEN** it replies SearchSeriesCompleted
