## 1. Enums and persistence

- [x] 1.1 Add `Paused = -2` to `DownloadPriority` enum in `FunkArr.Messages`
- [x] 1.2 Add `Paused = -2` to `PersistedDownloadPriority` enum in `FunkArr.Persistence`
- [x] 1.3 Update enum mapping switch expressions between `DownloadPriority` and `PersistedDownloadPriority` to include the new value

## 2. Download domain — dispatch logic

- [x] 2.1 Update `DownloadManagerState.DispatchNext()` to skip queue entries with `Priority == Paused`
- [x] 2.2 Verify paused items stay in `Queued` list at their position (no separate collection)

## 3. ArrApi — SABnzbd adapter

- [x] 3.1 Remove `Math.Clamp` in `SetPriority` handler — allow -2 through to `DownloadPriority.Paused`
- [x] 3.2 Update `QueueSlot` mapping to report `status: "Paused"` and `priority: "Low"` for paused items
- [x] 3.3 Verify `addfile` with `priority=-2` enqueues in paused state

## 4. Internal API endpoints

- [x] 4.1 Add `POST /api/downloads/{id}/pause` endpoint (sets priority to Paused)
- [x] 4.2 Add `POST /api/downloads/{id}/resume` endpoint (sets priority to Normal)

## 5. Tests

- [x] 5.1 Test `DownloadManagerState.DispatchNext()` skips paused items
- [x] 5.2 Test enum mapping round-trip for `Paused = -2`
- [x] 5.3 Test SABnzbd `SetPriority` with value -2 sets `DownloadPriority.Paused`
- [x] 5.4 Test `QueueSlot` reports paused status for paused items
- [x] 5.5 Test internal API pause/resume endpoints
