## ADDED Requirements

### Requirement: TvSearchWorkerState is a class with Apply methods

TvSearchWorkerState SHALL be a sealed class in FunkArr.Search with private setters. It SHALL expose: SearchId (Guid), ReplyTo (IActorRef), Source (string), Sources (SourceInfo[]), RuleSetId (string?), TvdbId (int?), ImdbId (string?), Season (int?), MediaName (string?), Items (EnrichedItem[]), BaseIdentity (MediaIdentity computed from TvdbId/ImdbId).

#### Scenario: Init from TvSearch command

- **WHEN** Init(TvSearch, IActorRef) is called
- **THEN** the state SHALL set SearchId, ReplyTo, Source, TvdbId, ImdbId, Season from the command, and Sources/Items to empty arrays

#### Scenario: Apply MediathekQueryCompleted

- **WHEN** Apply(MediathekQueryCompleted) is called
- **THEN** the state SHALL project result.Items to SourceInfo[] via SourceInfo.From and store as Sources

#### Scenario: Apply RuleSetResolved

- **WHEN** Apply(RuleSetResolved) is called with RuleSetId="tatort" and MediaName="Tatort"
- **THEN** the state SHALL set RuleSetId and MediaName

#### Scenario: Apply ScoreCompleted

- **WHEN** Apply(ScoreCompleted) is called
- **THEN** the state SHALL create EnrichedItem[] from the scored results, using Sources for the SourceInfo and BaseIdentity with Season/Episode from MetadataSpec, and Match=null

#### Scenario: Apply EpisodesEnriched

- **WHEN** Apply(EpisodesEnriched) is called with resolved episodes
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

#### Scenario: TryGetEnrichmentRequest when unresolved episodes exist

- **WHEN** TryGetEnrichmentRequest is called, Items contains entries with Identity.Season=null AND Identity.Episode=null, and TvdbId is set
- **THEN** it SHALL return true with an EnrichEpisodes message containing EpisodeCandidates built from the unresolved items

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

MovieSearchWorkerState SHALL be a sealed class in FunkArr.Search following the same pattern as TvSearchWorkerState but for movie searches. It SHALL expose: SearchId, ReplyTo, Source, Sources, RuleSetId, ImdbId (string?), TmdbId (int?), MediaName, Items, BaseIdentity (computed from ImdbId/TmdbId).

#### Scenario: Init from MovieSearch command

- **WHEN** Init(MovieSearch, IActorRef) is called
- **THEN** the state SHALL set SearchId, ReplyTo, Source, ImdbId, TmdbId from the command

#### Scenario: Apply MoviesEnriched

- **WHEN** Apply(MoviesEnriched) is called with resolved movies
- **THEN** the state SHALL patch Items by updating Identity.ImdbId/TmdbId and setting Match for each resolved index

### Requirement: MovieSearchWorkerState TryGet methods

MovieSearchWorkerState SHALL provide TryGet methods following the same pattern as TvSearchWorkerState.

#### Scenario: TryGetEnrichmentRequest for movies

- **WHEN** TryGetEnrichmentRequest is called, Items contains matched entries, and ImdbId or TmdbId is set
- **THEN** it SHALL return true with an EnrichMovies message containing MovieCandidates

#### Scenario: TryGetEnrichmentRequest when no movie IDs

- **WHEN** TryGetEnrichmentRequest is called and both ImdbId and TmdbId are null
- **THEN** it SHALL return false

#### Scenario: TryGetMediathekQuery for movie search

- **WHEN** TryGetMediathekQuery is called for a movie search with a query string
- **THEN** it SHALL return true with a QueryMediathek message configured for title+topic search with DurationMin=3600
