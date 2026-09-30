## Why

HTTP endpoints currently ignore client disconnections. When a user navigates away or a reverse proxy times out, the server keeps processing the full actor Ask call until its timeout elapses. ASP.NET provides `HttpContext.RequestAborted` as a `CancellationToken` that signals on disconnect, and Akka.NET's `Ask<T>` has a 3-arg overload accepting both timeout and CancellationToken. Threading this through lets the server abandon work immediately when the client is gone.

Only 1 of ~44 Ask calls in the API layer currently passes a CancellationToken (the SSE streaming endpoint). The rest use timeout-only.

## What Changes

- All Minimal API endpoint lambdas in FunkArr.Api accept `CancellationToken ct` as a parameter (auto-injected by the framework from `HttpContext.RequestAborted`)
- All controller action methods in FunkArr.ArrApi accept `CancellationToken cancellationToken` as a parameter
- ArrApi service classes (`NewznabSearchService`, `SabnzbdDownloadService`, `SabnzbdQueueService`, `NzbService`) accept and forward `CancellationToken` on their public methods
- All `Ask<T>(message, timeout)` calls in the API layer become `Ask<T>(message, timeout, ct)`
- Actor-to-actor Ask calls (internal, not tied to HTTP) remain timeout-only

## Capabilities

### New Capabilities

- `cancellation-token-flow`: Thread CancellationToken from HTTP request through API layer to actor Ask calls

### Modified Capabilities

None.

## Impact

- FunkArr.Api: ~30 endpoint lambdas across 4 endpoint classes (DownloadsApiEndpoints, RuleSetApiEndpoints, MediathekApiEndpoints, SystemApiEndpoints)
- FunkArr.ArrApi: 3 controllers + 4 service classes
- FunkArr.ArrApi.Tests: service test methods need CT parameter added
- No actor code changes
- No message/persistence changes
