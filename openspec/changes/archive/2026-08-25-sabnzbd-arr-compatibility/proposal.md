## Why

Sonarr and Radarr's SABnzbd client implementation calls API modes and reads response fields that FunkArr does not serve. Source-code analysis of `Sonarr/Sonarr` and `Radarr/Radarr` (`SabnzbdProxy.cs`) reveals that `mode=fullstatus` is called during connection test (reads `completedir`), queue/history delete operations are used after import, and pagination params (`start`/`limit`) are sent on every poll. Missing these causes connection-test failures, items piling up in history, and potential misbehavior with larger queues.

## What Changes

- Add `mode=fullstatus` endpoint returning `completedir` (path-mapped) for Sonarr connection test
- Wire `mode=queue&name=delete&value={nzoId}` to existing `QueueActor.Cancel`
- Add `mode=history&name=delete&value={nzoId}` via new `QueueActor.RemoveFromHistory` (soft delete — removes from CompletedJobIds, shard entity passivates naturally)
- Add `start`/`limit` pagination to queue and history responses (controller-side slicing)
- Add missing history fields: `bytes`, `nzb_name`, `download_time`
- Add missing queue fields: `index`, `priority`
- Enrich `get_config` with `pre_check_label` and empty sorting array to suppress Sonarr false warnings
- Add `mode=retry` for re-enqueueing failed downloads (requires persisting `DownloadUrl` in tracker state)
- Accept `priority` query param on `addfile` (store, ignore value)
- Expand controller `HandleGet` signature with `name`, `value`, `start`, `limit`, `del_files` query params

## Capabilities

### New Capabilities

_None — all work extends existing capabilities._

### Modified Capabilities

- `sabnzbd-download-client`: Add fullstatus, queue/history delete, retry, pagination, missing response fields, expanded get_config
- `download-request-tracker`: Persist DownloadUrl in state, add QueryRetryInfo message for retry support

## Impact

- `src/FunkArr/Api/Controllers/SabnzbdController.cs` — main controller changes
- `src/FunkArr/Api/Contracts/Sabnzbd/SabnzbdResponses.cs` — new/updated response records
- `src/FunkArr/DownloadClient/Queue/QueueActor.cs` — new RemoveFromHistory message
- `src/FunkArr/DownloadClient/Queue/QueueActorState.cs` — handle new event
- `src/FunkArr/DownloadClient/Queue/QueueCoordinatorEvents.cs` — new JobRemovedFromHistory event
- `src/FunkArr/Persistence/QueueCoordinatorJournal.cs` — new DTO + mapping
- `src/FunkArr/DownloadClient/Tracker/DownloadRequestActor.cs` — QueryRetryInfo message
- `src/FunkArr/DownloadClient/Tracker/DownloadRequestActorState.cs` — store DownloadUrl
- `src/FunkArr.Tests/Contracts/SabnzbdContractSpec.cs` — updated wire format tests
- No breaking API changes — all additions are new modes or new fields on existing responses
