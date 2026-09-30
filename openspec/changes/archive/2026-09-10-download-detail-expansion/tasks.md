## 1. Messages - Extend WorkerStatusResult and QueueItem

- [x] 1.1 Add `Channel` (string) and `HasSubtitles` (bool) parameters to `WorkerStatusResult` in `FunkArr.Messages/Download/WorkerStatusResult.cs`
- [x] 1.2 Add `Channel` (string) and `HasSubtitles` (bool) parameters to `QueueItem` in `FunkArr.Messages/Download/QueueResult.cs`

## 2. Download domain - Wire new fields

- [x] 2.1 Update `DownloadWorker` to pass `_state.Channel` and `_state.SubtitleUrl is not null` when constructing `WorkerStatusResult`
- [x] 2.2 Update `DownloadManager` QueryQueue handler to pass `Channel` and `HasSubtitles` from `WorkerStatusResult` when constructing `QueueItem`

## 3. API model and mapping

- [x] 3.1 Add `Channel` (string), `HasSubtitles` (bool), and `TotalDuration` (int) to `DownloadQueueItem` in `FunkArr.Api/Models/DownloadQueue.cs`
- [x] 3.2 Update `ToQueueItem` in `QueueApiEndpoints` to map `Channel`, `HasSubtitles`, and `TotalDuration` from `QueueItem` to the API model

## 4. Frontend - Queue card display

- [x] 4.1 Update `QueueCard.vue` to display channel name, subtitle badge (when `hasSubtitles` is true), and formatted video duration
- [x] 4.2 Add duration formatting helper to `utils/format.ts` (seconds to "Xh Ym" or "X min")
- [x] 4.3 Update `QueueGroupCard.vue` if it renders any per-item detail that should include the new fields

## 5. Tests

- [x] 5.1 Update existing `QueueApiEndpoints` tests to verify new fields in the mapped response
