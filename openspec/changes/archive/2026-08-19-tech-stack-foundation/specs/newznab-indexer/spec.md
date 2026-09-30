## ADDED Requirements

### Requirement: Capabilities endpoint
The system SHALL respond to `GET /api?t=caps` with a Newznab-compatible XML document declaring supported search types (tvsearch, search, movie) and available categories.

#### Scenario: Prowlarr requests capabilities
- **WHEN** a client sends `GET /api?t=caps&apikey=<key>`
- **THEN** the system returns HTTP 200 with XML content type and a `<caps>` document listing supported `<searching>` modes and `<categories>`

### Requirement: TV search
The system SHALL respond to `GET /api?t=tvsearch` with Newznab RSS/XML containing matching results from MediathekViewWeb. It MUST support query parameters `tvdbid`, `season`, `ep`, and `q`.

#### Scenario: Search by TVDB ID and season/episode
- **WHEN** a client sends `GET /api?t=tvsearch&tvdbid=12345&season=1&ep=3`
- **THEN** the system queries MediathekViewWeb, filters results for the matching show/episode, and returns Newznab RSS with entries containing title, size, download link, and category

#### Scenario: Search with no results
- **WHEN** a client sends `GET /api?t=tvsearch&tvdbid=99999&season=1&ep=1` and no content matches
- **THEN** the system returns HTTP 200 with an empty Newznab RSS response (zero items)

### Requirement: Movie search
The system SHALL respond to `GET /api?t=movie` with Newznab RSS/XML containing matching results. It MUST support query parameters `imdbid` and `q`.

#### Scenario: Search by IMDB ID
- **WHEN** a client sends `GET /api?t=movie&imdbid=tt1234567`
- **THEN** the system queries MediathekViewWeb for the movie title, filters results, and returns Newznab RSS with matching entries

### Requirement: Text search
The system SHALL respond to `GET /api?t=search` with Newznab RSS/XML from a free-text query parameter `q`.

#### Scenario: Free-text search
- **WHEN** a client sends `GET /api?t=search&q=Tatort`
- **THEN** the system queries MediathekViewWeb with the search term and returns Newznab RSS with matching entries

### Requirement: Fake NZB generation
The system SHALL serve a fake NZB file at `/api/fake_nzb` that encodes the real download URL and title as base64 parameters in the NZB XML. The NZB MUST be valid XML that Sonarr/Radarr accepts.

#### Scenario: Download link in search results points to fake NZB
- **WHEN** a search result is returned in Newznab RSS
- **THEN** its `<enclosure>` URL points to `/api/fake_nzb?url=<base64_url>&title=<base64_title>`

#### Scenario: Fake NZB is served
- **WHEN** a client requests `GET /api/fake_nzb?url=<base64>&title=<base64>`
- **THEN** the system returns HTTP 200 with `application/x-nzb` content type and valid NZB XML containing the real URL encoded in XML comments

### Requirement: Quality tiers
The system SHALL generate separate Newznab entries for each available quality tier (1080p, 720p, 480p) when multiple qualities are available for a single piece of content.

#### Scenario: Multiple qualities available
- **WHEN** MediathekViewWeb returns a show with 1080p and 720p versions
- **THEN** the Newznab RSS contains two separate entries with quality indicated in the title (e.g., `SHOW.S01E03.GERMAN.1080p.WEB.h264-FA` and `SHOW.S01E03.GERMAN.720p.WEB.h264-FA`)

### Requirement: API key validation
The system SHALL validate the `apikey` query parameter on all Newznab endpoints. Requests with missing or invalid API keys MUST receive a Newznab error response.

#### Scenario: Missing API key
- **WHEN** a client sends `GET /api?t=tvsearch&tvdbid=12345` without an `apikey` parameter
- **THEN** the system returns HTTP 200 with Newznab error XML (code 100, "Incorrect user credentials")

#### Scenario: Valid API key
- **WHEN** a client sends a request with a valid `apikey`
- **THEN** the request is processed normally
