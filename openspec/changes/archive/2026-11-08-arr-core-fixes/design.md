## Context

Sonarr/Radarr integrate with FunkArr via the SABnzbd download client API. They poll `mode=history&limit=N` to detect completed/failed downloads, and filter items by exact category name. Four data-layer bugs prevent correct operation:

1. History is returned oldest-first — Arr only sees the oldest N records
2. Category from `addfile` is reduced to a 2-value enum, then mapped back to hardcoded strings that don't match what the Arr registered with
3. Empty `pubDate` when `AiredAt` is null causes Arr to drop the entire feed
4. SABnzbd retry deletes the history entry before confirming success

## Goals / Non-Goals

**Goals:**
- History query returns newest-first so Arr always sees recent completions
- Raw category string from `addfile` is stored and echoed back verbatim
- Every Newznab search result has a valid `pubDate`
- Retry only removes the history entry after confirmed re-queue

**Non-Goals:**
- Changing MediaType enum or its mapping logic (category echo is additive)
- Fixing search cache key (that's search-correctness change)
- Fixing guid stability (that's stable-guid change)
- Fixing del_files file deletion (that's download-file-cleanup change)
- Category sub-classification beyond what Arr registers

## Decisions

### 1. History ordering: reverse in ToHistoryResult

`DownloadHistoryManagerState.ToHistoryResult` currently does `Records.Skip(start).Take(limit)` on a chronologically-appended list. Fix: reverse the iteration order before Skip/Take. This keeps the append-order storage intact (no migration) and only affects the query projection.

Implementation: use LINQ `.OrderByDescending` on the completion timestamp (or simply `.AsEnumerable().Reverse()` since Records is append-ordered) before Skip/Take.

### 2. Category mapping: align output with config

The `cat` parameter from `addfile` is reduced to `MediaType.Show`/`MediaType.Movie` by `ParseMediaType`. On output, `MapMediaTypeToCategory` returns `"movie"` (singular) but Radarr registers with category `"movies"` (plural, matching `get_config.movie_categories`). Radarr filters queue/history by exact category name, so items never match.

Approach: Change `MapMediaTypeToCategory` to return `"movies"` for `MediaType.Movie`. This is a one-line fix that aligns the output with what the config advertises. No threading of raw category strings through the pipeline — the enum round-trip already works for the two supported media types, just the output string was wrong.

Also update `ParseMediaType` to accept both `"movie"` and `"movies"` (it already does via the default case, but make it explicit).

### 3. pubDate fallback: current UTC

In `NewznabSearchService`, when building the RSS item, if `AiredAt` is null, use `DateTimeOffset.UtcNow` as the pubDate value. This ensures every item has a valid RFC-822 date. The air date is the semantically correct value when available; UTC now is a safe fallback that prevents feed parse errors.

### 4. Safe retry: delete after confirmation

In `SabnzbdDownloadService.RetryDownload`, the current flow is:
1. Delete history entry
2. Send retry message to DownloadManager
3. If retry fails, the history entry is already gone

Fix: Send the retry message first, await the response, and only delete the history entry on success. On failure, leave the history entry intact and return an error.

## Risks / Trade-offs

- **Category field on persistence DTO**: Extend-only addition. Existing persisted events will deserialize with `Category = null`, which is handled by the fallback to `MapMediaTypeToCategory`.
- **History reverse**: Negligible cost — the list is bounded at 1000 records. LINQ Reverse on a List<T> is O(n) but n ≤ 1000.
- **pubDate now**: The fallback time won't match the actual air date, but it prevents feed crashes and Arr's age-based rules still work within reasonable bounds.
- **Retry ordering**: The retry now requires an Ask (await response) instead of Tell-and-forget. This is correct — the caller needs to know if re-queue succeeded before deleting the record.
