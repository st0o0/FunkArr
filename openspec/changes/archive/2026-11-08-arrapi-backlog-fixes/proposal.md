## Why

Two known ArrApi/Download backlog items cause incorrect behavior: daily show searches never match because episode filtering compares MMDD dates against episode-of-season numbers, and download history trimming can discard entries before Sonarr/Radarr imports them during burst completions or Arr downtime.

## What Changes

- Fix `FilterBySeasonEpisode` to detect daily show requests (`season >= 1900`) and match `ep` as MMDD against `SearchResultItem.AiredAt` instead of `MatchMetadata.Episode`
- Add an age-based floor to `DownloadHistoryManagerState.TrimIfNeeded` — never trim records younger than 30 minutes, regardless of count

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `newznab-daily-episode`: Change from "known limitation" to working daily show episode matching using AiredAt comparison
- `download-history-trimming`: Add age-based trim protection so recent completions survive the Arr polling window

## Impact

- **ArrApi**: `NewznabSearchService.FilterBySeasonEpisode` — new daily-show branch
- **Download**: `DownloadHistoryManagerState.TrimIfNeeded` — age floor parameter, needs `TimeProvider` or timestamp comparison
- **Tests**: Both areas need test coverage for the new behavior
- **No API changes, no persistence changes, no message changes**
