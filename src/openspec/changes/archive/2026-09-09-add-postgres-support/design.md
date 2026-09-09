## Context

FunkArr uses `Akka.Persistence.Sql.Hosting` with `ProviderName.SQLiteMS` and `Microsoft.Data.Sqlite` for journal and snapshot persistence. The `WithSqlPersistence` API is provider-agnostic via LinqToDB - switching to PostgreSQL requires only a different connection string and provider name, plus the `Npgsql` ADO.NET driver.

Current persistence setup lives in `AkkaSetupContainer.cs` (line 33). Configuration flows through `FunkArrOptions.DataPath` into `DataPaths.Database` which builds the SQLite file path.

## Goals / Non-Goals

**Goals:**
- Add PostgreSQL as an opt-in persistence backend
- SQLite remains the zero-config default
- Provider selection based on a single env var (`FunkArr__Postgres__Host`)
- Docker Compose overlay for Postgres dev environments

**Non-Goals:**
- Data migration from SQLite to PostgreSQL (users start fresh)
- Support for other databases (MySQL, SQL Server)
- Connection pooling or advanced Npgsql configuration
- Removing or deprecating SQLite

## Decisions

### 1. Detection via Host presence

Switch to PostgreSQL when `PostgresOptions.Host` is non-empty. No explicit "provider" toggle.

**Why**: One env var is the simplest possible opt-in. The docker-compose.example.yml already documents this convention. A separate "DatabaseProvider" enum adds config surface without value.

### 2. PostgresOptions as a sealed class with ToConnectionString()

Bind `FunkArr:Postgres` section to a `PostgresOptions` class with `Host`, `Port`, `User`, `Password`, `Database`. The class builds the Npgsql connection string internally.

**Why**: Keeps connection string construction in one place. Individual env vars (`FunkArr__Postgres__Host`, etc.) are more compose-friendly than a raw connection string.

### 3. Npgsql added to FunkArr.Core

The `Npgsql` package reference goes in `FunkArr.Core.csproj` alongside the existing `Akka.Persistence.Sql.Hosting`. The host project already references Core transitively.

**Why**: Core owns the persistence provider setup and the options types. Adding it only to the host project would break the pattern where Core bundles framework refs.

### 4. Compose overlay, not inline

`docker-compose.postgres.yml` is a standalone overlay used with `-f` stacking, not merged into `docker-compose.dev.yml`.

**Why**: Dev compose stays SQLite by default (simple, no extra service). Users opt into Postgres explicitly. No breaking change.

## Risks / Trade-offs

- **[Schema drift]** SQLite and PostgreSQL auto-initialize may produce subtly different schemas. -> Mitigation: `Akka.Persistence.Sql` uses the same DDL generation for both; auto-initialize handles this.
- **[No migration path]** Users switching from SQLite to Postgres lose existing state. -> Acceptable at v0.x. Document in docker-compose.example.yml comments.
- **[Connection string in env vars]** Password visible in compose files and `docker inspect`. -> Standard practice for self-hosted arr stacks. Not a regression from current SQLite (no auth).
