## MODIFIED Requirements

### Requirement: MediathekViewWebManager is a singleton HTTP gateway

The MediathekViewWebManager SHALL be a Cluster Singleton actor that serves as the single point of access to the MediathekViewWeb API at `https://mediathekviewweb.de/api/query`.

#### Scenario: Successful query

- **WHEN** a MediathekQuery message is received
- **THEN** the Manager SHALL use the MediathekQueryBuilder to serialize the query to JSON, send an HTTP POST with `Content-Type: text/plain`, deserialize the response, and respond with a MediathekQueryCompleted containing the mapped MediathekItems

#### Scenario: HTTP error

- **WHEN** the HTTP request fails (network error, non-2xx status)
- **THEN** the Manager SHALL respond with a MediathekQueryFailed message containing the error reason

## ADDED Requirements

### Requirement: Extended mediathek search API parameters
The `GET /api/mediathek/search` endpoint SHALL accept additional optional query parameters for filtering and pagination beyond the existing `q` and `limit`.

#### Scenario: Channel filter parameter
- **WHEN** `GET /api/mediathek/search?q=news&channel=ARD` is requested
- **THEN** the system SHALL add a query field targeting the "channel" field with value "ARD" to the MediathekQuery

#### Scenario: Topic filter parameter
- **WHEN** `GET /api/mediathek/search?q=news&topic=Tagesschau` is requested
- **THEN** the system SHALL add a query field targeting the "topic" field with value "Tagesschau" to the MediathekQuery

#### Scenario: Duration filter parameters
- **WHEN** `GET /api/mediathek/search?q=news&durationMin=600&durationMax=3600` is requested
- **THEN** the system SHALL set `DurationMin=600` and `DurationMax=3600` on the MediathekQuery (values in seconds)

#### Scenario: Offset parameter for pagination
- **WHEN** `GET /api/mediathek/search?q=news&offset=20&limit=20` is requested
- **THEN** the system SHALL set `Offset=20` and `Size=20` on the MediathekQuery

#### Scenario: Sort parameters
- **WHEN** `GET /api/mediathek/search?q=news&sortBy=timestamp&sortOrder=desc` is requested
- **THEN** the system SHALL set `SortBy="timestamp"` and `SortOrder="desc"` on the MediathekQuery

### Requirement: Extended mediathek search API response
The mediathek search endpoint SHALL return a wrapped response object with enriched result items and a total count.

#### Scenario: Response structure
- **WHEN** the MediathekViewWebManager responds with MediathekQueryCompleted
- **THEN** the API response SHALL be JSON with `items` array and `totalResults` (int from `MediathekQueryCompleted.Total`)

#### Scenario: Enriched result item
- **WHEN** a result item is mapped to the API response
- **THEN** each item SHALL contain: `title` (string), `topic` (string), `channel` (string), `duration` (int, seconds), `quality` (int, estimated resolution), `description` (string or null), `timestamp` (long, unix), `size` (long, bytes), `hasSubtitles` (bool, true if UrlSubtitle is not null), `hasHd` (bool, true if UrlVideoHd is not null), `websiteUrl` (string or null)

#### Scenario: Backward compatibility
- **WHEN** existing consumers call the endpoint without new parameters
- **THEN** the response SHALL still work - the `items` array contains the same data as before plus additional fields, and `totalResults` is a new additive field in the wrapper object
