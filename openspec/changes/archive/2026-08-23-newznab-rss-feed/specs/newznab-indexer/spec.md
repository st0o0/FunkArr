## MODIFIED Requirements

### Requirement: RSS feed via SearchActor pipeline
When a Newznab search request arrives with no search criteria (empty query and no tvdbid/imdbid), the system SHALL route the request to `RssFeedCoordinator` to serve cached recent content from active rulesets. The response SHALL support Newznab pagination via `limit` and `offset` query parameters.

#### Scenario: Empty tvsearch serves RSS cache
- **WHEN** a client sends `GET /api?t=tvsearch&apikey=key` without `tvdbid` or `q` parameters
- **THEN** the system SHALL ask `RssFeedCoordinator` for cached results and return them as Newznab XML

#### Scenario: Empty text search serves RSS cache
- **WHEN** a client sends `GET /api?t=search&apikey=key` without a `q` parameter
- **THEN** the system SHALL ask `RssFeedCoordinator` for cached results and return them as Newznab XML

#### Scenario: RSS results include quality probing
- **WHEN** an RSS feed request is processed
- **THEN** the results SHALL include probed quality data (resolution, codec, file size) because `RssFeedCoordinator` queries through the full `SearchCoordinator` pipeline

#### Scenario: RSS results are cached
- **WHEN** two RSS feed requests arrive within the refresh interval
- **THEN** both requests SHALL be served from `RssFeedCoordinator`'s cache without triggering additional Mediathek queries

#### Scenario: RSS results filtered by ContentFilter
- **WHEN** the MediathekViewWeb response includes items with accessibility keywords (Audiodeskription, Gebärdensprache) or content type keywords (Trailer, Vorschau)
- **THEN** those items SHALL be excluded from the RSS feed because `SearchCoordinator`'s pipeline includes content filtering

#### Scenario: Pagination with limit parameter
- **WHEN** a client sends `GET /api?t=search&apikey=key&limit=50`
- **THEN** the system SHALL return at most 50 items from the RSS cache

#### Scenario: Pagination with offset parameter
- **WHEN** a client sends `GET /api?t=search&apikey=key&limit=50&offset=50`
- **THEN** the system SHALL return items 51-100 from the RSS cache

#### Scenario: Default pagination
- **WHEN** a client sends `GET /api?t=search&apikey=key` without limit/offset
- **THEN** the system SHALL return up to 100 items (default limit)
