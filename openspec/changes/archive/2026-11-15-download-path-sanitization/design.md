## Context

`DataPaths.ResolveDownload` constructs file and directory names directly from the `title` parameter (lines 74-76) without any sanitization. The title originates from MediathekViewWeb API responses — untrusted external input. A title containing path separators (`/`, `\`), traversal sequences (`..`), or invalid filename characters could produce paths outside the download directory.

The existing `data-paths` spec defines path resolution via `Path.GetFullPath` at construction time for root directories, but does not specify sanitization for dynamic title-based paths constructed at runtime.

## Goals / Non-Goals

**Goals:**
- Sanitize titles used in file and directory names to remove characters invalid on any supported filesystem (Linux ext4, Windows NTFS)
- Prevent path traversal by stripping `..` segments and path separators
- Truncate names exceeding 255 bytes (common filesystem limit)
- Validate that resolved paths are confined within the download root
- Maintain backward compatibility for existing downloads (sanitization should be idempotent for well-formed titles)

**Non-Goals:**
- Renaming or re-organizing existing completed downloads on disk
- Sanitizing titles in other contexts (display, logging, API responses)
- Unicode normalization (NFC/NFD) — out of scope for this change

## Decisions

### Decision 1: Sanitize at the point of path construction in DataPaths

Add a `SanitizeFileName` method to `DataPaths` and call it in `ResolveDownload` before constructing `dirName` and `fileName`. This centralizes sanitization at the single point where titles become paths.

**Alternative considered**: Sanitize in the DownloadWorker before passing titles to DataPaths. Rejected because DataPaths is the path-construction authority — all callers benefit from sanitization there, and a new caller cannot forget to sanitize.

### Decision 2: Replacement strategy for invalid characters

Replace each invalid character with `_` (underscore). Invalid characters are: `Path.GetInvalidFileNameChars()` (platform-specific superset) plus an explicit blocklist of `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`, and null byte. Strip leading/trailing dots and spaces (Windows filesystem restriction). Collapse consecutive underscores into one.

**Alternative considered**: Remove invalid characters entirely. Rejected because it silently changes titles more than necessary and could produce collisions (e.g. `Title/A` and `TitleA`).

### Decision 3: Path confinement check in ResolveDownload

After constructing the full `incompletePath` and `completePath`, resolve them with `Path.GetFullPath` and assert they start with the respective root (`Incomplete` or `Complete`). Throw `InvalidOperationException` if not. This is a defense-in-depth check — sanitization should prevent traversal, but the confinement check catches anything sanitization misses.

### Decision 4: Fallback for empty titles

If the sanitized title is empty (all characters were invalid), fall back to the entity ID as the filename. This prevents empty path segments.

### Decision 5: Truncation at 200 characters

Truncate sanitized names to 200 characters (not 255) to leave room for the `.mkv` extension, the entity ID suffix, and the directory nesting. Truncate at character boundaries, not byte boundaries, since .NET strings are UTF-16 and the filesystem limit is typically 255 bytes — 200 chars of ASCII is well within bounds, and multi-byte titles are also safe.

## Risks / Trade-offs

- **Risk**: Sanitization changes the effective filename for titles that contain characters like `:` (common in German broadcast titles, e.g. `Tatort: Schwarzer Freitag`). → Mitigation: This is acceptable and correct — colons are invalid on Windows. Existing Docker deployments run Linux where colons are valid in filenames, so existing files are unaffected. New downloads will use `_` instead.
- **Risk**: Two different titles could sanitize to the same string, causing a file collision. → Mitigation: The entity ID suffix (8 chars) already provides collision avoidance for non-episode titles. For episode titles, the episode identifier makes collisions extremely unlikely.
- **Trade-off**: The confinement check adds one `Path.GetFullPath` call per download. This is negligible compared to the actual download I/O.
