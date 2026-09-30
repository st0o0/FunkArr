## MODIFIED Requirements

### Requirement: Configuration via environment variables
The application SHALL be configurable via environment variables and appsettings.json for all operational parameters including API key, download paths, concurrency limits, log format, and persistence provider/connection string. The application SHALL listen on a hardcoded internal port of `6969` and SHALL NOT expose a configurable `HttpPort` option.

#### Scenario: Docker environment configuration
- **WHEN** the application runs in Docker with `FunkArr__ApiKey=mykey` and `FunkArr__DownloadPath=/media/downloads`
- **THEN** the application uses "mykey" as the API key and "/media/downloads" as the output directory

#### Scenario: Internal port is fixed
- **WHEN** the application starts in any environment
- **THEN** Kestrel SHALL listen on `http://+:6969` unless `ASPNETCORE_URLS` is explicitly set

#### Scenario: Docker port mapping
- **WHEN** the Docker container is started with `-p 8080:6969`
- **THEN** the application is accessible on the host at port 8080

#### Scenario: Persistence provider via environment variables
- **WHEN** the application runs in Docker with `FunkArr__Persistence__Provider=PostgreSql` and `FunkArr__Persistence__ConnectionString=Host=db;Database=funkarr;...`
- **THEN** the application uses PostgreSQL for Akka.Persistence

## REMOVED Requirements

### Requirement: RuleSetSettings class
**Reason**: `RuleSetSettings` is dead code — its fields (`SourceUrl`, `RuleSetPath`, `RefreshIntervalMinutes`) duplicate properties already on `FunkArrOptions` which is the actual config source.
**Migration**: No migration needed. `FunkArrOptions` already provides these values.
