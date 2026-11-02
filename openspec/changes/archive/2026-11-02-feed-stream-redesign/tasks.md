## 1. Feed Domain Model

- [x] 1.1 Replace FeedEntry record with flat fields (StableId, FirstSeenAt, MediaType, Title, Channel, Topic, Url, Duration, Size, Quality, AiredAt, Score, SubtitleUrl, Ids, Season, Episode) — remove SearchResultItem wrapper
- [x] 1.2 Update FeedGate.Evaluate to construct flat FeedEntry from SearchResultItem + MediathekItem fields
- [x] 1.3 Simplify PersistenceMapping (ToPersistence/ToDomain) to direct field-to-field copy
- [x] 1.4 Update FeedStoreManagerState and tests for new FeedEntry shape
- [x] 1.5 Update FeedApiEndpoints to read flat fields (entry.Title instead of entry.Item.Title)
- [x] 1.6 Update NewznabSearchService.ToFeedRss to pass FeedEntry fields directly to ToItem
- [x] 1.7 Update FeedGateTests and FeedStoreManagerTests for new FeedEntry construction
- [x] 1.8 Verify build and run all Feed tests

## 2. FeedCrawler Actor and State

- [x] 2.1 Create FeedCrawlerState record (CrawlInput, ListTimestamp, CrawlWindow, RuleSetLookup — set progressively through phases)
- [x] 2.2 Create FeedCrawler actor with Probing phase: Ask MediathekManager for list timestamp, handle response via PipeTo
- [x] 2.3 Add probe result handling: unchanged → report ForUnchanged and stop, success → Ask RuleSetResolver for rulesets
- [x] 2.4 Add ruleset response handling: build RuleSetLookup, compute CrawlWindow, Become(Crawling)
- [x] 2.5 Add probe/ruleset failure and timeout handling → report ForFailed and stop

## 3. Simplified Stream Pipeline

- [x] 3.1 Build stream in FeedCrawler.Crawling phase: Source.From(pageOffsets) with immutable CrawlWindow + RuleSetLookup as captured locals
- [x] 3.2 Port page-fetch SelectAsync stage (MediathekManager Ask, window filtering, reach-end detection)
- [x] 3.3 Port dedup (StatefulSelectMany with HashSet), match (RuleSetLookup.Match), and counting (closure-captured collected/matched)
- [x] 3.4 Port GroupBy → evaluation dispatch (SelectAsyncUnordered to TvEvaluation/MovieEvaluation)
- [x] 3.5 Port gating (EvalResult.Gate with FeedGate), publish batching (GroupedWithin → FeedStoreManager Ask), and aggregation (CrawlAccumulator)
- [x] 3.6 PipeTo Self → forward CrawlResult to parent → Context.Stop(Self)

## 4. FeedCrawlManager Thinning

- [x] 4.1 Replace StartCrawl() body: build CrawlInput from options + state, spawn FeedCrawler child, Context.Watch, set _running
- [x] 4.2 Add Terminated handler: if CrawlResult not yet received, treat as failed crawl
- [x] 4.3 Update HandleResult: stop crawler child, reset _running (keep persistence and telemetry logic as-is)
- [x] 4.4 Remove all stream-related usings and IMaterializer field from FeedCrawlManager

## 5. Type Reorganization

- [x] 5.1 Move crawler-internal types (PageEvaluation, FeedMatch, EvalResult, GatedResult, CrawlBatchResult, CrawlAccumulator) to FeedCrawlerTypes.cs
- [x] 5.2 Keep shared types (CrawlInput, CrawlWindow, CrawlResult, CrawlStatus) in CrawlResult.cs
- [x] 5.3 Add CrawlContext record (or expand CrawlInput) to carry pre-resolved window, lookup, and actor refs

## 6. Tests

- [x] 6.1 Add FeedCrawlerState unit tests (phase transitions, Apply methods)
- [x] 6.2 Add FeedCrawler actor tests: probe unchanged → ForUnchanged, probe failure → ForFailed, successful crawl → ForCompleted
- [x] 6.3 Update FeedCrawlManagerTests: verify spawns child, handles CrawlResult, handles Terminated without result
- [x] 6.4 Verify all existing Feed tests pass with updated types
- [x] 6.5 Run full build and dotnet format
