## Context

FunkArr emulates a SABnzbd download client for Sonarr/Radarr integration. Currently all downloads land in a single `DownloadPath` directory. Sonarr and Radarr send a `cat` parameter when submitting downloads, expecting the download client to route completed files into category-specific folders. This is a standard feature of real SABnzbd that FunkArr lacks.

The download pipeline is actor-based: `QueueCoordinator` (singleton, event-sourced) schedules jobs, `DownloadCoordinator` (sharded, event-sourced) runs the stage machine per job, and `DownloadRequestTracker` (sharded, event-sourced) tracks status for API queries.

## Goals / Non-Goals

**Goals:**
- Accept and thread `cat` parameter from SABnzbd `addfile` through the full pipeline
- Resolve category to output directory with configurable overrides
- Expose category in queue/history API responses (both SABnzbd and clean API)
- Return configured categories dynamically in SABnzbd `get_config`
- Rename `DownloadPath` → `Path` for cleaner env-var ergonomics

**Non-Goals:**
- Per-category `PathMapping` — users handle this via Sonarr/Radarr Remote Path Mappings
- Category-based priority or concurrency limits
- Category validation or allowlisting — any `cat` value is accepted
- Migration tooling for the `DownloadPath` → `Path` rename

## Decisions

### 1. Category resolution lives in a static helper, not in QueueCoordinator

**Decision:** Extract category → path resolution into a static `CategoryResolver.Resolve(downloadPath, category, categoryConfig)` method.

**Why:** The resolution logic (rooted check, combine, fallback) is pure and testable without actor infrastructure. QueueCoordinator calls it when building `StartDownload.OutputDir`. This keeps the actor focused on scheduling and makes the resolution trivially unit-testable.

**Alternative:** Inline in QueueCoordinator — rejected because it tangles scheduling with path logic and makes testing harder.

### 2. Category flows as metadata, FileService resolves paths

**Decision:** Category flows as an opaque `string?` through all messages (`StartDownload`, `RemuxVideo`, persistence DTOs). No pre-resolved `OutputDir` in messages. `FileService` holds `_categoryConfig` from `DownloadOptions` and resolves category → output path internally when `GetOutputPath(title, category)` or `EnsureOutputDirectory(title, category)` is called.

**Why:** No path strings flow through actor messages — only the category metadata. Resolution is centralized in FileService (which already owns path construction). Workers pass category through opaquely without understanding it. If category config changes, recovered jobs automatically resolve to the new path (the file is already at its final location from `JobCompleted`, so this only affects in-flight jobs resuming after recovery — which is correct behavior since they haven't muxed yet).

**Alternative:** Resolve in QueueCoordinator and pass `OutputDir` in `StartDownload` — rejected because it forces path strings through messages, duplicates path logic outside FileService, and freezes the resolved path at enqueue time rather than at mux time.

### 3. `DownloadOptions.Category` is `Dictionary<string, string>`

**Decision:** Use a flat `Dictionary<string, string>` for category-to-path mapping. .NET configuration binding handles `FunkArr__Download__Category__<name>=<path>` automatically.

**Why:** Simplest possible config structure. No nested objects needed — a category only has a path. The dictionary key is the category name, the value is the path (absolute or relative).

### 4. Breaking rename: `DownloadPath` → `Path`

**Decision:** Clean rename with no backwards compatibility shim.

**Why:** Project is pre-1.0, clean breaks are acceptable. `Path` avoids redundancy (`FunkArr__Download__Path` vs `FunkArr__Download__DownloadPath`). All references in code, config, and docs update together.

### 5. Persistence: extend-only with nullable `Category`

**Decision:** Add `[JsonProperty("cat")] public string? Category { get; set; }` to relevant persistence DTOs (`JobEnqueuedDto`, `RequestCreatedDto`, `JobAcceptedDto`). No version bump needed.

**Why:** Nullable property with no default means old events without the key deserialize to `null`, which is correct (no category). Extend-only is the established persistence convention.

## Risks / Trade-offs

- **[Config change breaks existing deployments]** → Documented as breaking change. Users must update `FunkArr__Download__DownloadPath` to `FunkArr__Download__Path`. Acceptable for 0.x.
- **[Category name as subfolder could create unexpected directories]** → Mitigated by sanitizing invalid filesystem characters. Sonarr/Radarr typically send clean category names (`tv`, `movies`).
- **[Config change after jobs are persisted]** → If a user changes category config, already-started jobs keep their resolved OutputDir (persisted in DownloadCoordinator events). Only new jobs use the new config. This is correct behavior — in-flight downloads shouldn't move.
- **[Case sensitivity]** → Category lookup is case-insensitive via `StringComparer.OrdinalIgnoreCase` on the dictionary. SABnzbd is case-insensitive, so we match that behavior.

## Open Questions

None — design is straightforward and all decisions were made during exploration.
