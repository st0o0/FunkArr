## ADDED Requirements

### Requirement: Servus AppBuilder startup
The host SHALL use `AppBuilder.Create(builder, b => b.Build())` with three setup
containers chained via `.WithSetup<T>()`:
1. `FunkArrServiceSetup : IServiceSetupContainer`
2. `FunkArrActorSystemSetup : ActorSystemSetupContainer`
3. `FunkArrApplicationSetup : ApplicationSetupContainer<WebApplication>`

The resulting runner SHALL be started with `await runner.RunAsync()`.

#### Scenario: Host boots with Servus AppBuilder
- **WHEN** `dotnet run` is executed from `src/FunkArr/`
- **THEN** the application starts without errors and logs startup messages to the console

#### Scenario: Setup containers are invoked in order
- **WHEN** the host boots
- **THEN** `FunkArrServiceSetup.SetupServices` runs before `FunkArrActorSystemSetup.BuildSystem` which runs before `FunkArrApplicationSetup.SetupApplication`

### Requirement: Serilog structured logging
The host SHALL configure Serilog before the AppBuilder chain. It SHALL call
`builder.Services.AddSerilog(...)` with console sink using template
`[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}`
and enrichers for MachineName, ThreadId, LogContext, and ApplicationVersion.
`builder.Logging.ClearProviders()` SHALL be called to remove default providers.

#### Scenario: Console log output uses structured template
- **WHEN** the application starts
- **THEN** log output follows the format `[HH:mm:ss INF] [SourceContext] Message`

#### Scenario: Akka.NET logs flow through Serilog
- **WHEN** the Akka actor system logs a lifecycle event
- **THEN** the message appears in Serilog console output with the same template

### Requirement: Akka.NET actor system configuration
`FunkArrActorSystemSetup` SHALL configure the actor system with name `"funkarr"`.
It SHALL clear default loggers and add `LoggerFactory` so Akka logs flow through
Serilog. It SHALL NOT configure persistence in this change.

#### Scenario: Actor system starts with correct name
- **WHEN** the host boots
- **THEN** the Akka actor system is named `"funkarr"`

#### Scenario: No persistence configured
- **WHEN** the host boots
- **THEN** no SQLite database file is created

### Requirement: FunkArrOptions binding
`FunkArrServiceSetup` SHALL register `FunkArrOptions` bound to config section `"FunkArr"`
with `.ValidateOnStart()`. `FunkArrOptions` SHALL have properties:
- `ApiKey` (string, default `"funkarr-default-api-key"`)
- `PersistencePath` (string, default `"data/funkarr.db"`)

#### Scenario: Default options bind without configuration
- **WHEN** no `FunkArr` section exists in configuration
- **THEN** `FunkArrOptions.ApiKey` equals `"funkarr-default-api-key"` and `PersistencePath` equals `"data/funkarr.db"`

#### Scenario: Environment variable overrides option
- **WHEN** `FunkArr__ApiKey` is set to `"custom-key"`
- **THEN** `FunkArrOptions.ApiKey` equals `"custom-key"`

#### Scenario: Invalid options fail startup
- **WHEN** `FunkArrOptions` validation fails
- **THEN** the host fails to start with an `OptionsValidationException`

### Requirement: Health and liveness endpoints
`FunkArrApplicationSetup` SHALL map:
- `GET /healthz` — ASP.NET health checks endpoint (200 for healthy/degraded, 503 for unhealthy)
- `GET /alive` — simple liveness probe returning 200 with body `"Alive"`

#### Scenario: Liveness probe responds
- **WHEN** `GET /alive` is requested
- **THEN** the response status is 200 and body is `"Alive"`

#### Scenario: Health check responds when healthy
- **WHEN** `GET /healthz` is requested and all health checks pass
- **THEN** the response status is 200

### Requirement: Kestrel HTTP configuration
The host SHALL listen on port 8080 by default using HTTP/1.1. The port SHALL be
overridable via `ASPNETCORE_URLS` environment variable.

#### Scenario: Default port binding
- **WHEN** `ASPNETCORE_URLS` is not set
- **THEN** Kestrel listens on `http://0.0.0.0:8080`

#### Scenario: Custom port via environment variable
- **WHEN** `ASPNETCORE_URLS` is set to `http://0.0.0.0:5000`
- **THEN** Kestrel listens on port 5000

### Requirement: Configuration files
The host SHALL load configuration from:
1. `appsettings.json` (required, production defaults)
2. `appsettings.Development.json` (optional, dev overrides)
3. Environment variables

`appsettings.json` SHALL contain sensible defaults including the `FunkArr` section
with `ApiKey` and `PersistencePath`, and Serilog minimum level set to `Information`
with `Microsoft.AspNetCore` override to `Warning`.

#### Scenario: Development overrides apply
- **WHEN** running in Development environment
- **THEN** `appsettings.Development.json` values override `appsettings.json`

#### Scenario: Environment variables override all files
- **WHEN** `FunkArr__ApiKey` is set as an environment variable
- **THEN** it takes precedence over the value in appsettings.json
