## MODIFIED Requirements

### Requirement: MediathekViewWebManager is a singleton HTTP gateway

The MediathekViewWebManager SHALL be a Cluster Singleton actor that serves as the single point of access to the MediathekViewWeb API at `https://mediathekviewweb.de/api/query`. The actor SHALL implement `IWithTimers` for periodic cache cleanup in addition to `IWithUnboundedStash` for backpressure.

#### Scenario: Successful query

- **WHEN** a MediathekQuery message is received
- **THEN** the Manager SHALL use the MediathekQueryBuilder to serialize the query to JSON, send an HTTP POST with `Content-Type: text/plain`, deserialize the response, and respond with a MediathekQueryCompleted containing the mapped MediathekItems

#### Scenario: HTTP error

- **WHEN** the HTTP request fails (network error, non-2xx status)
- **THEN** the Manager SHALL respond with a MediathekQueryFailed message containing the error reason
