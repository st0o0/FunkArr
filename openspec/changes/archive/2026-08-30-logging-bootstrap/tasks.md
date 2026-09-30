## 1. LoggingSetupContainer

- [x] 1.1 Create `LoggingSetupContainer : IServiceSetupContainer` in `src/FunkArr/Configuration/` — moves the full Serilog config (ReadFrom.Configuration, enrichers, console sink with template) from Program.cs into `SetupServices`

## 2. Program.cs

- [x] 2.1 Rewrite Program.cs — add bootstrap logger, wrap startup in try/catch/finally, remove inline AddSerilog, insert LoggingSetupContainer as first in AppBuilder chain, keep ClearProviders before chain
- [x] 2.2 Build, format, and verify: `dotnet build` succeeds, `dotnet format --verify-no-changes` passes, `dotnet run` boots with Serilog output, `/alive` returns 200
