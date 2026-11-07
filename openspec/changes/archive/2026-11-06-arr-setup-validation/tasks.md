## 1. Red tests first

- [x] 1.1 In `FunkArr.Feed.Tests`, extend `FeedStoreManagerStateTests` so a media-type filter with no matches asserts `Total` equals the whole store size and `FilteredTotal` is 0 (already covered by `Query_for_category_without_entries_is_empty` and `Query_keeps_order_and_category_totals_after_expiry`, no change needed)
- [x] 1.2 In `FunkArr.IntegrationTests/Arr/NewznabSearchTests.cs`, add a Theory over feed state (empty, TV only, movies only, both) and probe (`tvsearch`, `movie`, `search` with TV categories, `search` with movie categories); the feed probe replies `FeedResult(entries, total, filteredTotal)` with realistic values
- [x] 1.3 In `FunkArr.ArrApi.Tests` (`NewznabFeedProbeTests`, TestKit), cover `Search` directly: other-type feed answers with a cacheable validation item, `total` equals filtered total, offset beyond the last entry returns an empty page without validation item (the type matrix itself lives in 1.2)
- [x] 1.4 Run the new tests and confirm the "feed has other media type" cases fail for the expected reason

## 2. Fix probe answers

- [x] 2.1 Branch on the filtered result in `FeedOrValidation` and report `FilteredTotal` as `total` in `ToFeedRss`
- [x] 2.2 Keep `Cacheable` and the feed-unavailable fallback behavior unchanged; add a test for "feed answered, nothing matches"
- [x] 2.3 Run the new tests; all green

## 3. Verify

- [x] 3.1 `dotnet build src/FunkArr.slnx` with 0 warnings and `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 3.2 Run `FunkArr.Feed.Tests`, `FunkArr.ArrApi.Tests`, `FunkArr.IntegrationTests` and `FunkArr.Architecture.Tests`
- [x] 3.3 Repeat the manual verification from design.md against Radarr 5.21, 6.4 and 7.0 plus one version each of Sonarr and Prowlarr; Radarr must now pass
