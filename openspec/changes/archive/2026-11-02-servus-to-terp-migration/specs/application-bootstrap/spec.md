## MODIFIED Requirements

### Requirement: Terp ISetup startup
The host SHALL use `builder.Setups().Add<T>()` with setup containers in this order:
1. `LoggingSetupContainer`
2. `TelemetrySetupContainer`
3. `CoreSetupContainer`
4. `AkkaSetupContainer`
5. `SearchSetupContainer`
6. `DownloadSetupContainer`
7. `ScoringSetupContainer`
8. `RuleSetSetupContainer`
9. `EnrichmentSetupContainer`
10. `ArrApiSetupContainer`

After `builder.Build()`, the host SHALL use `app.Setups().Add<T>()` for
containers that implement `ISetup<WebApplication>` in this order:
1. `SearchSetupContainer`
2. `DownloadSetupContainer`
3. `RuleSetSetupContainer`
4. `ApplicationSetupContainer`

`ApplicationSetupContainer` SHALL be last in `app.Setups()` because it registers
the catch-all `/api/{**rest}` route and `MapFallbackToFile`.

The `builder.Setups().Add<T>()` chain and `await app.RunAsync()` SHALL be wrapped
in a try/catch/finally block. The catch block SHALL call `Log.Fatal(ex, ...)` and
the finally block SHALL call `await Log.CloseAndFlushAsync()`.

#### Scenario: Host boots with Terp setup chain
- **WHEN** the host starts via docker compose
- **THEN** the application starts without errors using builder.Setups().Add<T>() and app.Setups().Add<T>()

#### Scenario: Setup containers are invoked in order
- **WHEN** the host boots
- **THEN** builder setups execute in the order listed above, with infrastructure before domains
- **AND** app setups execute with ApplicationSetupContainer last

#### Scenario: Unknown API path returns 404
- **WHEN** a request is made to `/api/nonexistent`
- **THEN** the response is 404 JSON, not the SPA index.html

### Requirement: Serilog structured logging
The host SHALL create a bootstrap logger before the Setups chain via
`Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateBootstrapLogger()`.
The full Serilog configuration SHALL be handled by `LoggingSetupContainer`, not
inline in Program.cs. `builder.Logging.ClearProviders()` SHALL be called in
Program.cs before the `builder.Setups()` chain.

#### Scenario: Console log output uses structured template
- **WHEN** the application starts
- **THEN** log output follows the format `[HH:mm:ss INF] [SourceContext] Message`

#### Scenario: Akka.NET logs flow through Serilog
- **WHEN** the Akka actor system logs a lifecycle event
- **THEN** the message appears in Serilog console output with the same template
