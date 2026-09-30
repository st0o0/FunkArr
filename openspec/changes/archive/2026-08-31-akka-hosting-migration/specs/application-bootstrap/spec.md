## MODIFIED Requirements

### Requirement: Akka.NET actor system configuration
`FunkArrActorSystemSetup` SHALL configure the actor system with name `"funkarr"`.
It SHALL clear default loggers and add `LoggerFactory` so Akka logs flow through
Serilog. It SHALL configure SQLite persistence via `WithSqlPersistence` with
journal and snapshot health checks. It SHALL register an actor system liveness
health check via `WithActorSystemLivenessCheck()`.

It SHALL configure single-node clustering via `WithClustering()`.

It SHALL register all `*Manager` actors as cluster singletons via
`WithSingleton<T>()`. It SHALL register all `*Worker` actors as sharded entities
via `WithShardRegion<T>()`.

Actors with non-actor DI dependencies (e.g., `IHttpClientFactory`) SHALL use
`resolver.Props<T>()` for prop creation. Actors without DI dependencies SHALL
use `Props.Create(() => new T())`.

The builder chain SHALL order registrations so that actors without actor
dependencies are registered before actors that depend on them via
`Context.GetActor<T>()`.

The setup SHALL NOT use manual `system.ActorOf()` + `registry.Register<T>()`
calls. The setup SHALL NOT use `ClusterSharding.Get(system).Start(...)` directly.

#### Scenario: Actor system starts with correct name
- **WHEN** the host boots
- **THEN** the Akka actor system is named `"funkarr"`

#### Scenario: SQLite persistence configured
- **WHEN** the host boots
- **THEN** a SQLite database file is created at the configured persistence path

#### Scenario: Health checks registered
- **WHEN** `GET /healthz` is requested
- **THEN** the response includes actor system liveness and persistence health status

#### Scenario: Cluster forms on single node
- **WHEN** the host boots
- **THEN** a single-node Akka.Cluster forms and reaches `Up` state

#### Scenario: Manager actors run as cluster singletons
- **WHEN** the host boots
- **THEN** `MediathekViewWebManager`, `MatchMagicManager`, and `SearchGatewayManager` are registered as cluster singletons in the `IActorRegistry`

#### Scenario: Worker actors run as sharded entities
- **WHEN** the host boots
- **THEN** `TvSearchWorker` and `MovieSearchWorker` shard regions are registered in the `IActorRegistry`

#### Scenario: Actors resolve peers via registry
- **WHEN** `TvSearchWorker` is created by the shard region
- **THEN** it resolves `MediathekViewWebManager` and `MatchMagicManager` via `Context.GetActor<T>()` from the `IActorRegistry`
