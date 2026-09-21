## Purpose

State classes for TvSearchWorker and MovieSearchWorker - sealed classes with Apply methods for pipeline transformations and TryGet methods for routing decisions (Pathfinder pattern).

## Requirements

### Requirement: TvSearchWorkerState is a class with Apply methods

TvSearchWorkerState SHALL be a sealed class in FunkArr.Search with private setters. It SHALL expose: SearchId (Guid), ReplyTo (IActorRef), Source (string), Query (string?), Sources (SourceInfo[]), RuleSetId (string?), TvdbId (int?), ImdbId (string?), Season (int?), Limit (int?), Offset (int?), MediaName (string?), Items (EnrichedItem[]), BaseIdentity (MediaIdentity computed from TvdbId/ImdbId), EnrichmentConfig (EnrichmentConfig?).

#### Scenario: Init from SearchSeries command

- **WHEN** Init(SearchSeries, IActorRef) is called
- **THEN** the state SHALL set SearchId, ReplyTo, Source, Query, TvdbId, ImdbId, Season, Limit, Offset from the command, and Sources/Items to empty arrays, and EnrichmentConfig to null
- **AND** the common fields (SearchId, Source, Query, Limit, Offset) SHALL be accessible via the SearchRequest base type

#### Scenario: Apply QueryMediathekCompleted

- **WHEN** Apply(QueryMediathekCompleted) is called
- **THEN** the state SHALL project result.Items to SourceInfo[] via SourceInfo.From and store as Sources

#### Scenario: Apply RuleSetResolved stores EnrichmentConfig

- **WHEN** ApplyRuleSet(ruleSetId, mediaName, enrichmentConfig) is called
- **THEN** the state SHALL set RuleSetId, MediaName, and EnrichmentConfig

#### Scenario: Apply ScoreCompleted preserves ConstructedTitle

- **WHEN** Apply(ScoreCompleted) is called and scored items have MetadataSpec with ConstructedTitle
- **THEN** the state SHALL store the ConstructedTitle per item index for later use in enrichment requests

#### Scenario: Apply EnrichEpisodesCompleted

- **WHEN** Apply(EnrichEpisodesCompleted) is called with resolved episodes
- **THEN** the state SHALL patch Items by updating Identity.Season/Episode and setting Match for each resolved index

### Requirement: TvSearchWorkerState TryGet methods build outgoing messages

The state SHALL provide TryGet methods that decide if a pipeline step is needed AND build the ready-to-send message. Each returns bool and has an out parameter for the message.

#### Scenario: TryGetMediathekQuery for text search

- **WHEN** TryGetMediathekQuery is called and the state has a query string
- **THEN** it SHALL return true with a QueryMediathek message configured for topic search with DurationMin=300

#### Scenario: TryGetRuleSetRequest when topic available

- **WHEN** TryGetRuleSetRequest is called, Sources is non-empty, and RuleSetId is null
- **THEN** it SHALL return true with a ResolveRuleSet message using the first item's topic

#### Scenario: TryGetRuleSetRequest when RuleSetId already set

- **WHEN** TryGetRuleSetRequest is called and RuleSetId is already set
- **THEN** it SHALL return false

#### Scenario: TryGetScoringRequest when items and RuleSet available

- **WHEN** TryGetScoringRequest is called, Sources is non-empty, and RuleSetId is set
- **THEN** it SHALL return true with a ScoreItems message built from Sources

#### Scenario: TryGetScoringRequest when no items

- **WHEN** TryGetScoringRequest is called and Sources is empty
- **THEN** it SHALL return false

#### Scenario: TryGetEnrichmentRequest passes config and ConstructedTitle

- **WHEN** TryGetEnrichmentRequest is called, Items contains entries with Identity.Season=null AND Identity.Episode=null, TvdbId is set, and EnrichmentConfig.Enabled is true
- **THEN** it SHALL return true with an EnrichEpisodes message that includes the EnrichmentConfig and populates each EpisodeCandidate.ConstructedTitle from the stored constructed titles

#### Scenario: TryGetEnrichmentRequest skips when enrichment disabled

- **WHEN** TryGetEnrichmentRequest is called and EnrichmentConfig.Enabled is false
- **THEN** it SHALL return false

#### Scenario: TryGetEnrichmentRequest when all episodes resolved

- **WHEN** TryGetEnrichmentRequest is called and all Items have Season and Episode set
- **THEN** it SHALL return false

#### Scenario: TryGetEnrichmentRequest when no TvdbId

- **WHEN** TryGetEnrichmentRequest is called and TvdbId is null
- **THEN** it SHALL return false

