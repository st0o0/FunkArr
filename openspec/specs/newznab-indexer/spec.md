## MODIFIED Requirements

### Requirement: Quality tiers
The system SHALL generate separate Newznab entries for each available quality tier using probed quality data. Release titles SHALL reflect the verified resolution and codec.

#### Scenario: Verified quality in release title
- **WHEN** probing determines a video is 720p h265
- **THEN** the release title SHALL be `SHOW.S01E03.GERMAN.720p.WEB.h265-FA` (not hardcoded h264)

#### Scenario: Multiple verified qualities
- **WHEN** Url_Video_HD is probed as 1080p h265 and Url_Video as 720p h264
- **THEN** the Newznab RSS SHALL contain two entries: `SHOW.S01E03.GERMAN.1080p.WEB.h265-FA` and `SHOW.S01E03.GERMAN.720p.WEB.h264-FA`

#### Scenario: Estimated quality marked conservatively
- **WHEN** probing fails and quality is estimated
- **THEN** the release title SHALL use the conservative estimate (Url_Video_HD → 720p) rather than the optimistic guess (1080p)

### Requirement: Rich Newznab attributes
The system SHALL emit additional `newznab:attr` elements in search XML responses beyond the existing category and size attributes.

#### Scenario: Resolution attribute
- **WHEN** probing determines resolution as 1920x1080
- **THEN** the XML SHALL include `<newznab:attr name="resolution" value="1080p" />`

#### Scenario: Video codec attribute
- **WHEN** probing determines codec as h265
- **THEN** the XML SHALL include `<newznab:attr name="video" value="h265" />`

#### Scenario: Real file size attribute
- **WHEN** probing determines file size as 1288490188 bytes
- **THEN** the XML SHALL include `<newznab:attr name="size" value="1288490188" />`

#### Scenario: Language attribute
- **WHEN** a search result is returned
- **THEN** the XML SHALL include `<newznab:attr name="language" value="German" />`

#### Scenario: TVDB ID attribute for TV searches
- **WHEN** a TV search result is returned for tvdbId 83214
- **THEN** the XML SHALL include `<newznab:attr name="tvdbid" value="83214" />`

#### Scenario: Season and episode attributes
- **WHEN** a TV search result matches season 1 episode 5
- **THEN** the XML SHALL include `<newznab:attr name="season" value="1" />` and `<newznab:attr name="episode" value="5" />`

#### Scenario: Estimated quality — no resolution attribute
- **WHEN** quality is estimated (ProbeSource=Estimated) rather than verified
- **THEN** the XML SHALL NOT include a resolution attribute (to avoid misleading Sonarr)

### Requirement: API key validation
The system SHALL validate the `apikey` query parameter on all Newznab endpoints. Requests with missing or invalid API keys MUST receive a Newznab error response. Authentication SHALL be handled by the centralized `ApiKeyMiddleware` instead of a dedicated `ApiKeyFilter`.

#### Scenario: Missing API key
- **WHEN** a client sends `GET /api?t=tvsearch&tvdbid=12345` without an `apikey` parameter
- **THEN** the `ApiKeyMiddleware` SHALL return HTTP 200 with Newznab error XML (code 100, "Incorrect user credentials")

#### Scenario: Valid API key
- **WHEN** a client sends a request with a valid `apikey`
- **THEN** the request is processed normally

### Requirement: Controller-based implementation
The Newznab endpoints SHALL be implemented as an MVC controller (`NewznabController`) in the `FunkArr.Api` namespace, located in `src/FunkArr/Api/`. The controller SHALL be marked as version-neutral (no URL version segment). All query parameters SHALL be bound via `[FromQuery]` attributes in the action method signatures instead of manual `Request.Query` reads.

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

### Requirement: OpenAPI tagging
The Newznab controller SHALL be tagged with `"Newznab Emulation"` for API documentation grouping.

#### Scenario: Scalar documentation grouping
- **WHEN** the OpenAPI spec is rendered in Scalar
- **THEN** Newznab endpoints SHALL appear under the "Newznab Emulation" group

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
