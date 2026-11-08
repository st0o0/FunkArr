## Why

Sonarr and Radarr send SABnzbd priority `-2` to pause individual queue items. FunkArr currently clamps priority to `[-1, 1]` via `Math.Clamp`, silently converting pause requests into low-priority downloads. This breaks the per-item pause/resume workflow that users expect from the *arr UI.

## What Changes

- Add `Paused = -2` to `DownloadPriority` (Messages) and `PersistedDownloadPriority` (Persistence) enums
- Remove `Math.Clamp` in SABnzbd `SetPriority` handler — allow -2 through as a valid priority value
- `DownloadManagerState.DispatchNext()` skips queue entries with `Priority == Paused`
- SABnzbd `QueueSlot` reports per-item paused status derived from priority
- Setting priority to any value other than -2 on a paused item resumes it (back in normal dispatch order)
- Already-running downloads are not affected — mid-download pause is not feasible (non-resumable HLS/HTTP streams via FFmpeg)
- Internal API gets `/api/downloads/{id}/pause` and `/api/downloads/{id}/resume` endpoints for UI control

## Capabilities

### New Capabilities

_None — this extends existing download-priority behavior._

### Modified Capabilities

- `download-priority`: Add `Paused = -2` enum value, SABnzbd priority -2 mapping, dispatch skip behavior, and per-item paused status reporting

## Impact

- **Messages**: `DownloadPriority` enum gains `Paused = -2`
- **Persistence**: `PersistedDownloadPriority` enum gains `Paused = -2`, existing stored values unaffected
- **Download domain**: `DownloadManagerState.DispatchNext()` logic, priority bucket sorting
- **ArrApi**: `SetPriority` handler, `QueueSlot` mapping
- **Api**: New pause/resume endpoints
- **UI**: Queue view can show per-item paused state (future — not in scope for this change)
