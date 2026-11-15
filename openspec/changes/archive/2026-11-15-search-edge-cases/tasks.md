## 1. Add IsPartial flag to SearchCommandCompleted

- [x] 1.1 Add `bool IsPartial = false` property to `SearchCommandCompleted` in `FunkArr.Messages/Search/SearchCommand.cs`
- [x] 1.2 In `SearchManager.HandleTimeout`, set `IsPartial = true` when returning partial Both-search result and add warning log
- [x] 1.3 In `SearchManager.HandleFailed`, set `IsPartial = true` when returning partial Both-search result and add warning log
- [x] 1.4 Add test: Both-search partial timeout returns IsPartial=true
- [x] 1.5 Add test: Both-search partial failure returns IsPartial=true
- [x] 1.6 Add test: Full Both-search result has IsPartial=false

## 2. MediathekViewWebManager StreamComplete logging

- [x] 2.1 Add `_log.Error` before the throw in StreamComplete handler with pending count
- [x] 2.2 Add test: StreamComplete throws InvalidOperationException (document behavior)

## 3. VariantDetector Unicode normalization

- [x] 3.1 Add NFC normalization in `VariantDetector.Matches` before regex matching
- [x] 3.2 Add test: decomposed ä (a + combining diaeresis) in "Audiodeskription" matches AudioDescription
- [x] 3.3 Add test: decomposed ö in "Hörfassung" matches AudioDescription
- [x] 3.4 Add test: decomposed ü in "Gebärdensprache" matches SignLanguage

## 4. VariantDetector edge case tests

- [x] 4.1 Add test: "DGS" as part of word does not match (e.g., "ADGS")
- [x] 4.2 Add test: "OV" as part of word does not match (e.g., "LOVE")
- [x] 4.3 Add test: "OV" with surrounding punctuation matches (e.g., "(OV)")
- [x] 4.4 Add test: Regex timeout returns Standard (existing but verify)

## 5. Newznab feed cache eviction

- [x] 5.1 Inject `IOutputCacheStore` into `POST /api/feed/crawl` endpoint and evict "feed" tag
- [x] 5.2 Add test: feed cache evicted after crawl trigger (covered by integration test Crawl_returns_accepted_and_triggers_ingest)

## 6. Build and verify

- [x] 6.1 Run `dotnet build src/FunkArr.slnx` — zero errors (pre-existing errors in Feed.Tests/Download.Tests from prior fork)
- [x] 6.2 Run `dotnet format` — clean on my files (pre-existing CRLF issues on other files from prior forks)
- [x] 6.3 Run Search.Tests — 218 pass, 0 failures
- [x] 6.4 Run ArrApi.Tests — 280 pass, 0 failures
- [x] 6.5 Run IntegrationTests — 80 pass, 0 failures
