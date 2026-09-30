## Why

Sonarr polls `mode=queue` every second. Currently these queries hit `DownloadQueueActor` — the same actor that processes download events, persistence, and pipeline coordination. This couples API responsiveness to download workload. A dedicated `DownloadRequestTracker` (ShardRegion, one entity per nzoId) isolates API-facing status from download work, survives coordinator crashes, and provides a clean event-sourced history per download for the SABnzbd API contract.

## What Changes

- Introduce `DownloadRequestTracker` as a ShardRegion with one entity per `nzoId`
- Event-sourced (Tier 1): persists `RequestCreated`, `StatusChanged`, `ProgressUpdated`, `Completed`, `Failed`, `Cancelled`
- Serves SABnzbd API queries: `GetStatus` (for `mode=queue`) and `GetHistoryEntry` (for `mode=history`)
- Receives status updates from DownloadCoordinator (or temporarily from DownloadQueueActor until Phase 2c)
- Progress updates throttled (not every byte — periodic from coordinator)
- SABnzbd controller queries QueueCoordinator for job ordering, then fans out `GetStatus`/`GetHistoryEntry` to individual tracker entities
- Extract queue/history response building from `DownloadQueueActor` into tracker entities

## Capabilities

### New Capabilities
- `download-request-tracker`: ShardRegion entity (per nzoId) holding download request state — payload, status, progress, history — serving SABnzbd API queries with Tier 1 event-sourced persistence

### Modified Capabilities
- `sabnzbd-download-client`: SABnzbd controller queries DownloadRequestTracker entities for status/history instead of asking DownloadQueueActor
- `queue-api`: Queue views read from DownloadRequestTracker entities
- `persistence-dtos`: New DTO types for DownloadRequestTracker events

## Impact

- **Actors**: New `DownloadRequestTracker` shard entity; `DownloadQueueActor` loses `GetQueue`/`GetHistory` handling
- **Sharding**: First download-path ShardRegion — requires Akka.Cluster.Sharding setup (single-node, entity per nzoId)
- **Persistence**: New `PersistenceId: "download-request-{nzoId}"` per entity with Tier 1 event sourcing
- **Controllers**: `SabnzbdController` fans out `GetStatus`/`GetHistoryEntry` to shard entities instead of asking one actor
- **API contract**: SABnzbd responses unchanged — same JSON shape, different actor backend
- **Tests**: New DownloadRequestTracker actor tests; SabnzbdController integration tests adapted
