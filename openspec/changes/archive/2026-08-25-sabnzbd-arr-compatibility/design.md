## Context

FunkArr exposes a SABnzbd-compatible JSON API at `/download/api` that Sonarr/Radarr use as a download client. Source-code analysis of Sonarr's `SabnzbdProxy.cs` reveals the controller is missing several modes (`fullstatus`, delete, retry), pagination support, and response fields that Sonarr reads during its lifecycle (connection test, polling, import cleanup).

Current state:
- `SabnzbdController` handles `version`, `get_config`, `queue`, `history`, `addfile`
- `QueueActor` manages the download queue with event-sourced state, has `Cancel` but no history removal
- `DownloadRequestActor` tracks per-job status but does not persist `DownloadUrl` (needed for retry)
- Response records lack fields Sonarr expects (`bytes`, `nzb_name`, `index`, `priority`, etc.)

## Goals / Non-Goals

**Goals:**
- Full compatibility with Sonarr/Radarr SABnzbd client implementation (all modes they call, all fields they read)
- Sonarr connection test passes without warnings
- Sonarr can delete items from queue and history after import
- Retry of failed downloads works through Sonarr UI

**Non-Goals:**
- SABnzbd authentication via `ma_username`/`ma_password` (FunkArr uses apikey only)
- Real download size tracking (`bytes` field) — return 0, Sonarr uses it for display only
- Priority-based scheduling — accept the param, ignore it
- SABnzbd admin features (pause/resume global queue, set speed limit, server stats)

## Decisions

### 1. Controller signature — flat query params

Sonarr sends compound queries like `mode=queue&name=delete&value=nzoId&del_files=1`. Rather than introducing sub-routing or separate endpoints (SABnzbd uses a single endpoint), expand `HandleGet` with optional `[FromQuery]` params: `name`, `value`, `start`, `limit`, `del_files`.

Route internally:
```
mode=queue  + name=null   → list (paginated)
mode=queue  + name=delete → cancel via QueueActor.Cancel
mode=history + name=null  → list (paginated)
mode=history + name=delete → remove via QueueActor.RemoveFromHistory
```

**Why not separate endpoints:** SABnzbd's API is a single endpoint with mode switching. Sonarr constructs all requests against the same base URL. Keeping one endpoint matches the real API shape.

### 2. Pagination — controller-side slicing

Fetch all entries from actors, then `Skip(start).Take(limit)` in the controller. Return total count in response for Sonarr's pagination logic.

**Why not actor-side pagination:** The queue is bounded by `MaxConcurrent` + pending items (typically <50). History grows but is bounded by completed jobs which get cleaned up by Sonarr's delete calls. Controller-side slicing avoids adding pagination concerns to event-sourced actor state. If history grows unbounded in practice, actor-side pagination can be added later without API changes.

### 3. History delete — soft delete via QueueActor

Remove nzoId from `QueueActor.CompletedJobIds` via new persisted `JobRemovedFromHistory` event. The `DownloadRequestActor` shard entity is not deleted — it passivates naturally after inactivity and its journal remains for audit/debugging.

**Why not hard delete the shard entity:** Akka.Persistence has no built-in entity deletion. Deleting journal entries is possible but adds complexity for no user-visible benefit. The shard entity passivates after the configurable idle timeout, freeing memory. Journal entries are tiny.

**Why QueueActor owns the list:** QueueActor is already the single source of truth for which nzoIds exist in queue and history. Delete is just removing from its CompletedJobIds list.

### 4. Retry — persist DownloadUrl in tracker, re-enqueue as new job

`DownloadRequestActor.TrackDownload` already receives `DownloadUrl` but the state discards it. Persist it in state (populated from the existing `RequestCreated` event — no journal schema change needed since the URL is already in the `TrackDownload` command and flows through to the event).

Wait — the persistence DTO `RequestCreated` needs to be checked. The `DownloadUrl` must be in the journal DTO.

Retry flow:
1. Controller receives `mode=retry&value=nzoId`
2. Ask `DownloadRequestActor` for retry info (url, title, subtitle url, category)
3. Tell `QueueActor.Enqueue` with original data → gets new nzoId
4. Return `{ status: true, nzo_ids: [newNzoId] }`

**Why new nzoId:** Real SABnzbd creates a new job on retry. Sonarr expects this — it tracks the new nzoId going forward.

### 5. fullstatus — minimal response

Sonarr only reads `completedir` from `fullstatus`. Return a minimal object:
```json
{ "completedir": "<path-mapped download path>" }
```

Apply `PathMappingHelper` so Sonarr sees the path as mapped for Docker volume mounts.

### 6. get_config enrichment — suppress false warnings

Sonarr checks for:
- `misc.pre_check_label` — warns if enabled (pre-flight NZB check)
- TV/movie/date sorting — warns if sorting targets the configured category

Add `pre_check_label: false` to misc config. Add empty `sorters` array (Sonarr iterates it, empty = no warnings).

### 7. Response record changes — additive only

All changes to `SabnzbdResponses.cs` are additive (new fields with defaults). No breaking changes to existing serialization. New fields use `[JsonPropertyName]` for snake_case compatibility.

## Risks / Trade-offs

**[Unbounded history list]** → CompletedJobIds grows over time if Sonarr doesn't delete. Mitigation: Sonarr always deletes after successful import. If needed later, add a cap (e.g., keep last 1000) with oldest-first eviction.

**[Retry of expired content]** → Mediathek content may be removed between original download and retry. Mitigation: DownloadActor already handles HTTP 404/410 as failures — retry would fail cleanly with a meaningful error message.

**[DownloadUrl in journal]** → Need to verify the persistence DTO includes DownloadUrl. If not, a new journal DTO version is needed. Mitigation: Check `RequestCreated` DTO fields during implementation. The DTO already has the URL field from the `TrackDownload` command flow.

**[Controller-side pagination memory]** → Fetching all history entries for large histories. Mitigation: Each entry is a small Ask to a shard entity. For 1000+ entries this could be slow. Acceptable for v1; if needed, add a history count limit or actor-side pagination.
