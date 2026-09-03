## Why

The DownloadManager currently computes `IncompletePath` and `OutputPath` from `DownloadOptions`, then passes them through `InitDownload` messages, `DownloadInitialized` persistence events, and `DownloadWorkerState`. This couples infrastructure paths to domain messages, makes persisted events fragile when config changes, and forces the Manager to own path-resolution logic that belongs to the Worker.

## What Changes

- Remove `IncompletePath` and `OutputPath` from the `InitDownload` message
- Remove `IncompletePath` and `OutputPath` from the `DownloadInitialized` persistence event — **BREAKING**
- Remove `IncompletePath` and `OutputPath` from `DownloadWorkerState`
- Inject `DownloadOptions` into `DownloadWorker` via DI
- Worker computes paths at runtime (in `HandleStart`) from its persisted metadata + current config
- Manager no longer resolves paths — only forwards domain metadata
- `HandleReset` resets status without re-persisting paths

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `download-messages`: Remove `IncompletePath` and `OutputPath` from `InitDownload`; remove them from `DownloadInitialized` persistence DTO
- `download-worker`: Worker computes paths at runtime via injected `DownloadOptions`; state no longer holds paths; reset no longer re-persists path metadata
- `download-manager`: Manager no longer computes or forwards paths in `InitDownload`

## Impact

- **Persistence**: Breaking change to `DownloadInitialized` event shape. Acceptable at v0.x — no migration needed.
- **Code**: `FunkArr.Messages`, `FunkArr.Persistence`, `FunkArr.Download` (Worker, Manager, State), tests.
- **APIs**: No external API changes.
