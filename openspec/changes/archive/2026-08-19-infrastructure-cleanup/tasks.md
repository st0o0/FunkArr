## 1. Multi-DB Persistence

- [x] 1.1 Add `PersistenceProvider` enum (`Sqlite`, `PostgreSql`) in `Configuration/PersistenceProvider.cs`
- [x] 1.2 Add `PersistenceOptions` class (`Provider`, `ConnectionString`) in `Configuration/PersistenceOptions.cs`
- [x] 1.3 Add `Persistence` property to `FunkArrOptions`, keep `PersistencePath` at top level
- [x] 1.4 Update `FunkArrOptionsValidator` to validate persistence config (require ConnectionString for PostgreSQL)
- [x] 1.5 Update `FunkArrActorSystemSetup.BuildSystem` with provider selection logic (follow njord pattern)
- [x] 1.6 Add `Npgsql` package to `Directory.Packages.props` and `FunkArr.csproj`
- [x] 1.7 Update `appsettings.json` and `appsettings.Development.json` with `Persistence` section

## 2. File Operations Service

- [x] 2.1 Create `IFileService` interface in `Shared/IFileService.cs` with path construction and file operation methods
- [x] 2.2 Create `FileService` implementation in `Shared/FileService.cs`
- [x] 2.3 Register `FileService` as singleton in `FunkArrServiceSetup`
- [x] 2.4 Refactor `MuxingService` to use `IFileService` for output path construction and temp file cleanup
- [x] 2.5 Refactor `DownloadQueueActor` to use `IFileService` for temp paths, directory creation, and subtitle writing

## 3. Cleanup

- [x] 3.1 Delete `RuleSet/RuleSetSettings.cs`
- [x] 3.2 Verify no references to `RuleSetSettings` remain
- [x] 3.3 Update `docker-compose.example.yml` with PostgreSQL service and config example

## 4. Validation

- [x] 4.1 Build succeeds with no warnings
- [x] 4.2 All existing tests pass
- [x] 4.3 Write unit tests for `FileService` path construction methods
- [x] 4.4 Write unit tests for `PersistenceOptions` validation (SQLite default, PostgreSQL requires connection string)
