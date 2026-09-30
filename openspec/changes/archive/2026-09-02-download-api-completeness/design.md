## Context

The SABnzbd Download API adapter translates between the SABnzbd JSON wire format and internal download domain messages. Source code analysis of Sonarr and Radarr SABnzbd proxy implementations reveals concrete gaps: empty category lists cause connection test failures, pagination parameters are accepted but ignored, and delete operations discard the `del_files` flag.

The current structure (single `MapGet` with switch expression dispatching to static methods) is idiomatic for ASP.NET Minimal APIs and will be preserved. Microsoft's guidance recommends static methods with DI-injected parameters over interface-based handler registries for this pattern.

## Goals / Non-Goals

**Goals:**
- Fix all response fields that Sonarr/Radarr actually read during connection testing and runtime
- Forward all client-sent parameters (pagination, del_files, category, priority) through the message layer
- Add intermediate download status values that Radarr expects during pipeline processing
- Keep the adapter thin: parameter forwarding and response translation only

**Non-Goals:**
- Implementing SABnzbd modes not used by Sonarr/Radarr (pause, resume, server_stats, addurl, ~20 others)
- Changing the endpoint structure (switch + static methods stays)
- Adding business logic to the adapter layer
- Real disk space reporting (hardcoded "0" is fine for now)

## Decisions

### Keep switch + static methods over handler registry
The SABnzbd API has exactly 9 modes that Sonarr/Radarr use. This is a stable, finite set that won't grow. A handler registry pattern (IModeHandler + Dictionary dispatch) adds indirection the framework doesn't support idiomatically. ASP.NET Minimal API guidance recommends static methods with DI parameters.

### Extend existing message records with optional parameters
QueryQueue and QueryHistory gain optional pagination and category filter parameters. DeleteDownload gains an optional del_files flag. AddDownload gains an optional priority parameter. All new parameters are optional with defaults that preserve current behavior — no breaking change for existing callers (DownloadManager actors).

### Add intermediate DownloadStatus values
Radarr expects status strings like `Extracting`, `Moving`, `Verifying` during download pipeline stages. These map to new enum values in DownloadStatus. The adapter translates enum values to SABnzbd-compatible status strings. The download pipeline actors will emit these statuses as processing progresses.

### Category list is static configuration
Config categories are returned as a fixed list matching what Sonarr/Radarr expect. No dynamic category management needed — FunkArr's categories are determined by what *arr clients send, not user configuration.

### Speed fields use actual data from QueueItem
Queue slots already carry a `Speed` field (playback speed multiplier). The adapter will format this as bytes/second string. FullStatus aggregate speed sums active download speeds.

## Risks / Trade-offs

- **Message record changes touch actor code**: QueryQueue/QueryHistory/DeleteDownload are handled by DownloadManager. Adding optional parameters means the actor must handle them — but since defaults preserve current behavior, the actor can ignore them initially and add filtering/pagination incrementally.
  → Mitigation: New parameters use defaults (start=0, limit=0 meaning all, del_files=false, category=null). Actor code compiles unchanged.

- **DownloadStatus enum extension affects persistence**: Adding new enum values changes what gets persisted. Since we use extend-only persistence DTOs with integer backing, new values just get higher numbers.
  → Mitigation: New values get explicit integer assignments above existing ones. Recovery handles unknown values gracefully.

- **Category filter in adapter vs. domain**: Filtering by category could happen in the adapter (post-query) or in the domain (pre-query). Post-query is simpler but wastes work for large queues.
  → Decision: Forward category to the message. DownloadManager can filter in-memory — queue sizes are small (tens, not thousands).
