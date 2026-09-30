## Context

The `download-categories` change introduced `IDownloadFileService` to centralize filesystem operations, but left three gaps: the host still bootstraps directories, `DownloadWorker` still has one inline `Directory.CreateDirectory`, and path collisions exist for downloads without episode identifiers. The SABnzbd adapter also returns file paths where Sonarr/Radarr expect directory paths.

## Goals / Non-Goals

**Goals:**
- Make `IDownloadFileService` fully self-contained — no host involvement for directory setup
- Eliminate all inline filesystem calls from `DownloadWorker`
- Fix SABnzbd `storage` response for correct Sonarr/Radarr import
- Prevent silent overwrites when multiple downloads of the same show lack episode identifiers

**Non-Goals:**
- Path mapping for remote volume mounts (separate concern)
- Changes to `ReleaseTitleBuilder` (search domain)
- RuleSet filesystem extraction

## Decisions

### Self-bootstrapping via IHostedLifecycleService

`DownloadFileService` implements `IHostedLifecycleService.StartingAsync` to call `EnsureDirectories()` at startup. This removes the need for `ApplicationSetupContainer` to know about download internals. The service is already a singleton, so adding hosted lifecycle is zero-cost.

**Alternative considered**: Lazy initialization on first call. Rejected because directory creation at startup is cheap and fail-fast is preferable — if the download path is misconfigured, the app should fail at startup, not on first download.

### EnsureIncompletePath replaces ResolveTempPath

`ResolveTempPath` currently only resolves the path — the caller still has to create the directory. Rename to `EnsureIncompletePath` which creates the per-entity incomplete directory and returns the temp file path in one call. This eliminates the last `Directory.CreateDirectory` from `DownloadWorker`.

### Collision safety via entity ID suffix

When `ResolveOutputPath` detects that the title lacks an episode identifier (no `S01E01`, `E01`, or `yyyy-MM-dd` pattern), it appends the first 8 characters of the entity ID to the directory name. This ensures unique output paths without changing the filename pattern for well-identified content.

```
With S01E01:     complete/tv/Show.S01E05.Title.GERMAN.720p.../Show.S01E05...mkv
Without S01E01:  complete/tv/Show.Title.GERMAN.720p...-a1b2c3d4/Show.Title...mkv
                                                       ^^^^^^^^ 8-char entityId prefix
```

The disambiguator is appended to the **directory name only**, not the filename. This preserves the release title format that Sonarr/Radarr parse.

**Detection heuristic**: Regex check for `S\d{2,}E\d{2,}`, `E\d{2,}`, or `\d{4}-\d{2}-\d{2}` anywhere in the title. If none match, append the disambiguator. This is intentionally conservative — false negatives (rare titles that look like episode patterns) are harmless, while false positives (missing the disambiguator) cause overwrites.

### Storage field fix in adapter layer

The `DownloadSucceeded` persistence DTO stores `FilePath` (the MKV path). This is a persistence DTO — extend-only, cannot rename. The fix is in the SABnzbd adapter: `Path.GetDirectoryName(item.FilePath)` when building the history response. This is exactly the adapter's job — translating internal representation to SABnzbd wire format.

No changes to persistence DTOs or messages.

## Risks / Trade-offs

**[Entity ID in directory name is not human-readable]** → Acceptable tradeoff. Only affects downloads without episode identifiers. The 8-char prefix is short enough to not be ugly, long enough to avoid collisions (4 billion unique values).

**[Heuristic may misidentify titles]** → A title containing "2025-01-15" as part of its name (not as an episode date) would skip the disambiguator. This is extremely unlikely with Mediathek content and harmless if it happens — the title is still unique enough from the rest of the content.
