## Why

The FunkArr UI has no visibility into the download pipeline. Users cannot see what's queued, what's downloading, or what completed/failed — they have to check Sonarr/Radarr or guess. Adding Queue and History pages gives direct insight into the download lifecycle, and a dashboard widget surfaces active downloads at a glance.

## What Changes

- **New internal download API** (`/api/downloads/...`) — clean REST endpoints for the UI, separate from the SABnzbd adapter in ArrApi. Includes an SSE stream for real-time queue updates.
- **Queue page** (`/queue`) — live view of queued and active downloads with progress bars, speed, ETA. Card layout. Cancel/delete actions. Powered by SSE.
- **History page** (`/history`) — table of completed/failed downloads with pagination, retry for failed items, delete action.
- **Dashboard widget** — compact active downloads summary on the home page, sharing the same SSE composable.
- **Sidebar navigation** — two new nav items (Queue, History) between Dashboard and RuleSets.

## Capabilities

### New Capabilities

- `download-api-internal`: Internal REST + SSE endpoints for the UI to query queue state, history, and perform actions (delete, retry). Lives in FunkArr.Api, talks to DownloadManager and DownloadHistoryActor via existing messages.
- `download-queue-ui`: Queue page with card layout, progress visualization, SSE-powered live updates, cancel/delete actions, and a global `useQueueStream` composable.
- `download-history-ui`: History page with table layout, pagination, category filter, retry/delete actions, status indicators.

### Modified Capabilities

- `sidebar-layout`: Add Queue and History navigation items between Dashboard and RuleSets.

## Impact

- **FunkArr.Api** — new `DownloadApiEndpoints.cs` + response models. New dependency on `IDownloadManager` and `IDownloadHistory` actor refs (already registered in Core).
- **FunkArr.UI** — new views, components, composable, API client module, router entries.
- **No backend actor changes** — all messages (`QueryQueue`, `QueryHistory`, `DeleteDownload`, `RetryDownload`, `RemoveHistoryEntry`) already exist.
- **No breaking changes** — additive only.
