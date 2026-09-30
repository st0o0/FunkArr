## MODIFIED Requirements

### Requirement: Serilog console logging with fixed template
The application SHALL use Serilog with a console sink and a single plain-text output
template. Serilog SHALL be configured in `LoggingSetupContainer` (an `IServiceSetupContainer`)
via `services.AddSerilog(...)`. Default log providers SHALL be cleared with
`builder.Logging.ClearProviders()` in Program.cs before the AppBuilder chain.

Non-actor code (API endpoints, HTTP clients, services) SHALL use `ILogger<T>` from `Microsoft.Extensions.Logging` for logging. Actor code SHALL continue using Akka's `ILoggingAdapter`. Both flow through the Serilog pipeline.

#### Scenario: Plain text log output

- **WHEN** the application starts
- **THEN** console log output SHALL use the human-readable template `[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}`

#### Scenario: API endpoint logging uses ILogger

- **WHEN** an API endpoint logs a message
- **THEN** it SHALL use `ILogger<T>` resolved via DI, and the log entry SHALL appear in the Serilog console output with SourceContext set to the endpoint class name
