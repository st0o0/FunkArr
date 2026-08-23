## Purpose

Solution structure, .NET 10 target, central package management, Servus AppBuilder startup, Serilog logging, Docker build, CI/CD, and release-please versioning.

## Requirements

### Requirement: .NET 10 solution structure
The project SHALL use .NET 10 (net10.0) with a single Web SDK project, central package management via Directory.Packages.props, and shared build properties via Directory.Build.props.

#### Scenario: Solution builds successfully
- **WHEN** `dotnet build` is run on the solution
- **THEN** the solution compiles without errors targeting net10.0

### Requirement: Servus AppBuilder startup
The application SHALL use the Servus AppBuilder pattern with three setup containers: ServiceSetup (DI), ActorSystemSetup (Akka.NET), and ApplicationSetup (HTTP endpoints).

#### Scenario: Application starts with all subsystems
- **WHEN** the application starts
- **THEN** DI services are registered, the Akka actor system initializes with configured actors, and HTTP endpoints are mapped

### Requirement: Serilog structured logging
The application SHALL use Serilog for structured logging with console sink, environment and thread enrichers, and Akka.Logger.Serilog integration.

#### Scenario: Log output includes structured context
- **WHEN** a search request is processed
- **THEN** log entries include structured properties (request parameters, result count, duration) in addition to the message

### Requirement: Health check endpoints
The application SHALL expose health check endpoints at `/healthz` (liveness) and `/alive` (readiness).

#### Scenario: Healthy application
- **WHEN** a client requests `GET /healthz`
- **THEN** the system returns HTTP 200 when all subsystems (actor system, FFmpeg availability) are healthy

#### Scenario: FFmpeg not available
- **WHEN** FFmpeg is not found on PATH or at the configured location
- **THEN** the health check reports degraded status indicating FFmpeg is missing

### Requirement: Configuration via environment variables
The application SHALL be configurable via environment variables and appsettings.json for all operational parameters including API key, download paths, and concurrency limits. The application SHALL listen on a hardcoded internal port of `6969` and SHALL NOT expose a configurable `HttpPort` option.

#### Scenario: Docker environment configuration
- **WHEN** the application runs in Docker with `FunkArr__ApiKey=mykey` and `FunkArr__DownloadPath=/media/downloads`
- **THEN** the application uses "mykey" as the API key and "/media/downloads" as the output directory

#### Scenario: Internal port is fixed
- **WHEN** the application starts in any environment
- **THEN** Kestrel SHALL listen on `http://+:6969` unless `ASPNETCORE_URLS` is explicitly set

#### Scenario: Docker port mapping
- **WHEN** the Docker container is started with `-p 8080:6969`
- **THEN** the application is accessible on the host at port 8080

#### Scenario: PostgreSQL persistence via environment variables
- **WHEN** the application runs in Docker with `FunkArr__Postgres__Host=db`, `FunkArr__Postgres__Port=5432`, `FunkArr__Postgres__User=funkarr`, `FunkArr__Postgres__Password=secret`, `FunkArr__Postgres__Database=funkarr`
- **THEN** the application uses PostgreSQL for Akka.Persistence with a connection string built from `PostgresOptions.BuildConnectionString()`

### Requirement: Multi-arch Docker image
The project SHALL produce a multi-arch Docker image (linux/amd64, linux/arm64) based on `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra` with FFmpeg included. The Dockerfile SHALL use a `runtime-deps:10.0-noble` prep stage that installs FFmpeg, and the final `aspnet:10.0-noble-chiseled-extra` stage copies the FFmpeg binary from the prep stage.

#### Scenario: Docker image runs on ARM64
- **WHEN** the Docker image is pulled on a Raspberry Pi 4 or similar ARM64 host
- **THEN** the correct architecture variant is selected and the application starts successfully

### Requirement: CI/CD pipeline
The project SHALL use GitHub Actions for CI (build, test, lint on PR) and release (versioning via release-please, multi-arch Docker build, push to GHCR).

#### Scenario: PR triggers CI
- **WHEN** a pull request is opened
- **THEN** GitHub Actions runs `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`

#### Scenario: Merge to main triggers release
- **WHEN** a PR is merged to main and release-please determines a version bump is needed
- **THEN** a release PR is created, and upon merge, the Docker image is built and pushed to GHCR with the version tag
