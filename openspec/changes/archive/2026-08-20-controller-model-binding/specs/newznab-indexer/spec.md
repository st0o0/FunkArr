## MODIFIED Requirements

### Requirement: Controller-based implementation
The Newznab endpoints SHALL be implemented as an MVC controller (`NewznabController`) in the `FunkArr.Api` namespace. All query parameters SHALL be bound via `[FromQuery]` attributes in the action method signatures instead of manual `Request.Query` reads.

#### Scenario: Newznab route unchanged
- **WHEN** a client sends `GET /api?t=caps&apikey=key`
- **THEN** the system SHALL route to `NewznabController` at the same `/api` path as before

#### Scenario: TV search parameters bound via model binding
- **WHEN** a client sends `GET /api?t=tvsearch&tvdbid=12345&season=1&ep=3&q=tatort&apikey=key`
- **THEN** the system SHALL bind `t`, `tvdbid`, `season`, `ep` (mapped to `episode`), and `q` via `[FromQuery]` attributes

#### Scenario: Movie search parameters bound via model binding
- **WHEN** a client sends `GET /api?t=movie&imdbid=tt1234567&q=film&apikey=key`
- **THEN** the system SHALL bind `t`, `imdbid`, and `q` via `[FromQuery]` attributes

#### Scenario: Fake NZB download parameters bound via model binding
- **WHEN** a client sends `GET /index/api/fake_nzb?url=...&title=...&subtitle=...`
- **THEN** the system SHALL bind `url`, `title`, and `subtitle` via `[FromQuery]` attributes

## ADDED Requirements

### Requirement: RSS feed via SearchActor pipeline
When a Newznab search request arrives with no search criteria (empty query and no tvdbid/imdbid), the system SHALL route the request through `SearchActor.TextSearchRequest("")` to produce an RSS feed of recent content. The results SHALL flow through the full pipeline: MediathekViewWeb query, content filtering, quality probing, and caching.

#### Scenario: Empty tvsearch triggers RSS feed
- **WHEN** a client sends `GET /api?t=tvsearch&apikey=key` without `tvdbid` or `q` parameters
- **THEN** the system SHALL send `TextSearchRequest("")` to SearchActor and return the results as Newznab XML

#### Scenario: Empty text search triggers RSS feed
- **WHEN** a client sends `GET /api?t=search&apikey=key` without a `q` parameter
- **THEN** the system SHALL send `TextSearchRequest("")` to SearchActor and return the results as Newznab XML

#### Scenario: RSS results include quality probing
- **WHEN** an RSS feed request is processed
- **THEN** the results SHALL include probed quality data (resolution, codec, file size) just like regular search results

#### Scenario: RSS results are cached
- **WHEN** two RSS feed requests arrive within 55 minutes
- **THEN** the second request SHALL be served from SearchActor's cache

#### Scenario: RSS results filtered by ContentFilter
- **WHEN** the MediathekViewWeb response includes items with accessibility keywords (Audiodeskription, Gebärdensprache) or content type keywords (Trailer, Vorschau)
- **THEN** those items SHALL be excluded from the RSS feed
