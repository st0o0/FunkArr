## Purpose

Host bootstrap configuration using Servus AppBuilder, Serilog structured logging, Akka.NET actor system, configuration binding, and health endpoints.

## Requirements

### Requirement: Servus AppBuilder startup
The host SHALL use `AppBuilder.Create(builder, b => b.Build())` with four setup
containers chained via `.WithSetup<T>()`:
1. `LoggingSetupContainer : IServiceSetupContainer`
2. `FunkArrServiceSetup : IServiceSetupContainer`
3. `FunkArrActorSystemSetup : ActorSystemSetupContainer`
4. `FunkArrApplicationSetup : ApplicationSetupContainer<WebApplication>`

The AppBuilder chain and `await runner.RunAsync()` SHALL be wrapped in a
try/catch/finally block. The catch block SHALL call `Log.Fatal(ex, ...)` and
the finally block SHALL call `await Log.CloseAndFlushAsync()`.

#### Scenario: Host boots with Servus AppBuilder
- **WHEN** `dotnet run` is executed from `src/FunkArr/`
- **THEN** the application starts without errors and logs startup messages to the console

#### Scenario: Setup containers are invoked in order
- **WHEN** the host boots
- **THEN** `LoggingSetupContainer` runs first, then `FunkArrServiceSetup`, then `FunkArrActorSystemSetup`, then `FunkArrApplicationSetup`

### Requirement: Serilog structured logging
The host SHALL create a bootstrap logger before the AppBuilder chain via
`Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateBootstrapLogger()`.
The full Serilog configuration SHALL be handled by `LoggingSetupContainer`, not
inline in Program.cs. `builder.Logging.ClearProviders()` SHALL be called in
Program.cs before the AppBuilder chain.

#### Scenario: Console log output uses structured template
- **WHEN** the application starts
- **THEN** log output follows the format `[HH:mm:ss INF] [SourceContext] Message`

#### Scenario: Akka.NET logs flow through Serilog
- **WHEN** the Akka actor system logs a lifecycle event
- **THEN** the message appears in Serilog console output with the same template

### Requirement: Akka.NET actor system configuration
`FunkArrActorSystemSetup` SHALL configure the actor system with name `"funkarr"`.
It SHALL clear default loggers and add `LoggerFactory` so Akka logs flow through
Serilog. It SHALL configure SQLite persistence via `WithSqlPersistence` with
journal and snapshot health checks. It SHALL register an actor system liveness
health check via `WithActorSystemLivenessCheck()`.

It SHALL configure single-node clustering via `WithClustering()`.

It SHALL register all `*Manager` actors as cluster singletons via
`WithSingleton<T>()`. It SHALL register all `*Worker` actors as sharded entities
via `WithShardRegion<T>()`.

Actors with non-actor DI dependencies (e.g., `IHttpClientFactory`) SHALL use
`resolver.Props<T>()` for prop creation. Actors without DI dependencies SHALL
use `Props.Create(() => new T())`.

The builder chain SHALL order registrations so that actors without actor
dependencies are registered before actors that depend on them via
`Context.GetActor<T>()`.

The setup SHALL NOT use manual `system.ActorOf()` + `registry.Register<T>()`
calls. The setup SHALL NOT use `ClusterSharding.Get(system).Start(...)` directly.

#### Scenario: Actor system starts with correct name
- **WHEN** the host boots
- **THEN** the Akka actor system is named `"funkarr"`

#### Scenario: SQLite persistence configured
- **WHEN** the host boots
- **THEN** a SQLite database file is created at the configured persistence path

#### Scenario: Health checks registered
- **WHEN** `GET /healthz` is requested
- **THEN** the response includes actor system liveness and persistence health status

#### Scenario: Cluster forms on single node
- **WHEN** the host boots
- **THEN** a single-node Akka.Cluster forms and reaches `Up` state

#### Scenario: Manager actors run as cluster singletons
- **WHEN** the host boots
- **THEN** `MediathekViewWebManager`, `MatchMagicManager`, and `SearchGatewayManager` are registered as cluster singletons in the `IActorRegistry`

#### Scenario: Worker actors run as sharded entities
- **WHEN** the host boots
- **THEN** `TvSearchWorker` and `MovieSearchWorker` shard regions are registered in the `IActorRegistry`

#### Scenario: Actors resolve peers via registry
- **WHEN** `TvSearchWorker` is created by the shard region
- **THEN** it resolves `MediathekViewWebManager` and `MatchMagicManager` via `Context.GetActor<T>()` from the `IActorRegistry`

### Requirement: FunkArrOptions binding
`FunkArrServiceSetup` SHALL register `FunkArrOptions` bound to config section `"FunkArr"`
with `.ValidateOnStart()`. `FunkArrOptions` SHALL be defined in `FunkArr.Core` and have properties:
- `ApiKey` (string, default `"funkarr-default-api-key"`)
- `DataPath` (string, default `"data"`) — the single configurable storage root

