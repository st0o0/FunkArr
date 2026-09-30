## MODIFIED Requirements

### Requirement: Unified two-phase search protocol for TV and Movie
The `SearchRequestActor` SHALL implement a two-phase protocol for TV and Movie searches: (1) resolve search hints from the media actor, (2) query MediathekGateway using the hints, then delegate matching back to the media actor. For bare searches (no query parameters), the actor SHALL short-circuit with direct Mediathek queries instead of delegating to media actors.

#### Scenario: TV search full flow
- **WHEN** a `Search.Tv(tvdbId=329324, showName="Feuer & Flamme", season=1, episode=null)` arrives
- **THEN** the actor SHALL ask `ShowActor(329324).ResolveSearch`, build a `MediathekSearchQuery` from the returned `SearchHint`, ask `MediathekGatewayActor.QueryItems`, ask `ShowActor(329324).Match(items)`, then apply `QualityExpander` and `ResultScorer` to the matched results

#### Scenario: Movie search full flow
- **WHEN** a `Search.Movie(imdbId="tt0082096", query=null)` arrives
- **THEN** the actor SHALL ask `MovieActor("tt0082096").ResolveSearch`, build a `MediathekSearchQuery` from the returned `SearchHint`, ask `MediathekGatewayActor.QueryItems`, ask `MovieActor("tt0082096").Match(items)`, then apply `QualityExpander` and `ResultScorer`

#### Scenario: SearchHint with topic builds ByTopic query
- **WHEN** the `SearchHint` has `Topic="Feuer & Flamme"` and `Channels=["WDR"]` and `MinDuration=2100`
- **THEN** the actor SHALL build `MediathekSearchQuery.ByTopic("Feuer & Flamme").FromChannel("WDR").WithDuration(min: 2100).ExcludeFuture()`

#### Scenario: SearchHint without topic builds BySearch query
- **WHEN** the `SearchHint` has `Topic=null` and `SearchTerm="New Show"`
- **THEN** the actor SHALL build `MediathekSearchQuery.BySearch("New Show").ExcludeFuture()`

#### Scenario: Bare movie search via topic filter
- **WHEN** a `Search.Movie(imdbId=null, query=null)` arrives
- **THEN** the actor SHALL short-circuit without asking MovieActor, build `MediathekSearchQuery.ByTopic("Filme").ExcludeFuture().Limit(100)`, query MediathekGateway, and process results directly through ContentFilter, QualityExpander, and ResultScorer

#### Scenario: Bare TV search via latest entries
- **WHEN** a `Search.Tv(tvdbId=null, showName=null, season=null, episode=null)` arrives
- **THEN** the actor SHALL short-circuit without asking ShowActor, build `MediathekSearchQuery.Latest().ExcludeFuture().Limit(100)`, query MediathekGateway, and process results directly through ContentFilter, QualityExpander, and ResultScorer
