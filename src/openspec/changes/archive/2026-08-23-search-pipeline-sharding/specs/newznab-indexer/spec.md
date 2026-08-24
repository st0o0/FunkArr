## MODIFIED Requirements

### Requirement: RSS feed via SearchActor pipeline
When a Newznab search request arrives with no search criteria (empty query and no tvdbid/imdbid), the system SHALL route the request to `RssFeedCoordinator` to serve cached recent content from active rulesets. The `RssFeedCoordinator` SHALL populate its cache using scatter-gather on the `TextSearchPipeline` ShardRegion for parallel topic refresh.

#### Scenario: Empty tvsearch serves RSS cache
- **WHEN** a client sends `GET /api?t=tvsearch&apikey=key` without `tvdbid` or `q` parameters
- **THEN** the system SHALL ask `RssFeedCoordinator` for cached results and return them as Newznab XML

#### Scenario: Empty text search serves RSS cache
- **WHEN** a client sends `GET /api?t=search&apikey=key` without a `q` parameter
- **THEN** the system SHALL ask `RssFeedCoordinator` for cached results and return them as Newznab XML

#### Scenario: RSS cache populated via scatter-gather
- **WHEN** `RssFeedCoordinator` refreshes its cache
- **THEN** it SHALL send all topic queries to the `TextSearchPipeline` ShardRegion in parallel and collect results with a timeout
