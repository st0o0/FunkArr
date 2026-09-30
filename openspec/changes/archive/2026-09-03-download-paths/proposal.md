# Download Paths

## Problem

All downloads land flat in a single directory (`DownloadPath/title.mkv`). No separation between working files (FFmpeg in progress) and completed outputs. No category-based subfolders for Sonarr/Radarr to scope their import paths. No configurable categories.

## Solution

Restructure download paths around `complete/` and `incomplete/` subdirectories under `DownloadPath`, with configurable category subfolders and automatic cleanup of working files.

### Directory layout

```
{DownloadPath}/
├── incomplete/{DownloadId}/           Working files (FFmpeg WIP)
│   ├── video.mp4
│   └── subtitle.srt
└── complete/{CategoryDir}/{Title}/    Finished files
    └── {Title}.mkv
```

### Categories

User-defined via `FunkArr:Download:Categories` config section. Each category has a `Name` (required) and optional `Dir` (defaults to Name). No built-in default categories — the list starts empty.

SABnzbd `get_config` returns categories dynamically from this config. Sonarr/Radarr see them in the download client category dropdown.

Empty or unknown category → file lands directly under `complete/` (no category subfolder).

### Config shape

```
FunkArr:Download
├── DownloadPath        (string, default: "data/downloads")
├── ConcurrentDownloads (int, default: 3)
└── Categories[]
    ├── Name            (string, required)
    └── Dir             (string, optional, defaults to Name)
```

Docker-compose example:
```yaml
environment:
  FunkArr__Download__DownloadPath: /downloads
  FunkArr__Download__ConcurrentDownloads: 3
  FunkArr__Download__Categories__0__Name: sonarr
  FunkArr__Download__Categories__1__Name: radarr
  FunkArr__Download__Categories__2__Name: dokus
  FunkArr__Download__Categories__2__Dir: dokumentationen
```

### Cleanup

Temp directory `incomplete/{DownloadId}/` is deleted automatically after successful mux. On failure, temp files remain for debugging.

## Scope

- Extract `DownloadOptions` from `FunkArrOptions` with new path structure
- Update `DownloadManager` to build output paths with `complete/category/title/` pattern
- Update `DownloadWorker` / `FfmpegRunner` to use `incomplete/{DownloadId}/` for working files
- Update SABnzbd `get_config` to return categories from config
- Update SABnzbd history `storage` and `complete_dir` to reflect new paths
- Update setup health check to verify both `complete/` and `incomplete/` directories

## Out of scope

- Path mapping (container-to-host rewrite) — not needed
- Newznab category changes — existing `NewznabCategory` model is unaffected
