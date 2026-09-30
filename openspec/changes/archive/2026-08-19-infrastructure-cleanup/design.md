## Context

FunkArr uses Akka.Persistence.Sql with a hardcoded SQLite provider. File I/O (temp file creation, output directory creation, cleanup) is scattered across `DownloadQueueActor` and `MuxingService`. The startup in `Program.cs` is already clean (Serilog + Kestrel inline, everything else in containers), but `RuleSetSettings` is dead code duplicating fields from `FunkArrOptions`.

The reference project njord has already solved multi-DB persistence with a `PersistenceProvider` enum + `PersistenceOptions` class pattern. We follow this pattern exactly.

## Goals / Non-Goals

**Goals:**
- Support SQLite (default) and PostgreSQL for Akka.Persistence journal/snapshots
- Extract file I/O into a testable `IFileService` interface
- Remove dead `RuleSetSettings` class
- Keep backward-compatible configuration (existing `PersistencePath` still works for SQLite)

**Non-Goals:**
- Library-style file management (renaming, hardlinking) — Sonarr/Radarr handles that
- Entity Framework or direct SQL queries — persistence stays Akka event-sourced
- Moving Serilog/Kestrel config out of `Program.cs` — njord keeps them inline too, they need `WebApplicationBuilder` access

## Decisions

### 1. Persistence configuration follows njord pattern

**Choice:** Nested `PersistenceOptions` class with `PersistenceProvider` enum inside `FunkArrOptions`, provider selection logic in `FunkArrActorSystemSetup`.

**Alternatives considered:**
- Separate config section (`Persistence:*` at root level) — rejected because `FunkArr:*` namespace keeps all config together, matches njord.
- Connection string factory service — over-engineered for two providers with a simple switch expression.

**Config shape:**
```json
{
  "FunkArr": {
    "PersistencePath": "data/funkarr.db",
    "Persistence": {
      "Provider": "Sqlite",
      "ConnectionString": null
    }
  }
}
```

- SQLite: auto-derives connection string from `PersistencePath` when `ConnectionString` is null.
- PostgreSQL: requires explicit `ConnectionString`, throws `InvalidOperationException` if missing.
- `PersistencePath` stays at the top level (not nested) for backward compatibility and SQLite ergonomics.

### 2. File operations service

**Choice:** `IFileService` interface with `FileService` implementation, registered as singleton in `FunkArrServiceSetup`.

**Responsibilities:**
- `EnsureDirectoriesExist(tempPath, downloadPath)` — called once at actor startup
- `GetTempVideoPath(tempPath, nzoId)` → `{tempPath}/{nzoId}.mp4`
- `GetTempSubtitlePath(tempPath, nzoId)` → `{tempPath}/{nzoId}.srt`
- `GetOutputPath(downloadPath, title)` → `{downloadPath}/{title}/{title}.mkv`
- `CleanupTempFiles(videoPath, subtitlePaths)` — safe deletion
- `WriteSubtitleAsync(path, content)` — write subtitle bytes to disk

**What stays in MuxingService:** FFmpeg invocation, subtitle format conversion (VTT/TTML → SRT). These are muxing concerns, not file management. `MuxingService` receives an `IFileService` for cleanup and path operations.

**What stays in DownloadQueueActor:** HTTP download streaming to temp file. The actor gets temp file paths from `IFileService` but does the actual streaming (it needs progress reporting integrated with the Akka Stream pipeline).

**Alternatives considered:**
- Move all file I/O into `MuxingService` — doesn't help, the download actor also does file I/O.
- Abstract `IFileSystem` (like System.IO.Abstractions) — too heavy for what we need. A focused interface is better.

### 3. Dead code removal

`RuleSetSettings` is unused — its fields (`SourceUrl`, `RuleSetPath`, `RefreshIntervalMinutes`) are duplicates of properties already on `FunkArrOptions`. Remove the class entirely.

## Risks / Trade-offs

- **[Risk] PostgreSQL connection validation at startup** → Mitigated by `autoInitialize: true` which creates tables on first connect. If the DB is unreachable, `BackoffSupervisor` retries the persistent actor.
- **[Risk] Breaking config for existing users** → Mitigated by keeping `PersistencePath` at the same level. SQLite is the default provider, no config change needed for existing setups.
- **[Trade-off] `IFileService` adds indirection** → Acceptable because it makes file operations testable and centralizes path construction logic that's currently duplicated.

## Migration Plan

1. Add `PersistenceProvider` enum, `PersistenceOptions` class
2. Add `Persistence` property to `FunkArrOptions`, update validator
3. Update `FunkArrActorSystemSetup` with provider selection logic
4. Add `Npgsql` package to `Directory.Packages.props`
5. Create `IFileService` / `FileService`, register in `FunkArrServiceSetup`
6. Refactor `MuxingService` and `DownloadQueueActor` to use `IFileService`
7. Remove `RuleSetSettings`
8. Update `appsettings.json` / `appsettings.Development.json`
9. Update `docker-compose.example.yml` with PostgreSQL example

No data migration needed — Akka.Persistence.Sql uses the same journal/snapshot schema across providers.
