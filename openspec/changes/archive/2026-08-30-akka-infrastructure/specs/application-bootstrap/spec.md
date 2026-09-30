## MODIFIED Requirements

### Requirement: Akka.NET actor system configuration
`FunkArrActorSystemSetup` SHALL configure the actor system with name `"funkarr"`.
It SHALL clear default loggers and add `LoggerFactory` so Akka logs flow through
Serilog. It SHALL configure SQLite persistence via `WithSqlPersistence` with
journal and snapshot health checks. It SHALL register an actor system liveness
health check via `WithActorSystemLivenessCheck()`.

#### Scenario: Actor system starts with correct name
- **WHEN** the host boots
- **THEN** the Akka actor system is named `"funkarr"`

#### Scenario: SQLite persistence configured
- **WHEN** the host boots
- **THEN** a SQLite database file is created at the configured persistence path

#### Scenario: Health checks registered
- **WHEN** `GET /healthz` is requested
- **THEN** the response includes actor system liveness and persistence health status
