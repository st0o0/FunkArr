## MODIFIED Requirements

### Requirement: Configuration via environment variables
The application SHALL be configurable via environment variables and appsettings.json for all operational parameters including API key, download paths, concurrency limits, and log format. The application SHALL listen on a hardcoded internal port of `6969` and SHALL NOT expose a configurable `HttpPort` option.

#### Scenario: Docker environment configuration
- **WHEN** the application runs in Docker with `FunkArr__ApiKey=mykey` and `FunkArr__DownloadPath=/media/downloads`
- **THEN** the application uses "mykey" as the API key and "/media/downloads" as the output directory

#### Scenario: Internal port is fixed
- **WHEN** the application starts in any environment
- **THEN** Kestrel SHALL listen on `http://+:6969` unless `ASPNETCORE_URLS` is explicitly set

#### Scenario: Docker port mapping
- **WHEN** the Docker container is started with `-p 8080:6969`
- **THEN** the application is accessible on the host at port 8080

## REMOVED Requirements

### Requirement: HttpPort configuration option
**Reason**: Internal container port is infrastructure, not application configuration. Docker port mapping is the standard mechanism for exposing services on different ports.
**Migration**: Remove `FunkArr__HttpPort` environment variable. Use Docker port mapping (`-p <host>:6969`) or `ASPNETCORE_URLS` environment variable instead.
