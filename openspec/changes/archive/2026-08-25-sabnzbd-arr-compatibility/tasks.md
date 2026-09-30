## 1. Response Models

- [x] 1.1 Add `SabnzbdFullStatusResponse` record with `completedir` field to `SabnzbdResponses.cs`
- [x] 1.2 Add `index` (int) and `priority` (string) fields to `SabnzbdQueueSlot`
- [x] 1.3 Add `bytes` (long), `nzb_name` (string), `download_time` (int) fields to `SabnzbdHistorySlot`
- [x] 1.4 Add `pre_check_label` (bool) to `SabnzbdMiscConfig` and `sorters` (array) to `SabnzbdConfig`
- [x] 1.5 Add `SabnzbdStatusResponse` record for delete/retry success responses
- [x] 1.6 Add `SabnzbdRetryResponse` record with `status` and `nzo_ids` (reuse `SabnzbdAddFileResponse` or create dedicated)
- [x] 1.7 Update `SabnzbdContractSpec` verified snapshots for all changed response records

## 2. DownloadRequestActor — Retry Support

- [x] 2.1 Add `DownloadUrl` and `SubtitleUrl` properties to `DownloadRequestActorState`, populate from `RequestCreated` event
- [x] 2.2 Add `SubtitleUrl` to `TrackDownload` command record and `DownloadRequestActorEvents.RequestCreated`
- [x] 2.3 Add `SubtitleUrl` field to `RequestCreated` persistence DTO (nullable, default null for backward compat)
- [x] 2.4 Update `ToJournal()`/`ToDomain()` extensions for `RequestCreated` to include `SubtitleUrl`
- [x] 2.5 Add `QueryRetryInfo` command and `RetryInfo` response records to `DownloadRequestActor`
- [x] 2.6 Add `Command<QueryRetryInfo>` handler in `DownloadRequestActor.Ready()` returning `RetryInfo`
- [x] 2.7 Update `QueueActor.HandleEnqueue` to pass `SubtitleUrl` through `TrackDownload`

## 3. QueueActor — History Delete

- [x] 3.1 Add `RemoveFromHistory(string NzoId)` command record to `QueueActor`
- [x] 3.2 Add `JobRemovedFromHistory` event to `QueueActorEvents`
- [x] 3.3 Add `QueueJobRemovedFromHistory` persistence DTO with `ToJournal()`/`ToDomain()` extensions
- [x] 3.4 Add `Apply(JobRemovedFromHistory)` to `QueueActorState` — remove nzoId from `CompletedJobIds`
- [x] 3.5 Add `HandleRemoveFromHistory` command handler in `QueueActor.Ready()` with persistence
- [x] 3.6 Add `Recover<QueueJobRemovedFromHistory>` in `QueueActor.Recovering()`

## 4. Controller — New Modes and Pagination

- [x] 4.1 Expand `HandleGet` signature with `name`, `value`, `start`, `limit`, `del_files` optional query params
- [x] 4.2 Add `mode=fullstatus` case returning `SabnzbdFullStatusResponse` with path-mapped `completedir`
- [x] 4.3 Refactor `mode=queue` case to check for `name=delete` sub-operation vs list
- [x] 4.4 Wire queue delete: tell `QueueActor.Cancel(value)`, return `SabnzbdStatusResponse(true)`
- [x] 4.5 Add `start`/`limit` pagination to `HandleQueue` — `Skip(start ?? 0).Take(limit ?? int.MaxValue)`
- [x] 4.6 Add `index` and `priority` population in queue slot construction
- [x] 4.7 Refactor `mode=history` case to check for `name=delete` sub-operation vs list
- [x] 4.8 Wire history delete: tell `QueueActor.RemoveFromHistory(value)`, return `SabnzbdStatusResponse(true)`
- [x] 4.9 Add `start`/`limit` pagination to `HandleHistory`
- [x] 4.10 Add `bytes`, `nzb_name`, `download_time` population in history slot construction
- [x] 4.11 Add `mode=retry` case: ask tracker for `RetryInfo`, validate status, re-enqueue via `QueueActor.Enqueue`
- [x] 4.12 Expand `HandlePost` to accept optional `priority` query param (ignore value)
- [x] 4.13 Enrich `HandleGetConfig` with `pre_check_label` and `sorters`

## 5. Tests

- [x] 5.1 Add contract test for `SabnzbdFullStatusResponse` wire format
- [x] 5.2 Add contract test for `SabnzbdStatusResponse` wire format
- [x] 5.3 Update existing queue/history/config contract tests for new fields
- [x] 5.4 Build and run all tests — verify green

## 6. Spec Sync

- [x] 6.1 Sync delta specs to main specs via `openspec sync`
