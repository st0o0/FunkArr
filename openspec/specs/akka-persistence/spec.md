## Purpose

Akka.NET persistence configuration: SQLite journal/snapshot storage, persistence health checks, and actor system liveness verification.

## Requirements

### Requirement: SQLite persistence via WithSqlPersistence
`FunkArrActorSystemSetup` SHALL configure Akka.Persistence.Sql with SQLite using
`WithSqlPersistence(connectionString, ProviderName.SQLiteMS, autoInitialize: true)`.
The connection string SHALL be built from `FunkArrOptions.PersistencePath` as
`Data Source={Path.GetFullPath(persistencePath)}`.

#### Scenario: SQLite database created on startup
- **WHEN** the host boots
- **THEN** a SQLite database file exists at the path specified by `FunkArrOptions.PersistencePath`

#### Scenario: Default persistence path
- **WHEN** no `FunkArr:PersistencePath` configuration is provided
- **THEN** the database is created at `data/funkarr.db` relative to the working directory

### Requirement: Persistence health checks
Both the journal and snapshot builders SHALL register ASP.NET health checks via
`.WithHealthCheck()`. These health checks SHALL be aggregated by the existing
`/healthz` endpoint.

#### Scenario: Healthy persistence reported
- **WHEN** `GET /healthz` is requested and the SQLite database is accessible
- **THEN** the response status is 200

#### Scenario: Unhealthy persistence reported
- **WHEN** the SQLite database file is inaccessible or corrupt
- **THEN** `GET /healthz` returns 503

### Requirement: Actor system liveness check
`FunkArrActorSystemSetup` SHALL call `.WithActorSystemLivenessCheck()` to register
a health check that verifies the actor system is running. It SHALL NOT use
`WithAkkaClusterReadinessCheck()` (single-node deployment).

#### Scenario: Actor system liveness reported
- **WHEN** `GET /healthz` is requested and the actor system is running
- **THEN** the liveness check passes
