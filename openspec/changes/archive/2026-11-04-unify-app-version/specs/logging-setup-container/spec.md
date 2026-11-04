## MODIFIED Requirements

### Requirement: LoggingSetupContainer as ISetup<IServiceCollection>
`LoggingSetupContainer` SHALL implement `ISetup<IServiceCollection>` and configure Serilog
via `services.AddSerilog(...)`. It SHALL configure:
- `ReadFrom.Configuration(configuration)` for level overrides from appsettings
- `Enrich.WithMachineName()`, `Enrich.WithThreadId()`, `Enrich.FromLogContext()`
- `Enrich.WithProperty("ApplicationVersion", ...)` using the shared application version const from `FunkArr.Core`
- `WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")`

#### Scenario: Serilog configured via container
- **WHEN** the host boots with LoggingSetupContainer in the builder.Setups() chain
- **THEN** log output uses the structured template with all enrichment properties

#### Scenario: Container reads log levels from configuration
- **WHEN** `appsettings.json` sets `Serilog:MinimumLevel:Default` to `Warning`
- **THEN** only Warning-level and above messages appear in console output
