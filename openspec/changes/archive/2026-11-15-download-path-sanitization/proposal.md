## Why

Download file and directory names are constructed from MediathekViewWeb titles without any sanitization. Since titles are untrusted external input, a crafted title containing path separators (`/`, `\`) or traversal sequences (`..`) could write files outside the configured download directory. This is a path traversal vulnerability that should be fixed before any public release.

## What Changes

- Add a filename sanitization step in `DataPaths` that strips invalid filename characters, path separators, `..` sequences, and null bytes from titles before using them in path construction
- Add a path confinement check in `DataFiles.Move` to verify that both source and destination resolve to paths within the expected download root
- Truncate sanitized names that exceed filesystem limits (255 chars)
- Handle edge case where sanitization produces an empty string (fallback to entity ID)

## Capabilities

### New Capabilities
- `filename-sanitization`: Sanitize untrusted strings for use as file and directory names, with path confinement validation

### Modified Capabilities
- `data-paths`: Add sanitization to the title-based path construction in `ResolvePaths` methods (incomplete and complete path resolution)
- `data-files`: Add path confinement validation to `Move` to reject destinations outside the download root

## Impact

- `src/FunkArr.Core/Data/DataPaths.cs` — path construction methods that accept titles
- `src/FunkArr.Core/Data/DataFiles.cs` — Move operation
- `src/FunkArr.Core.Tests/` or `src/FunkArr.Download.Tests/` — new tests for sanitization and confinement
- No API changes, no persistence changes, no breaking changes
