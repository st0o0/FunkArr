## Why

FunkArr currently uses SQLite for Akka.Persistence (journal + snapshots). SQLite works well for single-node setups but becomes a bottleneck for users who want better durability, concurrent access, or integration with existing database infrastructure. PostgreSQL is the natural upgrade path - widely deployed, battle-tested with Akka.Persistence.Sql, and the standard choice in Docker-based arr stacks.

## What Changes

- Add `Npgsql` NuGet package for PostgreSQL ADO.NET connectivity
- Introduce `PostgresOptions` configuration record bound to `FunkArr:Postgres` section
- Modify `AkkaSetupContainer` to switch between SQLite (default) and PostgreSQL based on whether `Postgres__Host` is configured
- Add `docker-compose.postgres.yml` as a compose overlay for dev environments
- Verify `docker-compose.example.yml` documents the Postgres env vars correctly

## Capabilities

### New Capabilities

- `postgres-persistence`: Optional PostgreSQL persistence backend, activated by setting `FunkArr__Postgres__Host`. Includes connection string construction, provider selection, and Docker Compose overlay for development.

### Modified Capabilities

None - no existing specs.

## Impact

- **NuGet**: New `Npgsql` package in `Directory.Packages.props` and `FunkArr.Core.csproj`
- **Configuration**: New `PostgresOptions` class in `FunkArr.Core`, new binding in `ServiceSetupContainer`
- **Persistence**: `AkkaSetupContainer` gains provider-switching logic (SQLite vs PostgreSQL)
- **Docker**: New `docker-compose.postgres.yml` overlay file
- **Breaking**: None - SQLite remains the default, Postgres is opt-in
