## Why

Creating the FunkArr indexer in Radarr fails as soon as the feed holds entries of only one media type. The indexer's empty-criteria RSS probe is answered from the feed store, and FunkArr decides "feed has entries" from the unfiltered total. A feed with TV entries and no movies therefore answers Radarr's movie probe with an empty result instead of the validation item, and Radarr rejects the indexer ("Query successful, but no results in the configured categories were returned"). Verified against real Radarr 5.21, 6.4 and 7.0 (all fail) while Sonarr 4/5 and Prowlarr 1/2/3 pass with the same FunkArr. Once the feed crawl has run this is the normal state, and no existing test covers it.

## What Changes

- The feed-vs-validation decision for empty-criteria Newznab requests uses the entries that match the requested media type, not the total across all media types.
- The `total` reported in the RSS response is the filtered total, so it matches the items returned.
- New regression tests cover the matrix of feed state (empty, TV only, movies only, both) against probe type (`tvsearch`, `movie`, `search` with categories), in the existing hermetic test projects.

## Capabilities

### New Capabilities

None.

### Modified Capabilities
- `newznab-indexer-api`: Empty-criteria requests answer with entries of the requested media type or the validation item; reported total matches the returned items.
- `integration-test-arr`: Contract tests cover feed states where entries exist for only some media types.

## Impact

- `FunkArr.ArrApi/Newznab/Services/NewznabSearchService.cs` (`FeedOrValidation`)
- Tests in `FunkArr.ArrApi.Tests`, `FunkArr.IntegrationTests` and one contract assertion in `FunkArr.Feed.Tests`
- No change to `FeedResult`, the feed API or the UI
