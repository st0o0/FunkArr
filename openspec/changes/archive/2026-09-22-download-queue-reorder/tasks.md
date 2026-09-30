## 1. Types and Enum

- [x] 1.1 Add `DownloadPriority` enum (`Low = -1`, `Normal = 0`, `High = 1`) to `FunkArr.Messages.Download`
- [x] 1.2 Add `QueueEntry` readonly record struct (`Guid Id`, `DownloadPriority Priority`) to `FunkArr.Download`

## 2. Persistence Events

- [x] 2.1 Extend `DownloadEnqueued` with `DownloadPriority Priority` field in `FunkArr.Persistence.Events.Download`
- [x] 2.2 Add `DownloadMoved(Guid DownloadId, int Position)` persistence event
- [x] 2.3 Add `DownloadSwapped(Guid DownloadId1, Guid DownloadId2)` persistence event
- [x] 2.4 Add `DownloadPriorityChanged(Guid DownloadId, DownloadPriority Priority)` persistence event

## 3. Messages

- [x] 3.1 Add `MoveDownload` command and `MoveDownloadResponse` (Completed/Failed) to `FunkArr.Messages.Download`
- [x] 3.2 Add `SwapDownloads` command and `SwapDownloadsResponse` (Completed/Failed) to `FunkArr.Messages.Download`
- [x] 3.3 Add `SetDownloadPriority` command and `SetDownloadPriorityResponse` (Completed/Failed) to `FunkArr.Messages.Download`
- [x] 3.4 Extend `AddDownload` with `DownloadPriority Priority = DownloadPriority.Normal` parameter
- [x] 3.5 Extend `QueueItem` with `DownloadPriority Priority` field

## 4. State and Apply Methods

- [x] 4.1 Change `DownloadManagerState.Queued` from `IReadOnlyList<Guid>` to `IReadOnlyList<QueueEntry>`
- [x] 4.2 Change `DownloadManagerState.Dispatched` from `IReadOnlySet<Guid>` to `IReadOnlyDictionary<Guid, DownloadPriority>`
- [x] 4.3 Update `Apply(DownloadEnqueued)` to create QueueEntry with priority, insert at end of priority bucket
- [x] 4.4 Update `Apply(DownloadDispatched)` to look up priority from Queued, store in Dispatched dict
- [x] 4.5 Update `Apply(DownloadDequeued)` for new collection types
- [x] 4.6 Update `ResetDispatched()` to reconstruct QueueEntry with priority from Dispatched dict
- [x] 4.7 Add `Apply(DownloadMoved)` — remove and re-insert at position clamped to bucket boundaries
- [x] 4.8 Add `Apply(DownloadSwapped)` — exchange positions of two same-bucket entries
- [x] 4.9 Add `Apply(DownloadPriorityChanged)` — remove, change priority, insert at end of target bucket
- [x] 4.10 Update `Contains(Guid)` and `PaginateQueue()` for QueueEntry type

## 5. DownloadManager Actor

- [x] 5.1 Update `AddDownload` handler to pass priority to `DownloadEnqueued` event
- [x] 5.2 Add `MoveDownload` handler — validate ID in Queued, Persist `DownloadMoved`, reply
- [x] 5.3 Add `SwapDownloads` handler — validate both IDs in Queued, validate same priority, Persist `DownloadSwapped`, reply
- [x] 5.4 Add `SetDownloadPriority` handler — validate ID in Queued, Persist `DownloadPriorityChanged`, call `DispatchNext()`, reply
- [x] 5.5 Add recovery handlers for `DownloadMoved`, `DownloadSwapped`, `DownloadPriorityChanged`
- [x] 5.6 Update `QueryQueue` handler to include priority from state when assembling QueueItem responses

## 6. Internal API Endpoints

- [x] 6.1 Add `POST /api/downloads/queue/{id:guid}/move` endpoint in `DownloadsApiEndpoints`
- [x] 6.2 Add `POST /api/downloads/queue/{id:guid}/priority` endpoint in `DownloadsApiEndpoints`
- [x] 6.3 Add `POST /api/downloads/queue/swap` endpoint in `DownloadsApiEndpoints`

## 7. SABnzbd API

- [x] 7.1 Add `Value2` query parameter to `DownloadGetRequest`
- [x] 7.2 Add `name=priority` handler in SABnzbd queue mode — map priority int, handle Force (2) as `ForceStartDownload`
- [x] 7.3 Add `name=switch` handler in SABnzbd queue mode — parse value/value2 as GUIDs, send `SwapDownloads`
- [x] 7.4 Update `addfile` POST handler to read and use the `priority` query parameter
- [x] 7.5 Update `QueueSlot` mapping to use actual priority string instead of hardcoded `"Normal"`

## 8. Tests

- [x] 8.1 Test `DownloadManagerState.Apply(DownloadEnqueued)` with different priorities — verify bucket ordering
- [x] 8.2 Test `Apply(DownloadMoved)` — move within bucket, clamp at bucket boundaries
- [x] 8.3 Test `Apply(DownloadSwapped)` — same bucket success, cross-bucket rejection
- [x] 8.4 Test `Apply(DownloadPriorityChanged)` — re-insertion at end of target bucket
- [x] 8.5 Test `Apply(DownloadDispatched)` — priority preserved in Dispatched dict
- [x] 8.6 Test `ResetDispatched()` — priority reconstructed on recovery
- [x] 8.7 Test `DownloadManager` actor handlers for move, swap, priority (TestKit)
- [x] 8.8 Test SABnzbd priority mapping including Force (2) edge case

## 9. Build Verification

- [x] 9.1 Run `dotnet build src/FunkArr.slnx` — fix all compilation errors from breaking state changes
- [x] 9.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 9.3 Run all test projects and verify green
