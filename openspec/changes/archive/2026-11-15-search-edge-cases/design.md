# Design

## Partial search results

Add `bool IsPartial = false` to `SearchCommandCompleted`. SearchManager sets it to `true` when returning a partial Both-search result (one shard failed or timed out). The Newznab search service can log this but returns results normally — partial is better than nothing. The `IsPartial` default is `false` so existing consumers are unaffected.

## StreamComplete logging

The crash on StreamComplete is intentional — it triggers supervisor restart. Add a `_log.Error` before the throw to make the orphaned pending count visible in logs. No behavioral change.

## Unicode normalization

Call `value.Normalize(NormalizationForm.FormC)` in `VariantDetector.Matches` before regex matching. NFC is the standard form for precomposed characters. This ensures decomposed ä/ö/ü match the precomposed literals in regex patterns.

## OV/DGS patterns

The existing `\bOV\b` and `\bDGS\b` patterns use word boundaries, which correctly prevent matching inside words (e.g., "OVB", "ADGS"). Existing test "OVB Nachrichten" already confirms this for OV. Add tests for DGS-in-word and OV-in-word edge cases.

## Feed cache eviction

The Newznab OutputCache uses tag "feed". Currently only the "rulesets" tag is evicted (in RuleSetApiEndpoints). Two eviction points needed:

1. **FeedApiEndpoints.MapPost("/crawl")**: After a crawl is triggered, but this fires before entries arrive. Not ideal.
2. **Better: evict in the FeedStoreManager response path.** When `PublishFeedEntriesCompleted` reports `Published > 0`, evict. But FeedStoreManager is an actor — no access to IOutputCacheStore.

The practical approach: Add an `IFeedCacheEviction` service that `FeedApiEndpoints` uses. Create a polling or event-based mechanism. Simplest: on every feed query (`GET /api/feed`), check if entries changed. But this doesn't help Newznab.

**Chosen approach**: Inject `IOutputCacheStore` into the `/crawl` endpoint. After the Tell, wait for crawl completion by listening for the next status change (too complex). Simpler: use a `FeedCacheInvalidator` hosted service that subscribes to feed publish events. Even simpler: **evict the "feed" tag in the `/api/feed` GET endpoint when the response shows new entries**. But stale Newznab cache is the problem.

**Final approach**: The cleanest solution is to evict in the `POST /api/feed/crawl` flow. But crawl is async (Tell, not Ask). The real place is where Newznab searches happen — in `NewznabSearchService`. But that's downstream.

**Actual simplest**: Add a middleware/filter or post-processing in the FeedStoreManager that publishes to Akka EventStream when entries are added. Then a hosted service subscribes and evicts. OR: just evict on every crawl trigger — the 5-minute cache is acceptable for non-crawl scenarios, but after a manual crawl trigger, the user expects fresh results. So: evict "feed" tag in the `POST /api/feed/crawl` handler. And additionally, add a periodic eviction mechanism.

**Decision**: Evict "feed" tag in `POST /api/feed/crawl` handler (immediate, simple). For automatic crawls, add an `IFeedEntryNotifier` that the FeedStoreManager's publish path calls, which evicts the cache. Implement via `IOutputCacheStore` passed through DI to a service the actor can resolve.
