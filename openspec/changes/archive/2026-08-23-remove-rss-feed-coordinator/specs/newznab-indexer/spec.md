## MODIFIED Requirements

### Requirement: RSS feed via SearchActor pipeline
When a Newznab search request arrives with no search criteria (empty query and no tvdbid/imdbid), the system SHALL route the request through the normal `SearchRouter` → `TextSearchPipeline` path with an empty query string. The `TextSearchPipeline` entity (keyed by `""`) SHALL return the latest MediathekViewWeb content via its standard caching mechanism.

#### Scenario: Empty tvsearch serves latest content
- **WHEN** a client sends `GET /api?t=tvsearch&apikey=key` without `tvdbid` or `q` parameters
- **THEN** the system SHALL send a `TextSearchRequest("")` through `SearchRouter` and return the results as Newznab XML

#### Scenario: Empty text search serves latest content
- **WHEN** a client sends `GET /api?t=search&apikey=key` without a `q` parameter
- **THEN** the system SHALL send a `TextSearchRequest("")` through `SearchRouter` and return the results as Newznab XML

#### Scenario: RSS pagination applied in controller
- **WHEN** a client sends `GET /api?t=search&limit=50&offset=10&apikey=key` without a `q` parameter
- **THEN** the system SHALL apply `limit` and `offset` to the `TextSearchPipeline` response before converting to Newznab XML
