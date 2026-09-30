## MODIFIED Requirements

### Requirement: RSS feed support
TextSearchPipeline SHALL handle empty query strings as valid search requests. An empty query (`""`) SHALL produce the latest MediathekViewWeb content (MediathekViewWeb returns newest entries across all broadcasters when called with `Queries = []`). The entity SHALL cache and passivate identically to any other query.

#### Scenario: Empty query returns latest content
- **WHEN** a `Search("")` message arrives at TextSearchPipeline
- **THEN** the entity SHALL ask MediathekGatewayWorker with an empty search term, which sends `Queries = []` to MediathekViewWeb, returning the latest content sorted by timestamp descending

#### Scenario: Empty query caching
- **WHEN** multiple `Search("")` requests arrive within 55 minutes
- **THEN** the entity SHALL return the cached result from the first request without re-querying MediathekViewWeb

#### Scenario: Empty query sharding
- **WHEN** a `Search("")` message arrives
- **THEN** the `TextSearchPipelineMessageExtractor` SHALL use `""` as the entity ID, creating a single shared entity for all empty-query requests
