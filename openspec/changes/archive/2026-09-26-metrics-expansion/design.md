## Context

FunkArr uses `System.Diagnostics.Metrics` with OpenTelemetry Prometheus export. Each domain has a static `Telemetry` class with a `Meter` and instruments. The host's `TelemetrySetupContainer` registers all meters via `.AddMeter()`. This pattern is consistent across existing domains but coverage is incomplete.

## Goals / Non-Goals

**Goals:**
- Every domain has a `Telemetry.cs` with a named `Meter`
- Every error/failure path has a counter
- Key operations have duration histograms
- Stateful actors expose state gauges where operationally relevant
- Metrics naming follows `funkarr.<domain>.<metric>` convention consistently
- Document the conventions in AGENTS.md

**Non-Goals:**
- Distributed tracing (ActivitySource) - out of scope, project uses metrics only
- Custom Prometheus labels beyond what's operationally needed
- Dashboards or alerting rules
- Changing the existing metrics pattern (static class, static readonly instruments)

## Decisions

1. **Follow existing pattern exactly** - static `Telemetry` class, `internal static readonly` instruments, one `Meter` per domain. No base class, no abstraction.

2. **Naming convention**: `funkarr.<domain>.<noun>_<unit>` for histograms/gauges, `funkarr.<domain>.<noun>_total` for counters. Tags use short lowercase names (`reason`, `source`, `type`, `api`, `phase`, `status`).

3. **ObservableGauge for actor state** - Gauges like `paused`, `schedule_enabled`, `active` use callbacks that read from a static field set by the actor. Same pattern as existing `queue_size` and `active` gauges in Download.

4. **Duration measurement** - Use `System.Diagnostics.Stopwatch` or `TimeProvider.GetTimestamp()`/`GetElapsedTime()` for timing, record to Histogram in seconds. Consistent with existing `duration_seconds` in FfmpegRunner.

5. **Tag cardinality** - Keep tag values bounded. `ruleSetId` on scoring counters is acceptable since ruleset count is small and user-controlled. Never use unbounded values like download IDs or search queries as tags.

6. **Instrument at the operation boundary** - Prefer recording metrics at the point where the operation completes (success or failure), not at the call site. For actor messages, this means in the handler method.

## Risks / Trade-offs

- **Tag cardinality on ruleSetId**: If users create many rulesets, this grows the time series. Acceptable for v0.x; revisit if ruleset count grows past ~50.
- **ObservableGauge statics**: The static callback fields couple the Telemetry class to the actor's lifetime. Same trade-off as existing gauges - acceptable given single-instance singletons.
- **Volume**: Adding ~32 new instruments is a large diff but mechanically simple. Each instrument follows the same pattern.
