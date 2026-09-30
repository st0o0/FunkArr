## 1. Newznab XML Models

- [x] 1.1 Create `Indexer/Newznab/` folder with XML serialization models: `NewznabRssFeed`, `NewznabRssChannel`, `NewznabRssItem`, `NewznabRssGuid`, `NewznabEnclosure`, `NewznabResponse`, `NewznabAttribute` (sealed classes with `[Xml*]` attributes and `{ get; set; }`)
- [x] 1.2 Create `Indexer/NewznabSerializer.cs` — static class with cached `XmlSerializer` instance, `Serialize(NewznabRssFeed)` method producing UTF-8 indented XML, and `SerializeCaps`/`SerializeError` helpers
- [x] 1.3 Add unit tests for `NewznabSerializer` verifying RSS structure, namespace declarations, empty feed, item serialization, and caps/error responses

## 2. Release Title Builder

- [x] 2.1 Create `Indexer/ReleaseTitleBuilder.cs` — static class with `BuildStandard`, `BuildDaily`, `BuildMovie`, `BuildFallback` methods and `Sanitize` helper (umlaut transliteration, special char removal, dot-collapse, 40-char episode name limit)
- [x] 2.2 Add unit tests for all `ReleaseTitleBuilder` variants: standard with/without episode name, daily with/without episode name, movie, fallback (topic+title+date, title=topic, empty title), sanitization (umlauts, special chars, consecutive dots, length truncation)

## 3. SearchResult Enrichment

- [x] 3.1 Add optional properties to `SearchResult`: `int? ResolvedSeason`, `int? ResolvedEpisode`, `string? EpisodeName`, `string? AirDate`, `string? ResolvedShowName`
- [x] 3.2 Change `MatchedItemInfo` in `Search/SearchMessages.cs` from `(MediathekResultItem Item, string MatchedTitle)` to `(MediathekResultItem Item, MatchedEpisodeInfo? EpisodeInfo)`

## 4. Pipeline Threading

- [x] 4.1 Update `ShowActor.HandleMatch` to pass full `MatchedEpisodeInfo` in `MatchedItemInfo` instead of just the episode name string
- [x] 4.2 Update `SearchRequestActor.HandleMatchComplete` to propagate `MatchedEpisodeInfo` fields (season, episode, episodeName, airDate, showName) into `SearchResult` when creating results from `MatchedItemInfo`
- [x] 4.3 Update `QualityExpander` to preserve `ResolvedSeason`, `ResolvedEpisode`, `EpisodeName`, `AirDate`, `ResolvedShowName` when creating quality variant `SearchResult` copies
- [x] 4.4 Update `QualityProbeService` to preserve episode metadata fields when creating `SearchResult` instances

## 5. Controller Refactoring

- [x] 5.1 Replace `NewznabController.ToNewznabResult` with a `NewznabResultMapper.ToRssItem` static method that maps `SearchResult` directly to `NewznabRssItem` using `ReleaseTitleBuilder` (prefer `SearchResult.ResolvedSeason/Episode` over HTTP params, select standard/daily/fallback variant)
- [x] 5.2 Update `HandleTvSearch`, `HandleMovieSearch`, `HandleTextSearch` to use `NewznabSerializer.Serialize(feed)` instead of `NewznabXmlBuilder.BuildSearchResponse`
- [x] 5.3 Update caps and error endpoints to use `NewznabSerializer.SerializeCaps`/`SerializeError`
- [x] 5.4 Delete `Shared/Models/NewznabResult.cs` and `Indexer/NewznabXmlBuilder.cs`

## 6. Tests and Contracts

- [x] 6.1 Update `NewznabContractSpec` verified files (`.verified.txt`) to match new serialization output format
- [x] 6.2 Update `RuleSetMatchingEngineTests` to verify `MatchedItemInfo` carries full `MatchedEpisodeInfo`
- [x] 6.3 Add integration-style test verifying end-to-end: TV search result with resolved episode data produces enriched release title in Newznab XML
- [x] 6.4 Verify build passes (`dotnet build`) and all tests pass (`dotnet run --project FunkArr.Tests`)
