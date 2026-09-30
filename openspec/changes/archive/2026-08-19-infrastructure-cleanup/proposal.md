## Why

FunkArr's persistence layer is hardcoded to SQLite, and the startup code has configuration concerns scattered outside of setup containers. Users running FunkArr alongside an existing PostgreSQL instance (common in *arr stacks) cannot share infrastructure. The codebase also has dead code (`RuleSetSettings`) and file operations embedded directly in actors/services rather than centralized.

## What Changes

- **Multi-database persistence**: Support SQLite (default) and PostgreSQL for Akka.Persistence journal/snapshots, following the njord pattern (`PersistenceProvider` enum + `PersistenceOptions` class + provider selection in `ActorSystemSetup`).
- **Configuration restructure**: Nest persistence config under `FunkArr:Persistence` section. Keep `PersistencePath` as SQLite shorthand. Add `Provider` and `ConnectionString` fields.
- **File operations service**: Extract file I/O (temp file management, output directory creation, cleanup) from `MuxingService` and `DownloadQueueActor` into a dedicated `IFileService` for testability.
- **Startup cleanup**: Ensure `Program.cs` only contains what requires `WebApplicationBuilder` access (Serilog, Kestrel). Remove dead `RuleSetSettings` class. Verify all DI registrations are in `FunkArrServiceSetup`.
- **Validation**: Extend `FunkArrOptionsValidator` to validate persistence configuration (require `ConnectionString` when provider is PostgreSQL).

## Capabilities

### New Capabilities

- `multi-db-persistence`: Database provider abstraction supporting SQLite and PostgreSQL for Akka.Persistence, with configuration validation and auto-initialization.
- `file-operations`: Centralized file operations service handling temp file lifecycle, output directory creation, and cleanup — extracted from MuxingService and DownloadQueueActor.

### Modified Capabilities

- `project-infrastructure`: Startup cleanup — remove dead `RuleSetSettings`, ensure all registrations in setup containers, restructure persistence configuration under nested options.

## Impact

- **Configuration**: New `FunkArr:Persistence:Provider` and `FunkArr:Persistence:ConnectionString` settings. `PersistencePath` remains for SQLite backward compatibility.
- **Dependencies**: Add `Npgsql` package for PostgreSQL support.
- **Docker**: `docker-compose.example.yml` updated with PostgreSQL option.
- **Code**: `MuxingService`, `DownloadQueueActor` lose direct file I/O — delegated to `IFileService`. `RuleSetSettings` removed. `FunkArrActorSystemSetup` gains provider selection logic.
- **Breaking**: None. SQLite remains the default; existing configs work unchanged.
