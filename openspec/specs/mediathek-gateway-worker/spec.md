## Purpose

MediathekGatewayActor is a stateless singleton actor registered via `ActorRegistry` that provides throttled access to the MediathekViewWeb API, enforcing rate limiting to avoid overloading the upstream service.

## Requirements

### Requirement: MediathekGatewayActor registration
The MediathekGatewayActor SHALL be registered as a singleton actor via `ActorRegistry` instead of being a child actor of SearchCoordinator. All search pipeline entities SHALL access it through `ActorRegistry.Get<MediathekGatewayActor>()`.

#### Scenario: Singleton registration
- **WHEN** the application starts
- **THEN** `MediathekGatewayActor` SHALL be registered as a singleton resolvable via `ActorRegistry`

#### Scenario: Search entity access
- **WHEN** a `TvSearchActor` entity needs to query MediathekViewWeb
- **THEN** it SHALL ask the singleton `MediathekGatewayActor` via `ActorRegistry.Get<MediathekGatewayActor>()`

#### Scenario: Rate limiting preserved
- **WHEN** multiple search entities send concurrent `FetchItems` requests
- **THEN** the singleton `MediathekGatewayActor` SHALL process them through its single mailbox, naturally rate-limiting MediathekViewWeb access

### Requirement: Throttled Mediathek access via Ask
`MediathekGatewayActor` SHALL respond to `FetchItems(string searchTerm)` with `ItemsFetched(MediathekResultItem[])`. It SHALL use `MediathekClient` internally and enforce rate limiting.

#### Scenario: Successful fetch
- **WHEN** `MediathekGatewayActor` receives `FetchItems("Tatort")`
- **THEN** it SHALL query `MediathekClient` and reply with `ItemsFetched` containing the results

#### Scenario: Mediathek API failure
- **WHEN** `MediathekClient` throws an `HttpRequestException`
- **THEN** `MediathekGatewayActor` SHALL reply with `ItemsFetched(Array.Empty<MediathekResultItem>())` and log a warning

### Requirement: Rate limiting
`MediathekGatewayActor` SHALL limit concurrent requests to the MediathekViewWeb API. Maximum rate SHALL be approximately 2 requests per second.

#### Scenario: Burst of requests throttled
- **WHEN** 10 `FetchItems` requests arrive within 1 second
- **THEN** `MediathekGatewayActor` SHALL process them at approximately 2 per second, queuing the rest

### Requirement: Stateless actor
`MediathekGatewayActor` SHALL be stateless (no persistence, no cache). On restart, the throttle counter resets to zero.

#### Scenario: Restart resets state
- **WHEN** `MediathekGatewayActor` is restarted by its supervisor
- **THEN** the rate limiter SHALL reset and the worker SHALL be immediately ready to process requests
