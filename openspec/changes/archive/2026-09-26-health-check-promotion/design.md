## Approach

Extract check logic into `IHealthCheck` implementations in `FunkArr.Api`. Register them in `ServiceSetupContainer`. Refactor `SystemApiEndpoints` to reuse health check results for shared checks.

## Health Check Classes

All in `FunkArr.Api/HealthChecks/`:

- **`FfmpegHealthCheck`** -- runs `ffmpeg -version`, returns Unhealthy if not found or exit code != 0. Reuses the existing `CheckFfmpeg()` logic.
- **`MediathekViewWebHealthCheck`** -- HEAD request to mediathekviewweb.de with 3s timeout, returns Degraded on failure (not Unhealthy -- downloads still work).
- **`DirectoryHealthCheck`** -- parameterized check, instantiated 3x (data, complete, incomplete). Takes a `Func<DataPaths, string>` to resolve the path. Returns Unhealthy if not writable. Uses `IDataFiles.CanWrite()`.

## Registration

In `ServiceSetupContainer`:

```
services.AddHealthChecks()
    .AddCheck<FfmpegHealthCheck>("ffmpeg", failureStatus: HealthStatus.Unhealthy)
    .AddCheck<MediathekViewWebHealthCheck>("mediathekviewweb", failureStatus: HealthStatus.Degraded)
    .AddCheck("data-directory", new DirectoryHealthCheck(...), failureStatus: HealthStatus.Unhealthy)
    .AddCheck("complete-directory", new DirectoryHealthCheck(...), failureStatus: HealthStatus.Unhealthy)
    .AddCheck("incomplete-directory", new DirectoryHealthCheck(...), failureStatus: HealthStatus.Unhealthy);
```

DirectoryHealthCheck needs `DataPaths` and `IDataFiles` from DI. Since we need 3 instances with different path selectors, register them as factory-constructed instances rather than generic `AddCheck<T>`.

## Setup Endpoint Refactoring

`SystemApiEndpoints.MapSystemApi` keeps its existing shape but delegates to a shared method for the overlapping checks:
- FFmpeg, MVW, directories: call the same logic the health checks use
- API key, self-test indexer, self-test download: remain as-is (setup-only)

The simplest approach: extract the check logic into static methods on a shared class (or keep them on the health check classes as `internal static`), and have both the `IHealthCheck.CheckHealthAsync` and `SystemApiEndpoints` call them. This avoids running the ASP.NET health check subsystem from within the setup endpoint.

## `/healthz` Response

Keep the existing `HealthCheckOptions` in `ApplicationSetupContainer` (200 for Healthy/Degraded, 503 for Unhealthy). No custom response writer needed -- the default response body is sufficient for container probes.

## Testing

Unit tests for each health check:
- `FfmpegHealthCheck`: not easily unit-testable (process execution), skip or integration-test only
- `MediathekViewWebHealthCheck`: inject a mock `IHttpClientFactory` with `MockHttpMessageHandler`
- `DirectoryHealthCheck`: inject mock `IDataFiles` with `CanWrite()` returning true/false
