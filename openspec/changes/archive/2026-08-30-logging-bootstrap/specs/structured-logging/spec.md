## MODIFIED Requirements

### Requirement: Serilog console logging with fixed template
The application SHALL use Serilog with a console sink and a single plain-text output
template. Serilog SHALL be configured in `LoggingSetupContainer` (an `IServiceSetupContainer`)
via `services.AddSerilog(...)`. Default log providers SHALL be cleared with
`builder.Logging.ClearProviders()` in Program.cs before the AppBuilder chain.

#### Scenario: Plain text log output
- **WHEN** the application starts
- **THEN** console log output SHALL use the human-readable template `[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}`

## ADDED Requirements

### Requirement: Bootstrap logger for crash safety
Program.cs SHALL create a Serilog bootstrap logger before the AppBuilder chain:
`Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateBootstrapLogger()`.
The entire startup SHALL be wrapped in try/catch/finally:
- `catch`: `Log.Fatal(ex, "Application terminated unexpectedly")`
- `finally`: `await Log.CloseAndFlushAsync()`

#### Scenario: Startup failure is logged
- **WHEN** the AppBuilder chain throws an exception during startup
- **THEN** the exception is logged to console via `Log.Fatal` before the process exits

#### Scenario: Bootstrap logger replaced by real logger
- **WHEN** `LoggingSetupContainer` runs `AddSerilog()`
- **THEN** the bootstrap logger is replaced by the fully configured Serilog pipeline
