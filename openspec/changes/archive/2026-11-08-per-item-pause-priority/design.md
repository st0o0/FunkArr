## Context

FunkArr exposes a SABnzbd-compatible download client API. Sonarr/Radarr use priority `-2` to pause individual queue items and any other priority value to resume them. FunkArr currently clamps priority to `[-1, 1]`, silently dropping the pause intent.

The global pause mechanism (`PauseDownloads`/`ResumeDownloads`) already works — this change adds per-item granularity using the existing priority field.

Mid-download pause was investigated and ruled out: FFmpeg streams via HLS/HTTP are not resumable, and FFMpegCore only supports cancel (kill process), not suspend/resume.

## Goals / Non-Goals

**Goals:**
- Honor SABnzbd priority -2 as per-item pause for queued (not-yet-dispatched) downloads
- Report per-item paused status back to Sonarr/Radarr via QueueSlot
- Allow resume by setting priority to any non-pause value
- Expose pause/resume through internal API for future UI use

**Non-Goals:**
- Pausing already-running downloads (FFmpeg streams are not resumable)
- New persistence events for pause/resume (priority is already persisted)
- New PauseDownload/ResumeDownload actor commands (priority change IS the mechanism)
- UI changes (future work)

## Decisions

### Priority -2 as the pause mechanism, not a separate command

Priority -2 maps directly to SABnzbd semantics. Adding separate `PauseDownload`/`ResumeDownload` commands would create two paths to the same state and require extra persistence events. Since priority is already persisted and round-tripped, adding `Paused = -2` to the enum is sufficient.

Alternative considered: Separate `bool Paused` flag on QueueEntry. Rejected because it duplicates state — priority -2 already carries the same information, and SABnzbd clients set priority to resume (not a separate "resume" call).

### DispatchNext skip vs. separate paused collection

Paused items stay in the `Queued` list at their current position. `DispatchNext` skips entries with `Priority == Paused`. When priority changes to non-paused, the item is already in the correct bucket position.

Alternative considered: Move paused items to a separate `Paused` collection. Rejected because it complicates resume (need to re-insert at correct bucket position) and breaks the existing priority sort invariant.

### Internal API endpoints use priority under the hood

`POST /api/downloads/{id}/pause` sets priority to Paused. `POST /api/downloads/{id}/resume` sets priority to Normal. These are convenience wrappers — no new actor commands needed.

### Already-dispatched items: no-op

If a pause request arrives for a download that's already dispatched (FFmpeg running), the priority is updated in state but the download continues. The QueueSlot will not report it as paused since it's in "Downloading" status. This matches reality — we can't pause FFmpeg mid-stream.

## Risks / Trade-offs

- **[Paused items block queue position]** A paused High-priority item sits in the queue but doesn't dispatch. Items behind it in the same bucket dispatch normally since DispatchNext iterates and skips. → No mitigation needed, this is correct behavior.
- **[Priority -2 on addfile]** Sonarr/Radarr could theoretically send `addfile` with priority -2 (add already paused). → Support it: enqueue with `Paused` priority, item sits in queue without dispatching.
- **[Recovery]** Persisted priority values include -2 after this change. Rolling back would require ignoring unknown priority values gracefully. → Enum mapping `_ => throw` would catch this at startup. Acceptable for v0.x.
