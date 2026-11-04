# API Ask Timeout Cleanup

## Problem

Every Ask call in the API layer (FunkArr.Api and FunkArr.ArrApi) carries an
explicit timeout (`TimeSpan.FromSeconds(10)` etc.) alongside a
`CancellationToken`. The CT already cancels the Ask when the HTTP request
ends — via client disconnect or `WithRequestTimeout`. The explicit Ask timeout
is redundant and adds boilerplate (static fields, timeout calibration per
endpoint).

## Change

Remove explicit Ask timeouts from the API and ArrApi layers. Rely on
`CancellationToken` (bound to `HttpContext.RequestAborted`) as the sole
cancellation mechanism. `WithRequestTimeout` on each endpoint group remains
as the safety net.

### FunkArr.Api (5 endpoint files, ~30 Ask calls)

- Remove `_askTimeout` / `_queryTimeout` / `_statsTimeout` / `_testTimeout`
  static fields
- Change `Ask<T>(msg, timeout, ct)` → `Ask<T>(msg, ct)`
- Keep `WithRequestTimeout(...)` values unchanged
- `EndpointExceptionFilter`: remove the `AskTimeoutException` catch arm (dead
  code — Ask without timeout never throws it). Keep the `TimeoutException`
  catch for other potential sources (HttpClient etc.).

### FunkArr.ArrApi (3 service files, ~14 Ask calls)

- Remove `Timeout` properties from `SabnzbdQueueService` and
  `SabnzbdDownloadService`
- Remove `_feedTimeout` field and `timeout` local from `NewznabSearchService`
- Change `Ask<T>(msg, timeout, ct)` → `Ask<T>(msg, ct)`
- Change `catch (TimeoutException)` → `catch (OperationCanceledException)`
  (CT cancellation throws this instead of `AskTimeoutException`)
- Remove `DownloadTimeoutSeconds` and `SearchTimeoutSeconds` from
  `ArrApiOptions` (dead config)

## Out of scope

- Domain actor Ask timeouts (PipeTo chains without CT — timeouts are
  essential there)
- `WithRequestTimeout` values (these stay as the HTTP-level safety net)
- Global HOCON `ask-timeout` configuration
