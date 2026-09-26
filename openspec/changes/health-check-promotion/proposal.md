## Why

`/healthz` is registered with `AddHealthChecks()` but has zero `IHealthCheck` implementations -- it always returns 200 Healthy. Container orchestration (Docker health probes, Kubernetes liveness/readiness) gets no useful signal. Meanwhile, `/api/system/setup` runs 8 real checks (FFmpeg, MVW reachability, directories, self-tests, API key) but is only an API endpoint the UI consumes.

## What Changes

Extract the infrastructure checks from `SystemApiEndpoints` into proper `IHealthCheck` implementations and register them on `/healthz`. Keep the setup-specific checks (API key default warning, self-test endpoints) on `/api/system/setup` only.

### Health checks for `/healthz`

| Check | Severity | Rationale |
|-------|----------|-----------|
| FFmpeg available | Unhealthy | No downloads possible without it |
| Data directory writable | Unhealthy | No persistence possible |
| Complete directory writable | Unhealthy | No finished downloads possible |
| Incomplete directory writable | Unhealthy | No active downloads possible |
| MediathekViewWeb reachable | Degraded | Running downloads still work, only new searches fail |

### Checks that stay on `/api/system/setup` only

| Check | Reason |
|-------|--------|
| API key default warning | Not a health issue, just a security hint |
| Self-test indexer API | Circular (calls own endpoint), deadlock risk at startup |
| Self-test download API | Same reason |

### Shared logic

The check logic (HTTP HEAD to MVW, `ffmpeg -version`, directory write test) moves into reusable services or the `IHealthCheck` implementations themselves. `/api/system/setup` reuses them to avoid duplicating the logic -- it wraps the `IHealthCheck` results into its existing `CheckResult` model alongside the setup-only checks.

## Capabilities

### New Capabilities

- `health-checks`: Five `IHealthCheck` implementations registered on `/healthz` with appropriate `HealthStatus` (Unhealthy or Degraded)

### Modified Capabilities

- `setup-endpoint`: `/api/system/setup` reuses health check results instead of its own static methods for the shared checks (FFmpeg, MVW, directories). Self-test and API key checks remain setup-only.

## Impact

- **FunkArr.Core** or **FunkArr.Api**: New `IHealthCheck` classes (FfmpegHealthCheck, MediathekViewWebHealthCheck, DirectoryHealthCheck)
- **FunkArr (Host)**: `ServiceSetupContainer` registers the health checks with tags/severity
- **FunkArr.Api**: `SystemApiEndpoints` delegates to health check results for shared checks, keeps self-test and API key checks
- No new projects, no new dependencies
