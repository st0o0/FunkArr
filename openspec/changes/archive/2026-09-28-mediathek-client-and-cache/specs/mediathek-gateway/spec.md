## MODIFIED Requirements

### Requirement: MediathekViewWebManager is a singleton HTTP gateway

The MediathekViewWebManager SHALL be a Cluster Singleton actor that serves as the single point of access to the MediathekViewWeb API. The actor SHALL implement `IWithUnboundedStash` for backpressure. The actor SHALL delegate HTTP requests and caching to an injected `MediathekClient` service. The actor SHALL NOT implement `IWithTimers` (cache cleanup is handled by the cache backend).

#### Scenario: Successful query

- **WHEN** a MediathekQuery message is received
- **THEN** the Manager SHALL use the MediathekQueryBuilder to build a query object, call `MediathekClient.QueryAsync` with the query object, and respond with the result mapped to a MediathekQueryCompleted message

#### Scenario: HTTP error

- **WHEN** the MediathekClient call fails (exception from HttpClient)
- **THEN** the Manager SHALL respond with a MediathekQueryFailed message containing the error reason
