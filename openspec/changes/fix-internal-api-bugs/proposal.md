## Why

Three bugs in the internal `/api` surface can be fixed without touching the HTTP contract, ahead of the larger `rewrite-internal-api` restructuring: actor Ask timeouts surface as 500 instead of 504, the live log stream loses entries as soon as two browser tabs are open, and retrying a download deletes its history entry before the retry is known to succeed.

## What Changes

- `EndpointExceptionFilter` maps `Akka.Actor.AskTimeoutException` (which derives from `AkkaException`, not `TimeoutException`) to 504. Default `_ =>` arms in endpoint response switches that currently return `GatewayTimeout()` for unknown response types return 500 and log instead.
- `RingBufferSink` replaces its single shared `Channel` with one bounded channel per stream subscriber, so every connected client receives every entry and nothing is buffered while no client is connected.
- Retry: `POST /api/downloads/{id}/retry` no longer tells the DownloadHistoryManager anything. `DownloadManager` sends `RemoveHistoryEntry` itself, inside the `DownloadEnqueued` Persist callback, only when the retry is accepted.

No route, request or response body changes.

## Capabilities

### New Capabilities
- `api-ask-error-mapping`: How actor Ask outcomes map to HTTP status codes in `/api` (timeout → 504, unexpected response → 500).
- `log-stream`: Recent-log snapshot and multi-subscriber live log stream.

### Modified Capabilities
- `download-api-internal`: Retry endpoint no longer contacts the history manager.
- `download-retry`: DownloadManager removes the history entry after an accepted manual retry.

## Impact

- `src/FunkArr.Api/EndpointExceptionFilter.cs`, `ApiResults.cs`, `*ApiEndpoints.cs` (default switch arms), `RingBufferSink.cs`, `SystemApiEndpoints.cs` (log stream), `DownloadsApiEndpoints.cs` (retry)
- `src/FunkArr.Download/DownloadManager.cs` (retry → history)
- Tests: `FunkArr.Api.Tests`, `FunkArr.Download.Tests`, `FunkArr.IntegrationTests` (retry)
- UI: none
