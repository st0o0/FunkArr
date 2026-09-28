## Purpose

Singleton HTTP gateway to the MediathekViewWeb API. Handles query construction, stream-based concurrency control, and response mapping. Delegates HTTP requests and caching to MediathekClient.
## Requirements
### Requirement: MediathekViewWebManager is a singleton HTTP gateway

The MediathekViewWebManager SHALL be a Cluster Singleton actor that serves as the single point of access to the MediathekViewWeb API. The actor SHALL use an internal Akka.Streams pipeline for concurrency control and request processing. The actor SHALL delegate HTTP requests and caching to an injected `MediathekClient` service. The actor SHALL NOT implement `IWithUnboundedStash` or `IWithTimers`.

#### Scenario: Successful query

- **WHEN** a `QueryMediathek` message is received
- **THEN** the Manager SHALL assign a `Guid` RequestId, store the Sender in a pending dictionary keyed by RequestId, use the `MediathekQueryBuilder` to build a JSON query string, feed it into the stream pipeline, and when the stream result arrives, look up the original Sender and respond with a `QueryMediathekCompleted` message

#### Scenario: HTTP error

- **WHEN** the `MediathekClient` call fails with an exception inside the stream pipeline
- **THEN** the exception SHALL be caught in the `SelectAsyncUnordered` stage, routed back to the actor as a `StreamFailure`, and the actor SHALL respond to the original Sender with a `QueryMediathekFailed` message

### Requirement: MediathekViewWebManager uses Akka.Streams for concurrency control

The Manager SHALL materialize a single long-lived Akka.Streams pipeline in its constructor. The pipeline SHALL use `Source.ActorRef` for input, `Select` for query building, `SelectAsyncUnordered` for throttled HTTP execution, and `Sink.ActorRef` to route results back to Self.

#### Scenario: Concurrency limit

- **WHEN** multiple `QueryMediathek` messages arrive concurrently
- **THEN** the stream's `SelectAsyncUnordered(maxConcurrent)` SHALL limit parallel HTTP requests to the configured maximum (default 3)

#### Scenario: Buffer overflow

- **WHEN** more requests arrive than the `Source.ActorRef` buffer can hold (64 elements)
- **THEN** the overflow strategy `DropNew` SHALL drop the excess request, and the caller SHALL receive no response (Ask timeout acts as circuit breaker)

#### Scenario: Stream error resilience

- **WHEN** an unexpected exception occurs in the stream pipeline
- **THEN** the `Deciders.ResumingDecider` supervision strategy SHALL drop the failing element and continue processing, and the actor SHALL log a warning

### Requirement: MediathekViewWebManager tracks in-flight requests

The Manager SHALL maintain a `Dictionary<Guid, IActorRef>` mapping RequestIds to original Senders. This dictionary serves as both sender correlation and in-flight request tracking.

#### Scenario: Request lifecycle

- **WHEN** a `QueryMediathek` is received and a stream result arrives for it
- **THEN** the Manager SHALL add an entry to the dictionary when the request enters the stream, and remove the entry when the stream result is processed

#### Scenario: Orphaned request

- **WHEN** the stream drops or fails to process a request (e.g., supervision resume)
- **THEN** the dictionary entry SHALL remain until the caller's Ask times out (no explicit cleanup required)

### Requirement: MediathekQueryBuilder maps query messages to API format

The MediathekQueryBuilder SHALL be an internal class (not an actor) that provides a fluent API to construct the MediathekViewWeb JSON request body. It SHALL support all API features as a 1:1 mapping.

#### Scenario: Full query construction

- **WHEN** a MediathekQuery with multiple fields, duration filters, sorting, and pagination is built
- **THEN** the Builder SHALL produce a JSON string matching the API format with `queries` array, `sortBy`, `sortOrder`, `future`, `offset`, `size`, `duration_min`, and `duration_max` fields

#### Scenario: Minimal query

- **WHEN** a MediathekQuery with only one query field is built
- **THEN** the Builder SHALL produce valid JSON with defaults: `sortBy=timestamp`, `sortOrder=desc`, `future=false`, `offset=0`, `size=15`

### Requirement: MediathekItem maps all quality variants

The MediathekItem response record SHALL include all URL variants returned by the API.

#### Scenario: Item with all variants

- **WHEN** an API response item has url_video_low, url_video, url_video_hd, and url_subtitle
- **THEN** the MediathekItem SHALL contain all four URLs as nullable string fields plus url_website

#### Scenario: Item with missing variants

- **WHEN** an API response item has only url_video (no HD, no low, no subtitle)
- **THEN** the MediathekItem SHALL have null for UrlVideoLow, UrlVideoHd, and UrlSubtitle

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

