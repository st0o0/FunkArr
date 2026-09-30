## 1. Messages & Core Types

- [x] 1.1 Add `MetadataSpec` record to `FunkArr.Messages.Scoring` with Season (string?), Episode (string?), AiredAt (DateTimeOffset?)
- [x] 1.2 Extend `ScoredItem` with `Metadata` (MetadataSpec?) parameter
- [x] 1.3 Add `ReleaseTitleBuilder` static class to `FunkArr.Core` with `Build(topic, title, metadata, quality, category)` method — umlaut normalization, special char removal, dot formatting, quality mapping, S/E formatting
- [x] 1.4 Add `ReleaseTitleBuilderTests` in a suitable test project covering all scenario variants (tv with S/E, tv with airdate, tv without metadata, movie with year, movie without year, umlauts, special chars, padding, absolute episode)

## 2. MatchMagic — Surface MetadataSpec

- [x] 2.1 Update MatchMagicActor evaluation to populate MetadataSpec from identification results (SeasonAndEpisodeNumber → Season+Episode, AirdateExtraction → AiredAt, ByAbsoluteEpisodeNumber → Episode, ItemTitleExact/Includes → AiredAt from Timestamp)
- [x] 2.2 Update existing MatchMagic tests for ScoredItem shape change (add Metadata parameter)

## 3. Search — Build Scene Titles

- [x] 3.1 Update `TvSearchWorkerState.ToResultItem` and `ToScoredResult` to use `ReleaseTitleBuilder.Build` for the Title field, passing MetadataSpec from ScoredItem
- [x] 3.2 Update `MovieSearchWorkerState.ToResultItem` and `ToScoredResult` analogously
- [x] 3.3 Update TvSearchWorker and MovieSearchWorker tests for scene-style titles

## 4. NZB — Add Category Meta

- [x] 4.1 Add `X-FunkArr-Category` meta to NZB generation in `Nzb.cs` / NZB builder
- [x] 4.2 Update NZB parser to extract Category from `X-FunkArr-Category` meta
- [x] 4.3 Update NZB round-trip and generator tests

## 5. Download — Use Scene Title for Filename

- [x] 5.1 Update `DownloadManager.HandleAdd` to use `cmd.Title + ".mkv"` for OutputPath instead of `SanitizeFilename(cmd.Title) + ".mkv"`, remove `SanitizeFilename` method
- [x] 5.2 Update DownloadManager and DownloadWorker tests for scene-style filenames

## 6. Newznab — Wire Category Through

- [x] 6.1 Update `SearchHandler.ToRss` NZB payload encoding to include category so it's available when the SABnzbd endpoint parses the NZB
- [x] 6.2 Update SABnzbd `DownloadApiEndpoints` to pass parsed Category to AddDownload
- [x] 6.3 Update ArrApi tests for category in NZB payload
