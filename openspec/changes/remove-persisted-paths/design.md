## Context

The DownloadManager currently computes `IncompletePath` and `OutputPath` from `DownloadOptions` and passes them through `InitDownload` → `DownloadInitialized` → `DownloadWorkerState`. These infrastructure paths are persisted in the event journal, making them fragile to config changes and coupling the Manager to path-resolution logic.

## Goals / Non-Goals

**Goals:**
- Messages carry only domain intent (metadata), not infrastructure details
- Paths computed at runtime from current config, always reflecting latest settings
- Simplify Manager — it only queues and dispatches, no path logic
- Simplify Worker state — fewer fields to persist and recover

**Non-Goals:**
- Changing path structure or category resolution logic (stays in `DownloadOptions`)
- Adding new options or config sections
- Migrating existing journal data (v0.x, clean break)

## Decisions

### Decision: Worker computes paths in HandleStart, not HandleInit

Path computation moves to `HandleStart` because that's when the Worker actually needs the paths (to create directories and launch FFmpeg). `HandleInit` persists only domain metadata.

**Alternative considered**: Compute in `HandleInit` and store as local (non-persisted) fields. Rejected because the Worker may recover and receive `StartDownload` without a preceding `HandleInit` — the paths must be recomputable from persisted state + config at any point.

**Implementation**: Worker receives `DownloadOptions` via `IOptionsMonitor<DownloadOptions>` in constructor. In `HandleStart`, it computes:
- `incompletePath = Path.Combine(options.IncompletePath, entityId)`
- `outputPath = Path.Combine(options.CompletePath, categoryDir, title, title + ".mkv")`

These are stored as private fields (not in the state record), recomputed on recovery.

### Decision: Paths as private fields, not state record members

Paths are runtime-derived, not persisted. They live as `private string?` fields on the actor, computed in `HandleStart` and in `OnRecoveryCompleted` (for Workers recovering in Downloading→Initialized state that will receive a new StartDownload).

### Decision: HandleReset persists a lightweight reset event

Currently `HandleReset` re-persists a full `DownloadInitialized` with all metadata including paths. With paths removed from the event, `HandleReset` can persist the same `DownloadInitialized` (minus path fields) — or a simpler dedicated reset event. Using the existing `DownloadInitialized` event (now without paths) keeps the change minimal.

## Risks / Trade-offs

- **[Breaking persistence]** `DownloadInitialized` shape changes. Old journals won't replay correctly. → Acceptable at v0.x per project policy. No migration needed.
- **[Config change mid-download]** If `DownloadPath` changes while a download is active, the Worker uses the new path on next start but the incomplete directory from the old config remains. → Existing behavior is the same (paths were snapshot at init time). No regression.
