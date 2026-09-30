## 1. Internal Download API

- [x] 1.1 Create API response models (`DownloadQueue.cs`, `DownloadHistory.cs`) in `FunkArr.Api/Models/`
- [x] 1.2 Create `DownloadApiEndpoints.cs` in `FunkArr.Api/` with REST endpoints (queue snapshot, history with pagination, delete queue, delete history, retry)
- [x] 1.3 Add SSE stream endpoint (`GET /api/downloads/queue/stream`) with 3-second polling loop
- [x] 1.4 Register `MapDownloadInternalApi()` in `ApplicationSetupContainer`
- [x] 1.5 Add API endpoint tests in `FunkArr.Api.Tests/`

## 2. Frontend API Client & SSE Composable

- [x] 2.1 Create `src/api/downloads.ts` with fetch functions for history, delete, retry
- [x] 2.2 Create `src/composables/useQueueStream.ts` — global SSE composable with shared reactive state
- [x] 2.3 Create formatting utilities (size, speed, duration, relative date) in `src/utils/format.ts`

## 3. Queue Page

- [x] 3.1 Create `QueueCard.vue` component — card for a single queue item with progress bar and cancel action
- [x] 3.2 Create `Queue.vue` view — page consuming `useQueueStream`, rendering cards, summary footer, empty state
- [x] 3.3 Register `/queue` route in `main.ts`

## 4. History Page

- [x] 4.1 Create `History.vue` view — table layout, pagination controls, category filter, retry/delete actions, empty state
- [x] 4.2 Register `/history` route in `main.ts`

## 5. Dashboard Widget

- [x] 5.1 Create `ActiveDownloads.vue` component — compact download progress widget with "View Queue" link
- [x] 5.2 Add `ActiveDownloads` widget to `Home.vue`

## 6. Navigation

- [x] 6.1 Add Queue and History nav items to `AppLayout.vue` sidebar (between Dashboard and RuleSets)
