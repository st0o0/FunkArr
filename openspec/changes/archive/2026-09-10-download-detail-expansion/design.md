## Context

The download pipeline already carries rich metadata: `InitDownload` includes `Channel`, `SubtitleUrl`, and `Duration`. The `DownloadWorkerState` holds all of these. However, when the Worker responds to `QueryWorkerStatus`, it builds a `WorkerStatusResult` that omits `Channel` and `SubtitleUrl`. The Manager then constructs `QueueItem` records that also lack these fields. The API model `DownloadQueueItem` follows suit.

The existing queue UI spec already says cards should show "channel and category as metadata", but the API never delivers the channel. This change closes that gap.

## Goals / Non-Goals

**Goals:**
- Pass channel, subtitle presence, and video duration through the full query chain: Worker -> Manager -> API -> UI
- Display channel name, a subtitle badge, and formatted duration on queue cards

**Non-Goals:**
- Adding subtitle download progress tracking
- Changing persistence DTOs (no new events, these are runtime query fields)
- Adding channel/subtitle info to history items (separate concern)

## Decisions

### 1. HasSubtitles bool vs. SubtitleUrl string

**Decision**: Expose `HasSubtitles` (bool) rather than the full `SubtitleUrl`.

The UI only needs to know if subtitles are being downloaded, not the URL. The Worker may also null out `SubtitleUrl` on subtitle failure and retry without subtitles - so `HasSubtitles` reflects the *current* state accurately. Sending the raw URL would be unnecessary detail and a mild info leak.

### 2. TotalDuration on QueueItem and API model

**Decision**: Add `TotalDuration` to `QueueItem` and `DownloadQueueItem`.

`TotalDuration` is already on `QueueItem` as it's used for percentage calculation in the API layer. The API model doesn't expose it, though. Adding it lets the UI show video length (e.g., "52 min") and is essentially free since the data is already flowing through.

### 3. WorkerStatusResult field additions

**Decision**: Add `Channel` (string) and `HasSubtitles` (bool) to `WorkerStatusResult`.

The Worker already has `_state.Channel` and `_state.SubtitleUrl` in scope when building the response. Two new fields, no new messages or queries needed.

## Risks / Trade-offs

- **Record parameter growth**: `WorkerStatusResult` goes from 10 to 12 fields, `QueueItem` from 9 to 12. Still under the "flat record" threshold from conventions. No nesting needed.
- **SSE payload size increase**: Marginal - adding a short channel string, a bool, and an int per item.
