# Download Paths — Design

## Context

All downloads currently land flat in `{DownloadPath}/{title}.mkv`. No separation between working files and finished outputs, no category subfolders. The `DownloadPath` is a property on the monolithic `FunkArrOptions`. SABnzbd `get_config` returns hardcoded categories. Sonarr/Radarr need category-scoped paths for reliable import.

## Goals / Non-Goals

**Goals:**
- Separate working files (`incomplete/`) from finished outputs (`complete/`)
- Category-based subfolders under `complete/` for Sonarr/Radarr import isolation
- User-configurable categories via standard .NET config binding (ENV-friendly)
- Extract `DownloadOptions` as a dedicated config section
- Automatic cleanup of `incomplete/{DownloadId}/` after successful mux

**Non-Goals:**
- Path mapping (container-to-host rewrite) — not needed
- Newznab category model changes
- Default built-in categories — list starts empty, fully user-defined

## Decisions

### 1. Directory structure: `complete/` and `incomplete/` under DownloadPath

```
{DownloadPath}/
├── incomplete/{DownloadId}/           Working files
│   ├── video.mp4
│   └── subtitle.srt
└── complete/{CategoryDir}/{Title}/    Finished output
    └── {Title}.mkv
```

**Why `incomplete/complete` instead of separate TempPath:** Single root path simplifies Docker volume mounts. One `DownloadPath` config instead of two. Mirrors SABnzbd's own `complete_dir`/`incomplete_dir` convention that Sonarr/Radarr users already understand.

**Why title subfolder:** Sonarr/Radarr expect the `storage` path to be a directory they can scan. A directory per download is the SABnzbd convention.

**Why DownloadId for incomplete, not title:** Avoids filename collisions during concurrent downloads of similarly-named content. Temp files are ephemeral — human-readable names don't matter here.

### 2. DownloadOptions as dedicated config class

```csharp
public sealed class DownloadOptions
{
    public const string SectionName = "FunkArr:Download";

    public string DownloadPath { get; set; } = "data/downloads";
    public int ConcurrentDownloads { get; set; } = 3;
    public List<DownloadCategory> Categories { get; set; } = [];
}

public sealed class DownloadCategory
{
    public string Name { get; set; } = "";
    public string Dir { get; set; } = "";
}
```

**Why extract from FunkArrOptions:** Follows the decomposition pattern from the old architecture. Keeps `FunkArrOptions` focused on global concerns (ApiKey, DataPath). Makes `ConcurrentDownloads` configurable (currently hardcoded to 3).

**Why `Dir` defaults to empty string, not to Name:** Empty means "use Name as directory". This keeps the common case zero-config: `Categories__0__Name=sonarr` is all you need. The resolution logic: `string.IsNullOrEmpty(cat.Dir) ? cat.Name : cat.Dir`.

### 3. Category resolution for output path

```
ResolveCategoryDir(category):
  1. Find matching DownloadCategory by Name (case-insensitive)
  2. Found → use Dir (or Name if Dir is empty)
  3. Not found or empty category → no category subfolder (directly under complete/)
```

Empty/unknown category lands in `complete/` root. No `_unsorted` fallback directory.

### 4. Path construction in DownloadManager

The Manager builds both paths when handling `AddDownload`:

```csharp
var incompletePath = Path.Combine(downloadPath, "incomplete", downloadId.ToString());
var categoryDir = ResolveCategoryDir(cmd.Category);
var outputDir = string.IsNullOrEmpty(categoryDir)
    ? Path.Combine(downloadPath, "complete", cmd.Title)
    : Path.Combine(downloadPath, "complete", categoryDir, cmd.Title);
var outputPath = Path.Combine(outputDir, cmd.Title + ".mkv");
```

Both `incompletePath` and `outputPath` are passed to the Worker via `InitDownload`. The Worker uses `incompletePath` for FFmpeg working files and `outputPath` for the final mux output.

### 5. SABnzbd `get_config` categories from config

Currently hardcoded to `["sonarr", "radarr", "tv", "movies"]`. Change to dynamically read from `DownloadOptions.Categories`. The `complete_dir` in config and `fullstatus` SHALL point to `{DownloadPath}/complete`.

### 6. Cleanup strategy

After successful mux (FFmpeg exit 0), the Worker deletes `incomplete/{DownloadId}/`. On failure, temp files remain for debugging. Cleanup is a best-effort `Directory.Delete(path, recursive: true)` — failure to clean up is logged but does not affect download status.

### 7. Directory creation

The Manager or Worker SHALL ensure `incomplete/{DownloadId}/` and the output directory (`complete/{categoryDir}/{title}/`) exist before starting FFmpeg. Use `Directory.CreateDirectory` which is idempotent.

## Risks / Trade-offs

- **[Risk] Persistence compatibility** — Existing `DownloadInitialized` events store `OutputPath` as a flat path. Workers that recover from old events will have the old flat path. → Mitigation: Recovery works with whatever path is persisted. Only new downloads get the new structure.
- **[Risk] DownloadPath change requires config update** — Users who set `FunkArr__DownloadPath` need to change to `FunkArr__Download__DownloadPath`. → Mitigation: v0.x, breaking changes are fine.
- **[Trade-off] Empty category = no subfolder** — Downloads without a recognized category land directly in `complete/`. This keeps the no-config experience simple but means mixed content if users don't configure categories.
