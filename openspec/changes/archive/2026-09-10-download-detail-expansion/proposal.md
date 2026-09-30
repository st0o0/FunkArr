## Why

The DownloadWorker holds rich per-download metadata (channel name, subtitle URL, video duration) in its state, but `WorkerStatusResult` and the downstream `QueueItem`/`DownloadQueueItem` strip these fields. The UI queue cards and download-api-internal spec already mention channel display, but the API model never delivers it. Surfacing this data gives users immediate visibility into what's being downloaded without parsing the release title.

## What Changes

- Add `Channel` (string) and `HasSubtitles` (bool) to `WorkerStatusResult` message
- Add `Channel` (string), `HasSubtitles` (bool), and `TotalDuration` (int, seconds) to `QueueItem` message
- Add `channel` (string), `hasSubtitles` (bool), and `totalDuration` (int, seconds) to `DownloadQueueItem` API model
- Update `DownloadWorker` to include channel and subtitle info in status response
- Update `DownloadManager` to pass new fields when constructing QueueItems
- Update `QueueApiEndpoints` to map the new fields to the API model
- Update queue card UI components to display channel, subtitle badge, and formatted duration

## Capabilities

### New Capabilities

None.

### Modified Capabilities
- `download-messages`: Add `Channel` and `HasSubtitles` to `WorkerStatusResult`; add `Channel`, `HasSubtitles`, `TotalDuration` to `QueueItem`
- `download-api-internal`: Add `channel`, `hasSubtitles`, `totalDuration` fields to `DownloadQueueItem` API response model
- `download-queue-ui`: Display channel, subtitle indicator, and video duration on queue cards

## Impact

- **Messages**: `WorkerStatusResult` and `QueueItem` records gain new fields
- **Download domain**: `DownloadWorker` and `DownloadManager` updated to pass through new fields
- **Api**: `DownloadQueueItem` model extended, `ToQueueItem` mapping updated
- **UI**: `QueueCard.vue` and `QueueGroupCard.vue` display new metadata
- **No breaking changes** - all additions to existing records
