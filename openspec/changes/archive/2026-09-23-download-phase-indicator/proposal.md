## Why

The download progress indicator shows a single percentage based on FFmpeg's remux position (`CurrentTimeUs / TotalDuration`), not bytes downloaded. This creates a confusing UX: the size display shows "3.7 GB / 3.7 GB" (download complete) while the percentage reads "79%" (remux still running). Users see full size downloaded but incomplete progress, with no indication that the download finished and remuxing is in progress.

## What Changes

- Add a `phase` field to queue items distinguishing "downloading" from "remuxing"
- Calculate percentage differently per phase: bytes-based during download, time-based during remux
- Show the current phase label in the Activity UI so users understand what's happening
- Update both the internal REST API (`/api/downloads/queue`) and SABnzbd API (`/download/api?mode=queue`) responses

## Capabilities

### New Capabilities
- `download-phase-progress`: Two-phase progress reporting for downloads — distinguishes downloading (bytes-based) from remuxing (time-based) with separate percentage calculation and phase label

### Modified Capabilities
- `download-queue-ui`: Show phase label ("Downloading" / "Remuxing") alongside the progress bar
- `sabnzbd-download-api`: Add phase-aware percentage calculation to SABnzbd queue response
- `download-api-internal`: Add phase field and phase-aware percentage to internal queue API response

## Impact

- **Backend**: `DownloadMappingExtensions.cs` (internal API), `SabnzbdResponseMapper.cs` (SABnzbd API) — percentage calculation changes
- **Messages**: `QueueItem` record may need a phase indicator, or phase can be derived from existing `BytesDownloaded` vs `CurrentTimeUs` state
- **Frontend**: `ActiveDownloadCard.vue` — display phase label, adjust progress bar semantics
- **API models**: `DownloadQueueItem` (internal), `QueueSlot` (SABnzbd) — add phase field
