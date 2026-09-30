## Context

FunkArr's Program.cs currently calls `builder.Services.AddSerilog(...)` inline before
the AppBuilder chain. This works but offers no crash safety — if startup fails before
Serilog is wired up or if the AppBuilder chain throws, the process dies silently.

Akka.Pathfinder solves this with a two-phase approach: a bootstrap logger in Program.cs
catches early failures, and the real Serilog setup lives in a dedicated setup container.

## Goals / Non-Goals

**Goals:**
- Startup failures always produce log output (bootstrap logger)
- Serilog configuration lives in a focused setup container, not inline in Program.cs
- Program.cs is minimal: bootstrap logger, Kestrel, AppBuilder chain, crash handler

**Non-Goals:**
- No OpenTelemetry integration (separate change)
- No changes to Akka logging config (stays in FunkArrActorSystemSetup)
- No `IHostBuilderSetupContainer` — we use `IServiceSetupContainer` with `AddSerilog()`

## Decisions

### LoggingSetupContainer implements IServiceSetupContainer

The container uses `services.AddSerilog(...)` (the modern .NET 8+ pattern) rather than
`IHostBuilderSetupContainer` with `UseSerilog()` (the older pattern used in Pathfinder).
`AddSerilog()` replaces the bootstrap logger automatically.

**Why:** Consistent with what we already have. `AddSerilog()` is the recommended
approach for new .NET projects and doesn't require access to `IHostBuilder`.

### LoggingSetupContainer lives in FunkArr/Configuration/

Not in FunkArr.Core, because the Serilog sink/enricher packages live in the host
project. Moving them to Core would widen Core's dependency surface for no benefit
while there's only one host.

### Bootstrap logger uses Console sink only, Debug level

Mirrors Akka.Pathfinder's pattern. The bootstrap logger is intentionally minimal —
it only needs to catch crashes before the real logger takes over. No enrichers,
no template customization.

### ClearProviders stays in Program.cs

`builder.Logging.ClearProviders()` must run before the AppBuilder chain, so it
stays in Program.cs alongside the bootstrap logger. The LoggingSetupContainer
only handles `AddSerilog()`.

## Risks / Trade-offs

- **Two Serilog configurations to maintain** — The bootstrap logger is intentionally
  minimal (3 lines) so drift is not a real concern. The real config is in one place
  (LoggingSetupContainer).
