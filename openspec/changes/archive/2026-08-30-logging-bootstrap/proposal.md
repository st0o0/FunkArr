## Why

Program.cs currently configures Serilog inline with no crash safety. If anything
fails before or during startup (bad config, missing dependency, options validation),
the process exits silently — no log output. In a Docker container this means an
unexplained exit code with no diagnostics.

## What Changes

- Add a Serilog bootstrap logger in Program.cs that catches startup failures
- Wrap the entire startup in try/catch/finally with `Log.Fatal` and `Log.CloseAndFlushAsync`
- Extract the full Serilog configuration (enrichers, console sink, template) from
  Program.cs into a new `LoggingSetupContainer : IServiceSetupContainer`
- Insert `LoggingSetupContainer` as the first container in the AppBuilder chain
- Program.cs retains only the bootstrap logger, Kestrel config, and the AppBuilder chain

## Capabilities

### New Capabilities

- `logging-setup-container`: Dedicated Servus setup container for Serilog configuration

### Modified Capabilities

- `structured-logging`: Serilog moves from inline Program.cs to LoggingSetupContainer; bootstrap logger added
- `application-bootstrap`: AppBuilder chain gains LoggingSetupContainer as first entry; Program.cs wrapped in try/catch/finally

## Impact

- `src/FunkArr/Program.cs` — rewritten: bootstrap logger + try/catch/finally + slimmed AppBuilder chain
- `src/FunkArr/Configuration/LoggingSetupContainer.cs` — new file
- No new NuGet dependencies (Serilog packages already in host project)
