## Purpose

Dedicated Servus setup container that configures Serilog as the application's logging pipeline, ensuring logging is available before any other setup container runs.

## Requirements

### Requirement: LoggingSetupContainer as IServiceSetupContainer
`LoggingSetupContainer` SHALL implement `IServiceSetupContainer` and configure Serilog
via `services.AddSerilog(...)`. It SHALL configure:
- `ReadFrom.Configuration(configuration)` for level overrides from appsettings
- `Enrich.WithMachineName()`, `Enrich.WithThreadId()`, `Enrich.FromLogContext()`
- `Enrich.WithProperty("ApplicationVersion", ...)` using the entry assembly version
- `WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")`

#### Scenario: Serilog configured via container
- **WHEN** the host boots with LoggingSetupContainer in the AppBuilder chain
- **THEN** log output uses the structured template with all enrichment properties

#### Scenario: Container reads log levels from configuration
- **WHEN** `appsettings.json` sets `Serilog:MinimumLevel:Default` to `Warning`
- **THEN** only Warning-level and above messages appear in console output

### Requirement: LoggingSetupContainer is first in AppBuilder chain
`LoggingSetupContainer` SHALL be the first `.WithSetup<T>()` call in the AppBuilder
chain, before `FunkArrServiceSetup`, `FunkArrActorSystemSetup`, and
`FunkArrApplicationSetup`.

#### Scenario: Logging available to subsequent containers
- **WHEN** `FunkArrServiceSetup.SetupServices` runs
- **THEN** Serilog is already configured and log calls from service setup appear in output
