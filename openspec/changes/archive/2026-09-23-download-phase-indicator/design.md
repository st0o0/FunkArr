## Context

FunkArr downloads media by piping video/audio/subtitle streams through FFmpeg into an MKV container. The process has two distinct phases: (1) FFmpeg downloads the HTTP streams and (2) FFmpeg remuxes them into the output file. Both happen inside the same FFmpeg process, so `BytesDownloaded` grows during the download phase while `CurrentTimeUs` advances during remuxing.

Currently, both the internal API (`DownloadMappingExtensions.cs`) and SABnzbd API (`SabnzbdResponseMapper.cs`) calculate `percentage` as `CurrentTimeUs / TotalDuration * 100`. The UI displays this percentage alongside `BytesDownloaded / TotalBytes` size text. When the download phase finishes, the size shows "full" but percentage stays below 100% until the remux completes — confusing users.

## Goals / Non-Goals

**Goals:**
- Show which phase a download is in (downloading vs remuxing)
- Show meaningful progress per phase: bytes-based for downloading, time-based for remuxing
- Keep changes minimal — derive phase from existing state, no new actor messages

**Non-Goals:**
- Separate the FFmpeg process into two distinct steps
- Add a third "post-processing" or "moving" phase
- Change how FFmpeg is invoked or how progress is reported from the FFmpeg runner

## Decisions

### 1. Derive phase from existing state — no new messages

**Decision**: Determine the phase by comparing `BytesDownloaded` vs `TotalBytes` and `CurrentTimeUs` vs `TotalDuration` at mapping time, rather than adding a phase field to `DownloadWorkerState` or `QueueItem`.

**Rationale**: The state already carries enough information. When `BytesDownloaded < TotalBytes`, we're downloading. When `BytesDownloaded >= TotalBytes` and `CurrentTimeUs < TotalDuration`, we're remuxing. This avoids changing the actor state model or persistence format.

**Alternative considered**: Adding `DownloadPhase` enum to `QueueItem` — rejected because it couples the phase concept to the message contract and requires changes in the download worker actor. The phase is a presentation concern.

### 2. Phase-aware percentage calculation in mappers

**Decision**: Both `DownloadMappingExtensions.ToApi()` and `SabnzbdResponseMapper.BuildQueueSlot()` compute percentage based on the derived phase:
- **Downloading**: `BytesDownloaded * 100 / TotalBytes` (byte progress)
- **Remuxing**: `CurrentTimeUs / TotalDuration * 100` (time progress, same as current)

**Rationale**: Each phase shows the metric that actually correlates with what the user sees (size growing vs time advancing).

### 3. Add phase string to API response models

**Decision**: Add a `phase` string field to `DownloadQueueItem` (internal API) and include phase info in `QueueSlot.Status` (SABnzbd API). Values: `"downloading"` or `"remuxing"`.

**Rationale**: The UI needs to know which phase to display. For SABnzbd API, encoding phase in the `Status` field (e.g. `"Downloading"` / `"Remuxing"`) is compatible with how Sonarr/Radarr interpret status strings.

### 4. UI shows phase label inline

**Decision**: Display the phase as a small label next to the progress percentage in `ActiveDownloadCard.vue`. During remuxing, show a distinct visual cue (e.g. different progress bar color or label).

**Rationale**: Minimal UI change, immediately explains why "3.7 GB / 3.7 GB" shows less than 100%.

## Risks / Trade-offs

- **[Risk] Phase detection edge case**: If `BytesDownloaded == TotalBytes` but `CurrentTimeUs == 0`, the phase could briefly show "remuxing" before FFmpeg starts writing. → **Mitigation**: Treat `CurrentTimeUs == 0 && BytesDownloaded >= TotalBytes` as still "downloading" (transitional moment).
- **[Risk] SABnzbd API compatibility**: Changing `Status` from `"Downloading"` to `"Remuxing"` might confuse Sonarr/Radarr. → **Mitigation**: Keep SABnzbd `Status` as `"Downloading"` for both phases; add phase info only to the internal API. SABnzbd percentage change alone is safe.
