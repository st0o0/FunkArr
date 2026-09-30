## Context

The solution has 19 projects with correct references and NuGet pinning, but Program.cs
is a bare 3-line scaffold. No domain work can start until the host boots with logging,
actor system, configuration, and HTTP pipeline. The reference project njord follows the
same Servus AppBuilder pattern and serves as the structural template.

## Goals / Non-Goals

**Goals:**
- Bootable host with Serilog, Akka.NET actor system, and health endpoints
- Servus AppBuilder with 3 setup containers matching njord's pattern
- FunkArrOptions bound and validated on startup
- appsettings.json with sensible defaults for local development
- Clean foundation that domain slices can build on without touching Program.cs

**Non-Goals:**
- No actors registered yet (no domain logic)
- No API endpoints beyond health/liveness
- No domain-scoped options (DownloadOptions, SearchOptions, etc.) — those arrive with their domain
- No persistence configuration yet — no actors need it
- No Docker/CI changes

## Decisions

### Servus AppBuilder with 3 setup containers

Follow njord's proven pattern:
- `FunkArrServiceSetup : IServiceSetupContainer` — DI registrations, options binding
- `FunkArrActorSystemSetup : ActorSystemSetupContainer` — actor system name, loggers, persistence (when needed)
- `FunkArrApplicationSetup : ApplicationSetupContainer<WebApplication>` — HTTP pipeline, endpoints

**Why:** Separates concerns cleanly. Each domain slice adds to the relevant container
without modifying Program.cs. The pattern is battle-tested in njord.

**Alternative:** Inline everything in Program.cs — rejected because it doesn't scale
as domains are added.

### Serilog configured in Program.cs, not in setup containers

Serilog must be configured before `AppBuilder.Create()` so that startup failures
are logged. This matches njord: `builder.Services.AddSerilog(...)` and
`builder.Logging.ClearProviders()` happen in Program.cs before the AppBuilder chain.

### FunkArrOptions as the first options class

Only the cross-cutting `FunkArrOptions` is introduced now (ApiKey, PersistencePath).
Domain-scoped options arrive with their domain slice. The options class lives in
`FunkArr/Configuration/` (host project), not in Core, because it binds to the
root config section and is host-specific.

### Actor system boots without persistence

The initial ActorSystemSetup configures loggers and the system name but does NOT
wire up `WithSqlPersistence`. Persistence config arrives with the first actor that
needs it (likely the download queue). This avoids creating an empty SQLite database
on first boot when nothing uses it.

### Kestrel defaults to port 8080

Single HTTP port, no gRPC (unlike njord). FunkArr only serves REST/JSON and XML.
Port configurable via `ASPNETCORE_URLS` or `FunkArr:Http:Port`.

## Risks / Trade-offs

- **Actor system without persistence** — First domain slice that needs persistence
  will have to add `WithSqlPersistence` to the ActorSystemSetup. This is intentional:
  keep bootstrap minimal.
- **No architecture tests yet** — ArchUnitNET tests arrive with the first domain slice
  that introduces types to enforce boundaries on. An empty host has nothing to test.
