## Why

Sonarr and Radarr send a `cat` parameter (e.g., `tv`, `movies`) when submitting downloads to a SABnzbd-compatible API. The download client uses this category to route completed files into category-specific folders, which the *arr apps then monitor for import. FunkArr currently ignores this parameter — all downloads land in a single flat directory, forcing users into manual folder management or preventing multi-app setups (Sonarr + Radarr) from working cleanly.

## What Changes

- **BREAKING**: Rename `DownloadOptions.DownloadPath` to `Path` (env: `FunkArr__Download__Path`). Clean break, no migration — project is pre-1.0.
- Accept the `cat` query parameter on the SABnzbd `addfile` endpoint.
- Thread category through the full download pipeline: `SabnzbdController` → `QueueCoordinator` → `DownloadCoordinator` → `RemuxWorker` → `DownloadRequestTracker`.
- Resolve output directory per category with three-tier logic:
  1. Configured absolute path (`/data/movies`) → used as-is.
  2. Configured relative path (`serien`) → appended to `Path`.
  3. No config for category → category name used as subfolder under `Path`.
  4. No category sent → `Path` directly.
- Return category in SABnzbd queue/history API responses.
- Return configured categories dynamically in SABnzbd `get_config` response (replacing hard-coded entries).
- Add new `Dictionary<string, string> Category` to `DownloadOptions` for optional per-category path overrides.
- Extend persistence DTOs with nullable `Category` field (extend-only, backwards compatible).

## Capabilities

### New Capabilities

- `category-routing`: Category-based output directory resolution — config model, path resolution logic, and category metadata threading through the download pipeline.

### Modified Capabilities

- `sabnzbd-download-client`: Accept `cat` parameter on `addfile`, return category in queue/history responses, dynamically populate `get_config` categories.
- `download-coordinator`: Carry category metadata through the stage machine and pass resolved output directory.
- `download-request-tracker`: Store and expose category in per-download state and API responses.
- `queue-coordinator`: Thread category through enqueue/start-download messages and resolve category → output directory.
- `options-structure`: Rename `DownloadPath` → `Path`, add `Category` dictionary.
- `persistence-dtos`: Extend event DTOs with nullable category fields.
- `queue-api`: Return category field in queue/history API responses.

## Impact

- **Config**: `DownloadPath` → `Path` rename breaks existing env vars / appsettings. Users must update `FunkArr__Download__DownloadPath` to `FunkArr__Download__Path`.
- **API**: SABnzbd `addfile` gains optional `cat` parameter (additive). Queue/history responses gain `cat` field (additive).
- **Persistence**: New nullable fields in event DTOs — old events without category will deserialize with `null` (no migration needed).
- **Affected code**: `DownloadOptions`, `SabnzbdController`, `QueueController`, `QueueCoordinator`, `DownloadCoordinator`, `DownloadRequestTracker`, `RemuxWorker`, persistence journal DTOs, `appsettings.json`.
