## Why

The solution skeleton exists (19 projects, build infra, NuGet pinning) but Program.cs
is a bare 3-line scaffold. No code can be written until the host boots with Serilog,
Akka.NET, Servus AppBuilder, and configuration binding in place. This is the foundation
every domain slice depends on.

## What Changes

- Wire up Program.cs with Serilog structured logging (console sink, enrichers)
- Add Servus AppBuilder startup with three setup containers (Service, ActorSystem, Application)
- Define FunkArrOptions and bind to `FunkArr` config section with validation
- Configure Akka.NET hosting with SQLite persistence and Serilog logger
- Add appsettings.json / appsettings.Development.json with sensible defaults
- Register a health check endpoint (`/healthz`) and liveness probe (`/alive`)

## Capabilities

### New Capabilities

- `application-bootstrap`: Host startup wiring — Serilog, Akka.NET, Servus AppBuilder, config binding, health endpoints

### Modified Capabilities

- `options-structure`: Adding the initial FunkArrOptions class and registration (subset — only the cross-cutting options, not domain-scoped ones yet)
- `structured-logging`: Implementing the Serilog setup specified in this spec

## Impact

- `src/FunkArr/Program.cs` — rewritten with full startup
- `src/FunkArr/Configuration/` — new setup containers and options classes
- `src/FunkArr/appsettings.json`, `appsettings.Development.json` — new config files
- After this change, `dotnet run` from `src/FunkArr/` boots an Akka actor system with logging