`FunkArrOptions` SHALL expose the following computed read-only properties:
- `PersistencePath` — returns `Path.Combine(DataPath, "funkarr.db")`
- `DownloadPath` — returns `Path.Combine(DataPath, "downloads")`
- `RuleSetDataPath` — returns `Path.Combine(DataPath, "community")`

These computed properties SHALL NOT be bindable from configuration. Only `DataPath` SHALL be configurable.

`RuleSetUpdaterOptions` SHALL NOT have a `DataPath` property. Consumers needing the ruleset data path SHALL use `FunkArrOptions.RuleSetDataPath`.

#### Scenario: Default options bind without configuration
- **WHEN** no `FunkArr` section exists in configuration
- **THEN** `FunkArrOptions.ApiKey` equals `"funkarr-default-api-key"` and `DataPath` equals `"data"`

#### Scenario: Computed paths derive from DataPath
- **WHEN** `FunkArrOptions.DataPath` is `"/data"`
- **THEN** `PersistencePath` equals `"/data/funkarr.db"`, `DownloadPath` equals `"/data/downloads"`, and `RuleSetDataPath` equals `"/data/community"`

#### Scenario: Environment variable overrides DataPath
- **WHEN** `FunkArr__DataPath` is set to `"/custom/path"`
- **THEN** `FunkArrOptions.DataPath` equals `"/custom/path"` and all computed paths derive from it

#### Scenario: Environment variable overrides ApiKey
- **WHEN** `FunkArr__ApiKey` is set to `"custom-key"`
- **THEN** `FunkArrOptions.ApiKey` equals `"custom-key"`

#### Scenario: Invalid options fail startup
- **WHEN** `FunkArrOptions` validation fails
- **THEN** the host fails to start with an `OptionsValidationException`

### Requirement: Health and liveness endpoints
`FunkArrApplicationSetup` SHALL map:
- `GET /healthz` -- ASP.NET health checks endpoint (200 for healthy/degraded, 503 for unhealthy)
- `GET /alive` -- simple liveness probe returning 200 with body `"Alive"`
- `app.UseStaticFiles()` -- serve Vue frontend assets from dist/
- `app.MapRuleSetApi()` -- internal REST API for rulesets and scoring history
- `app.MapSetupApi()` -- internal REST API for setup health checks
- `app.MapIndexerApi()` -- Newznab indexer API (parameterless, dependencies resolved via DI)
- `app.MapDownloadApi()` -- SABnzbd download client API (parameterless, dependencies resolved via DI)
- SPA fallback route serving `index.html` for unmatched routes (mapped last)

`ApplicationSetupContainer` SHALL NOT resolve `IActorRegistry`, `IOptions<FunkArrOptions>`, or any `IActorRef` directly. All dependency resolution SHALL happen inside the endpoint handlers.

#### Scenario: Liveness probe responds
- **WHEN** `GET /alive` is requested
- **THEN** the response status is 200 and body is `"Alive"`

#### Scenario: ApplicationSetupContainer has no manual DI resolution
- **WHEN** reviewing `ApplicationSetupContainer.SetupApplication`
- **THEN** the method SHALL NOT call `GetRequiredService<IActorRegistry>()`, `GetRequiredService<IOptions<FunkArrOptions>>()`, or `registry.Get<T>()`

#### Scenario: Health check responds when healthy
- **WHEN** `GET /healthz` is requested and all health checks pass
- **THEN** the response status is 200

#### Scenario: Setup API is mapped
- **WHEN** `GET /api/health/setup` is requested
- **THEN** the setup health check endpoint responds (not 404)

#### Scenario: Static files and SPA fallback are mapped
- **WHEN** reviewing `ApplicationSetupContainer.SetupApplication`
- **THEN** `UseStaticFiles()` is called before endpoint mapping, `MapRuleSetApi()` and `MapSetupApi()` are called, and a SPA fallback route is registered last

#### Scenario: SPA fallback does not intercept API routes
- **WHEN** `GET /api/rulesets` is requested
- **THEN** the API endpoint responds, not the SPA fallback

### Requirement: Configuration files
The host SHALL load configuration from:
1. `appsettings.json` (required, production defaults)
2. `appsettings.Development.json` (optional, dev overrides)
3. Environment variables

`appsettings.json` SHALL contain sensible defaults including the `FunkArr` section
with `ApiKey` and `DataPath`, and Serilog minimum level set to `Information`
with `Microsoft.AspNetCore` override to `Warning`.

#### Scenario: Development overrides apply
- **WHEN** running in Development environment
- **THEN** `appsettings.Development.json` values override `appsettings.json`

#### Scenario: Environment variables override all files
- **WHEN** `FunkArr__ApiKey` is set as an environment variable
- **THEN** it takes precedence over the value in appsettings.json
