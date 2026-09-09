## ADDED Requirements

### Requirement: PostgreSQL persistence opt-in via configuration
The system SHALL use PostgreSQL for Akka.Persistence journal and snapshots when `FunkArr__Postgres__Host` is configured. The system SHALL fall back to SQLite when `FunkArr__Postgres__Host` is not set or empty.

#### Scenario: PostgreSQL activated when Host is set
- **WHEN** `FunkArr__Postgres__Host` is set to a non-empty value
- **THEN** the system connects to PostgreSQL using the configured host, port, user, password, and database

#### Scenario: SQLite remains default when no Postgres config
- **WHEN** `FunkArr__Postgres__Host` is not set or empty
- **THEN** the system uses SQLite at `{DataPath}/funkarr.db` as before

### Requirement: PostgreSQL connection string construction
The system SHALL construct a valid Npgsql connection string from individual configuration properties: `Host`, `Port` (default 5432), `User`, `Password`, and `Database` (default "funkarr").

#### Scenario: Connection string with all properties set
- **WHEN** Host="db", Port=5432, User="funkarr", Password="secret", Database="funkarr"
- **THEN** the connection string SHALL be `Host=db;Port=5432;Username=funkarr;Password=secret;Database=funkarr`

#### Scenario: Connection string with defaults
- **WHEN** Host="db", User="funkarr", Password="secret", and Port/Database are not set
- **THEN** Port defaults to 5432 and Database defaults to "funkarr"

### Requirement: Auto-initialize database schema
The system SHALL auto-initialize the PostgreSQL journal and snapshot tables on startup, matching the existing SQLite behavior.

#### Scenario: First startup with empty PostgreSQL database
- **WHEN** the system starts with PostgreSQL configured and the database has no Akka tables
- **THEN** the journal and snapshot tables are created automatically

### Requirement: Docker Compose overlay for PostgreSQL
A `docker-compose.postgres.yml` overlay SHALL provide a PostgreSQL 17 service with healthcheck and pre-configured environment variables for FunkArr.

#### Scenario: Dev environment with Postgres via overlay
- **WHEN** running `docker compose -f docker-compose.dev.yml -f docker-compose.postgres.yml up`
- **THEN** a PostgreSQL container starts, and FunkArr connects to it for persistence

#### Scenario: FunkArr waits for healthy database
- **WHEN** the compose stack starts
- **THEN** the FunkArr service SHALL depend on the db service's healthcheck passing
