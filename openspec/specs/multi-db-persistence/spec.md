## Purpose

Pluggable Akka.Persistence backend supporting SQLite (default) and PostgreSQL, configurable via `FunkArr:Postgres:*` options.

## Requirements

### Requirement: Persistence provider selection
The system SHALL support SQLite and PostgreSQL as Akka.Persistence backends. SQLite SHALL be the default provider. When `PostgresOptions` is configured (i.e. `Host` is non-empty), the system SHALL use PostgreSQL; otherwise it SHALL silently fall back to SQLite.

#### Scenario: Default SQLite persistence
- **WHEN** no `FunkArr:Postgres:Host` is configured (or it is empty)
- **THEN** the system SHALL use SQLite with a connection string derived from `PersistencePath` (`data/funkarr.db` by default)

#### Scenario: Explicit SQLite with custom path
- **WHEN** `PostgresOptions.IsConfigured` is false and `PersistencePath` is `custom/path.db`
- **THEN** the system SHALL connect to SQLite at the absolute path resolved from `custom/path.db`

#### Scenario: PostgreSQL persistence
- **WHEN** `FunkArr:Postgres:Host` is `db`, `FunkArr:Postgres:Port` is `5432`, `FunkArr:Postgres:User` is `funkarr`, `FunkArr:Postgres:Password` is `secret`, and `FunkArr:Postgres:Database` is `funkarr`
- **THEN** the system SHALL use PostgreSQL with a connection string built by `PostgresOptions.BuildConnectionString()` and auto-initialize journal/snapshot tables

#### Scenario: PostgreSQL not configured falls back to SQLite
- **WHEN** `FunkArr:Postgres:Host` is null or empty
- **THEN** `PostgresOptions.IsConfigured` SHALL return false and the system SHALL silently fall back to SQLite without throwing any exception

### Requirement: Persistence configuration model
The system SHALL expose PostgreSQL configuration through a `PostgresOptions` class under the `FunkArr:Postgres` configuration section with properties `Host`, `Port`, `User`, `Password`, and `Database`. The class SHALL expose an `IsConfigured` property that returns true when `Host` is non-empty, and a `BuildConnectionString()` method that constructs the connection string from the individual fields.

#### Scenario: Configuration via environment variables
- **WHEN** `FunkArr__Postgres__Host=db`, `FunkArr__Postgres__Port=5432`, `FunkArr__Postgres__User=funkarr`, `FunkArr__Postgres__Password=secret`, `FunkArr__Postgres__Database=funkarr` are set
- **THEN** `PostgresOptions.IsConfigured` SHALL return true and `BuildConnectionString()` SHALL produce a valid PostgreSQL connection string

#### Scenario: Configuration via appsettings.json
- **WHEN** appsettings.json contains `"FunkArr": { "Postgres": { "Host": "db", "Port": 5432, "User": "funkarr", "Password": "secret", "Database": "funkarr" } }`
- **THEN** the system SHALL use PostgreSQL with the connection string built from those fields

### Requirement: Auto-initialization of persistence schema
The system SHALL auto-initialize journal and snapshot store tables on first connection for both SQLite and PostgreSQL providers.

#### Scenario: First startup with PostgreSQL
- **WHEN** the system starts for the first time with PostgreSQL configured and the database exists but has no Akka tables
- **THEN** Akka.Persistence SHALL create the required journal and snapshot tables automatically
