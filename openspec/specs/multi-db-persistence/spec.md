## Purpose

Pluggable Akka.Persistence backend supporting SQLite (default) and PostgreSQL, configurable via `FunkArr:Persistence:*` options.

## Requirements

### Requirement: Persistence provider selection
The system SHALL support SQLite and PostgreSQL as Akka.Persistence backends, selectable via the `FunkArr:Persistence:Provider` configuration option. SQLite SHALL be the default provider.

#### Scenario: Default SQLite persistence
- **WHEN** no `Persistence:Provider` is configured
- **THEN** the system SHALL use SQLite with a connection string derived from `PersistencePath` (`data/funkarr.db` by default)

#### Scenario: Explicit SQLite with custom path
- **WHEN** `Persistence:Provider` is `Sqlite` and `PersistencePath` is `custom/path.db`
- **THEN** the system SHALL connect to SQLite at the absolute path resolved from `custom/path.db`

#### Scenario: SQLite with explicit connection string
- **WHEN** `Persistence:Provider` is `Sqlite` and `Persistence:ConnectionString` is set
- **THEN** the system SHALL use the explicit connection string instead of deriving one from `PersistencePath`

#### Scenario: PostgreSQL persistence
- **WHEN** `Persistence:Provider` is `PostgreSql` and `Persistence:ConnectionString` is `Host=db;Database=funkarr;Username=funkarr;Password=secret`
- **THEN** the system SHALL use PostgreSQL with the provided connection string and auto-initialize journal/snapshot tables

#### Scenario: PostgreSQL without connection string
- **WHEN** `Persistence:Provider` is `PostgreSql` and `Persistence:ConnectionString` is null or empty
- **THEN** the system SHALL throw `InvalidOperationException` at startup with a message indicating the connection string is required

### Requirement: Persistence configuration model
The system SHALL expose persistence configuration through a `PersistenceOptions` class nested under `FunkArrOptions` with a `PersistenceProvider` enum.

#### Scenario: Configuration via environment variables
- **WHEN** `FunkArr__Persistence__Provider=PostgreSql` and `FunkArr__Persistence__ConnectionString=Host=db;...` are set
- **THEN** the system SHALL use PostgreSQL with the provided connection string

#### Scenario: Configuration via appsettings.json
- **WHEN** appsettings.json contains `"FunkArr": { "Persistence": { "Provider": "PostgreSql", "ConnectionString": "..." } }`
- **THEN** the system SHALL use PostgreSQL with the provided connection string

### Requirement: Auto-initialization of persistence schema
The system SHALL auto-initialize journal and snapshot store tables on first connection for both SQLite and PostgreSQL providers.

#### Scenario: First startup with PostgreSQL
- **WHEN** the system starts for the first time with PostgreSQL configured and the database exists but has no Akka tables
- **THEN** Akka.Persistence SHALL create the required journal and snapshot tables automatically
