## MODIFIED Requirements

### Requirement: TvSearchWorkerState is a class with Apply methods

TvSearchWorkerState SHALL be a sealed class in FunkArr.Search with private setters. It SHALL expose: SearchId (Guid), ReplyTo (IActorRef), Source (string), Query (string?), Sources (SourceInfo[]), RuleSetId (string?), TvdbId (int?), ImdbId (string?), Season (int?), Limit (int?), Offset (int?), MediaName (string?), Items (EnrichedItem[]), BaseIdentity (MediaIdentity computed from TvdbId/ImdbId).

#### Scenario: Init from SearchSeries command

- **WHEN** Init(SearchSeries, IActorRef) is called
- **THEN** the state SHALL set SearchId, ReplyTo, Source, Query, TvdbId, ImdbId, Season, Limit, Offset from the command, and Sources/Items to empty arrays
- **AND** the common fields (SearchId, Source, Query, Limit, Offset) SHALL be accessible via the SearchRequest base type

#### Scenario: Apply QueryMediathekCompleted

- **WHEN** Apply(QueryMediathekCompleted) is called
- **THEN** the state SHALL project result.Items to SourceInfo[] via SourceInfo.From and store as Sources

#### Scenario: Apply RuleSetResolved

- **WHEN** ApplyRuleSet(ruleSetId, mediaName) is called with RuleSetId="tatort" and MediaName="Tatort"
- **THEN** the state SHALL set RuleSetId and MediaName

#### Scenario: Apply ScoreCompleted

- **WHEN** Apply(ScoreCompleted) is called
- **THEN** the state SHALL create EnrichedItem[] from the scored results, using Sources for the SourceInfo and BaseIdentity with Season/Episode from MetadataSpec, and Match=null

#### Scenario: Apply EnrichEpisodesCompleted

- **WHEN** Apply(EnrichEpisodesCompleted) is called with resolved episodes
- **THEN** the state SHALL patch Items by updating Identity.Season/Episode and setting Match for each resolved index

### Requirement: MovieSearchWorkerState is a class with Apply methods

MovieSearchWorkerState SHALL be a sealed class in FunkArr.Search following the same pattern as TvSearchWorkerState but for movie searches. It SHALL expose: SearchId, ReplyTo, Source, Query, Sources, RuleSetId, ImdbId (string?), TmdbId (int?), Limit (int?), Offset (int?), MediaName, Items, BaseIdentity (computed from ImdbId/TmdbId).

#### Scenario: Init from SearchMovie command

- **WHEN** Init(SearchMovie, IActorRef) is called
- **THEN** the state SHALL set SearchId, ReplyTo, Source, Query, ImdbId, TmdbId, Limit, Offset from the command
- **AND** the common fields (SearchId, Source, Query, Limit, Offset) SHALL be accessible via the SearchRequest base type

#### Scenario: Apply EnrichMoviesCompleted

- **WHEN** Apply(EnrichMoviesCompleted) is called with resolved movies
- **THEN** the state SHALL patch Items by updating Identity.ImdbId/TmdbId and setting Match for each resolved index
