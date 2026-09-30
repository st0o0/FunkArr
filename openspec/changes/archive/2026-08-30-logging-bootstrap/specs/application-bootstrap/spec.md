## MODIFIED Requirements

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