### Requirement: TvSearchWorkerState produces final result

The state SHALL have a ToSearchCompleted() method that expands Items into ReleaseVariants, maps to SearchResultItems, sorts by Score descending, and wraps in SearchCompleted.

#### Scenario: ToSearchCompleted with enriched items

- **WHEN** ToSearchCompleted is called with Items containing enriched episodes
- **THEN** it SHALL expand each EnrichedItem via ReleaseVariant.Expand, map via ToResultItem, sort descending by Score, and return SearchCompleted(SearchId, items, total)

#### Scenario: ToSearchCompleted with no items

- **WHEN** ToSearchCompleted is called and Items is empty
- **THEN** it SHALL return SearchCompleted(SearchId, [], 0)

### Requirement: MovieSearchWorkerState is a class with Apply methods

MovieSearchWorkerState SHALL be a sealed class in FunkArr.Search following the same pattern as TvSearchWorkerState but for movie searches. It SHALL expose: SearchId, ReplyTo, Source, Query, Sources, RuleSetId, ImdbId (string?), TmdbId (int?), Limit (int?), Offset (int?), MediaName, Items, BaseIdentity (computed from ImdbId/TmdbId), EnrichmentConfig (EnrichmentConfig?).

#### Scenario: Init from SearchMovie command

- **WHEN** Init(SearchMovie, IActorRef) is called
- **THEN** the state SHALL set SearchId, ReplyTo, Source, Query, ImdbId, TmdbId, Limit, Offset from the command
- **AND** the common fields (SearchId, Source, Query, Limit, Offset) SHALL be accessible via the SearchRequest base type

#### Scenario: Apply EnrichMoviesCompleted

- **WHEN** Apply(EnrichMoviesCompleted) is called with resolved movies
- **THEN** the state SHALL patch Items by updating Identity.ImdbId/TmdbId and setting Match for each resolved index

### Requirement: MovieSearchWorkerState TryGet methods

MovieSearchWorkerState SHALL provide TryGet methods following the same pattern as TvSearchWorkerState.

#### Scenario: ApplyRuleSet stores EnrichmentConfig

- **WHEN** ApplyRuleSet(ruleSetId, mediaName, enrichmentConfig) is called
- **THEN** the state SHALL store the EnrichmentConfig

#### Scenario: TryGetEnrichmentRequest for movies includes config

- **WHEN** TryGetEnrichmentRequest is called, Items contains matched entries, and ImdbId or TmdbId is set
- **THEN** it SHALL return true with an EnrichMovies message that includes the EnrichmentConfig

#### Scenario: TryGetEnrichmentRequest skips when enrichment disabled

- **WHEN** TryGetEnrichmentRequest is called and EnrichmentConfig.Enabled is false
- **THEN** it SHALL return false

#### Scenario: TryGetEnrichmentRequest when no movie IDs

- **WHEN** TryGetEnrichmentRequest is called and both ImdbId and TmdbId are null
- **THEN** it SHALL return false

#### Scenario: TryGetMediathekQuery for movie search

- **WHEN** TryGetMediathekQuery is called for a movie search with a query string
- **THEN** it SHALL return true with a QueryMediathek message configured for title+topic search with DurationMin=3600

### Requirement: Merge enrichment into ItemTraces
SearchWorker state SHALL provide a method to merge enrichment results (EnrichedEpisode[]/EnrichedMovie[]) into the stored ItemTrace[] by candidate index, producing ItemTraces with EnrichmentTrace populated.

#### Scenario: Merge enriched episodes into traces
- **WHEN** TvSearchWorkerState merges EnrichedEpisode[] into ItemTrace[]
- **THEN** matched items with a corresponding EnrichedEpisode get EnrichmentTrace with enriched=true, method, confidence, resolved fields
- **THEN** matched items without a corresponding EnrichedEpisode get EnrichmentTrace with enriched=false and detail

#### Scenario: Merge enriched movies into traces
- **WHEN** MovieSearchWorkerState merges EnrichedMovie[] into ItemTrace[]
- **THEN** matched items with a corresponding EnrichedMovie get EnrichmentTrace with enriched=true, resolved title and year

### Requirement: Build RecordHistory message
SearchWorker state SHALL provide a method to build a RecordHistory message from the current state after scoring and enrichment.

#### Scenario: Build RecordHistory
- **WHEN** SearchWorker state builds RecordHistory
- **THEN** the message contains requestId, ruleSetId, origin, timestamp, candidateCount, matchedCount, enrichedCount, and the merged ItemTrace[]
