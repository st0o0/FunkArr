## Context

Two backlog items from the ArrApi audit remain unresolved:

1. **Daily show episode matching (#14)**: `FilterBySeasonEpisode` (NewznabSearchService:74-103) compares `ep` as integer against `MatchMetadata.Episode` (episode-of-season from TVDB). For daily shows, Sonarr encodes the airdate as `season=YYYY&ep=MMDD`. The comparison `217 == 1108` always fails. `SearchResultItem.AiredAt` (DateTimeOffset?) is available at filter time but unused.

2. **History trim safety (#7)**: `DownloadHistoryManagerState.TrimIfNeeded` (line 64-74) trims oldest-first when count exceeds `MaxHistoryRecords` (default 1000). `DownloadCompletion.CompletedAt` (long, epoch) exists on every record but is not considered during trimming. If Arr is down during a burst of completions, records can be trimmed before Arr polls them.

## Goals / Non-Goals

**Goals:**
- Daily show searches return results when Sonarr uses `season=YYYY&ep=MMDD` convention
- Recent download history entries survive long enough for Arr to poll them

**Non-Goals:**
- Adding airdate to `MatchMetadata` (not needed — `SearchResultItem.AiredAt` is already available)
- Import-tracking state for history (too complex for the actual risk level)
- Changing persistence events or message contracts

## Decisions

### 1. Daily show detection: `season >= 1900`

Sonarr sends `season=2024` for daily shows. Real TV season numbers never exceed ~50. Threshold of 1900 cleanly separates the two cases.

**Alternative**: Check if the show is flagged as "daily" in metadata — not available, would require enrichment changes.

### 2. MMDD matching against `SearchResultItem.AiredAt`

When daily-show mode is detected, parse `ep` as MMDD (month * 100 + day), construct a date from `season` (year) + MMDD, and compare against `item.AiredAt?.Date`. This stays entirely within `FilterBySeasonEpisode` — no plumbing changes.

**Alternative**: Add MMDD to `MatchMetadata.Episode` in enrichment — requires cross-domain changes, over-engineered for this.

### 3. Age-based trim floor with `TimeProvider`

`TrimIfNeeded` receives `TimeProvider` (already used project-wide) and skips records where `CompletedAt` is within the last 30 minutes. Count-based ceiling remains. The 30-minute floor covers Arr polling intervals with generous margin (Arr polls every ~60s, but may be temporarily down).

**Alternative**: Configurable floor via `DownloadOptions` — not worth it at v0.x, hardcode as constant.

### 4. No persistence event changes

`HistoryTrimmed(int Count)` stays as-is. The age floor only changes *which* records are eligible for trimming, not the trim event itself. If all records are younger than 30 minutes, trim count is 0 and no event is persisted.

## Risks / Trade-offs

- **Daily show without AiredAt**: If `SearchResultItem.AiredAt` is null, the daily-show filter branch cannot match. Mitigation: fall through to no-match (same as today — no regression).
- **History grows beyond 1000 during burst**: If many downloads complete within 30 minutes, the list temporarily exceeds `MaxHistoryRecords`. Mitigation: acceptable — bounded by download throughput (FFmpeg remux speed), unlikely to exceed 1000+ in 30 minutes.
- **Leap year / invalid MMDD**: `ep=0230` (Feb 30) won't match any real airdate. Mitigation: this is correct behavior — no episode aired on that date.
