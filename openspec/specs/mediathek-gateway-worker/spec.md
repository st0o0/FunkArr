## Purpose

MediathekGatewayWorker is a stateless child actor of SearchCoordinator that provides throttled access to the MediathekViewWeb API, enforcing rate limiting to avoid overloading the upstream service.

## Requirements

### Requirement: Throttled Mediathek access via Ask
`MediathekGatewayWorker` SHALL respond to `FetchItems(string searchTerm)` with `ItemsFetched(MediathekResultItem[])`. It SHALL use `MediathekClient` internally and enforce rate limiting.

#### Scenario: Successful fetch
- **WHEN** `MediathekGatewayWorker` receives `FetchItems("Tatort")`
- **THEN** it SHALL query `MediathekClient` and reply with `ItemsFetched` containing the results

#### Scenario: Mediathek API failure
- **WHEN** `MediathekClient` throws an `HttpRequestException`
- **THEN** `MediathekGatewayWorker` SHALL reply with `ItemsFetched(Array.Empty<MediathekResultItem>())` and log a warning

### Requirement: Rate limiting
`MediathekGatewayWorker` SHALL limit concurrent requests to the MediathekViewWeb API. Maximum rate SHALL be approximately 2 requests per second.

#### Scenario: Burst of requests throttled
- **WHEN** 10 `FetchItems` requests arrive within 1 second
- **THEN** `MediathekGatewayWorker` SHALL process them at approximately 2 per second, queuing the rest

### Requirement: Stateless actor
`MediathekGatewayWorker` SHALL be stateless (no persistence, no cache). On restart, the throttle counter resets to zero.

#### Scenario: Restart resets state
- **WHEN** `MediathekGatewayWorker` is restarted by its supervisor
- **THEN** the rate limiter SHALL reset and the worker SHALL be immediately ready to process requests
