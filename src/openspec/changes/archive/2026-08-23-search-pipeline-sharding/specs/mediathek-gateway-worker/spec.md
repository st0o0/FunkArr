## MODIFIED Requirements

### Requirement: MediathekGatewayWorker registration
The MediathekGatewayWorker SHALL be registered as a singleton actor via `ActorRegistry` instead of being a child actor of SearchCoordinator. All search pipeline entities SHALL access it through `ActorRegistry.Get<MediathekGatewayWorker>()`.

#### Scenario: Singleton registration
- **WHEN** the application starts
- **THEN** `MediathekGatewayWorker` SHALL be registered as a singleton resolvable via `ActorRegistry`

#### Scenario: Search entity access
- **WHEN** a `TvSearchPipeline` entity needs to query MediathekViewWeb
- **THEN** it SHALL ask the singleton `MediathekGatewayWorker` via `ActorRegistry.Get<MediathekGatewayWorker>()`

#### Scenario: Rate limiting preserved
- **WHEN** multiple search entities send concurrent `FetchItems` requests
- **THEN** the singleton `MediathekGatewayWorker` SHALL process them through its single mailbox, naturally rate-limiting MediathekViewWeb access
