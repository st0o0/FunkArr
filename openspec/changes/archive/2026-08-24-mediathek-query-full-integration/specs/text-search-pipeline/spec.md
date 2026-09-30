## MODIFIED Requirements

### Requirement: Mediathek fetch stage

TextSearchActor SHALL Ask MediathekGateway with a `QueryItems` message containing a `MediathekSearchQuery` built via the appropriate factory: `MediathekSearchQuery.Search(query).ExcludeFuture().Limit(200).Build()` for non-empty queries, `MediathekSearchQuery.Latest().ExcludeFuture().Limit(100).Build()` for empty queries (RSS/browse feed).

#### Scenario: Mediathek query
- **WHEN** a pipeline execution starts with a non-empty query
- **THEN** the entity SHALL Ask MediathekGateway with `QueryItems(MediathekSearchQuery.Search(query).ExcludeFuture().Limit(200).Build())`

### Requirement: RSS feed support

TextSearchActor SHALL handle empty query strings as valid search requests. An empty query (`""`) SHALL use `MediathekSearchQuery.Latest().ExcludeFuture().Limit(100).Build()` to produce the latest MediathekViewWeb content. The entity SHALL cache and passivate identically to any other query.

#### Scenario: Empty query returns latest content
- **WHEN** a `Search("")` message arrives at TextSearchActor
- **THEN** the entity SHALL ask MediathekGatewayActor with `QueryItems(MediathekSearchQuery.Latest().ExcludeFuture().Limit(100).Build())`, which produces `Queries = []` with `future: false` on the wire, returning the latest aired content sorted by timestamp descending

#### Scenario: Empty query caching
- **WHEN** multiple `Search("")` requests arrive within 55 minutes
- **THEN** the entity SHALL return the cached result from the first request without re-querying MediathekViewWeb

#### Scenario: Empty query sharding
- **WHEN** a `Search("")` message arrives
- **THEN** `IShardedMessage.EntityKey` SHALL return `""`, creating a single shared entity for all empty-query requests
