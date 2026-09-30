## Why

Sonarr/Radarr identify media by parsing release titles in scene-style format (`Show.S01E05.Title.GERMAN.720p.WEB.h264-Group`). Currently FunkArr uses the raw Mediathek title for both Newznab RSS items and download filenames (`Tatort - Der letzte Schrei.mkv`), which Sonarr cannot reliably parse. The scoring system already extracts season/episode/airdate through IdentificationSpec strategies but discards these results — only score and matched flag survive in ScoredItem. Surfacing identification metadata and building scene-style titles makes FunkArr a drop-in indexer+downloader for the *arr stack.

## What Changes

- Add `MetadataSpec` record to Messages carrying identification results (season, episode, aired date) from scoring through search results to downloads.
- Extend `ScoredItem` to include the extracted `MetadataSpec` so identification data flows out of MatchMagic.
- Add a `ReleaseTitleBuilder` pure function in Core that formats scene-style titles from topic, title, MetadataSpec, quality, and category.
- Change `SearchResultItem.Title` to use the scene-style formatted title (built in Search workers after scoring).
- Extend NZB `X-FunkArr-*` meta fields to carry `Category` so the download pipeline receives it.
- Change `DownloadManager` to derive `OutputPath` from the scene-style title already present in `AddDownload.Title`.
- Release group tag: `-FunkArr`.

## Capabilities

### New Capabilities

- `release-title-format`: Scene-style release title construction from metadata (topic, title, season/episode/airdate, quality, category) with umlaut normalization, sanitization, and quality-tier mapping. Defines the `MetadataSpec` record and `ReleaseTitleBuilder` formatter.

### Modified Capabilities

- `matchmagic-evaluation`: ScoredItem gains MetadataSpec carrying extracted season/episode/airdate from identification strategies, so scoring results include identification metadata.
- `search-messages`: SearchResultItem.Title becomes the scene-style formatted title; Topic remains available as a separate field.
- `nzb-object-model`: NZB meta gains `X-FunkArr-Category` field.
- `download-messages`: AddDownload gains Category-aware title; DownloadManager derives OutputPath from the pre-formatted title.

## Impact

- **FunkArr.Messages**: New `MetadataSpec` record, `ScoredItem` extended.
- **FunkArr.Core**: New `ReleaseTitleBuilder` static class.
- **FunkArr.MatchMagic**: Evaluation returns MetadataSpec in ScoredItem.
- **FunkArr.Search**: TvSearchWorker/MovieSearchWorker build scene title after scoring.
- **FunkArr.ArrApi**: Newznab SearchHandler uses scene title as-is; NZB gains Category meta.
- **FunkArr.Download**: DownloadManager uses scene title for OutputPath, removes SanitizeFilename.
- **Persistence**: No changes — DTOs don't store titles.
- **Tests**: New tests for ReleaseTitleBuilder, updated tests for ScoredItem, SearchWorker, NZB generation.
