## Why

FeedCrawlManager is a monolith — all crawl logic (probe, paging, filtering, ruleset matching, evaluation dispatch, gating, publishing, result aggregation) lives in one ~250-line inline Akka.Streams graph with seven mutable closure-captured variables. Every other domain (Download, Search, RuleSet) separates orchestration from work: managers are thin, workers/services do the processing, state flows cleanly. Feed needs the same structural maturity.

## What Changes

- **Extract crawl execution into a `FeedCrawler` child actor.** FeedCrawlManager becomes thin orchestration (cursor persistence, timer scheduling, status queries). FeedCrawler handles one crawl run's lifecycle using Become-based phases (Probing → Crawling → done), with a simplified stream for the batch processing pipeline.
- **Eliminate mutable closure state in the stream.** The probe/setup phase (list timestamp check, ruleset fetch, window computation) runs as actor message exchanges before the stream materializes. The stream receives all context as immutable input — no `null?` closures.
- **Give Feed its own domain model.** Replace `FeedEntry`'s wrapping of `SearchResultItem` with owned fields. Remove the Feed→Search message type dependency.
- **Consolidate the internal type proliferation.** The nine internal records in `CrawlResult.cs` (CrawlInput, CrawlWindow, PageEvaluation, FeedMatch, EvalResult, GatedResult, CrawlBatchResult, CrawlAccumulator, CrawlResult) get reorganized — some move into FeedCrawler's state, some become the stream's stage types, some collapse.
- **Simplified stream graph.** With probe/setup extracted, the stream reduces to: pages → filter → dedup → match → evaluate → gate → publish → aggregate. No probe stage, no ruleset fetch stage, no closure setup.

## Capabilities

### New Capabilities

- `feed-crawl-lifecycle`: Defines the FeedCrawler child actor — one crawl run's lifecycle, Become-based phases (Probing → Crawling), probe/setup as actor messages, stream materialization with immutable input, CrawlResult reporting to parent.
- `feed-domain-model`: Feed's own entry model with owned fields instead of wrapping SearchResultItem. Covers FeedEntry, the persistence mapping, and the FeedGate output type.

### Modified Capabilities

- `crawl-stream-pure`: Stream pipeline simplification — probe and ruleset loading move out of the stream into actor phases. The stream receives pre-computed CrawlWindow + RuleSetLookup as immutable input. Counting-in-closures and errors-as-data requirements stay, ConcatMany requirement may change since the probe is no longer a stream stage.

## Impact

- **FunkArr.Feed**: FeedCrawlManager shrinks to ~80 lines (cursor + timer + spawn crawler). New FeedCrawler actor (~150 lines). New FeedCrawlerState record. Stream graph rebuilds from ~250 to ~80 lines.
- **FunkArr.Messages/Feed**: FeedEntry record changes (owned fields, drop SearchResultItem dependency). **BREAKING** for FeedEntry consumers — FunkArr.Api feed endpoint adapts.
- **FunkArr.Persistence/Events/Feed**: PersistedFeedEntry stays (already has flat fields). PersistenceMapping simplifies since FeedEntry now matches persistence shape directly.
- **FunkArr.Feed.Tests**: FeedCrawlManager tests simplify. New FeedCrawler tests (probe phases, stream pipeline). FeedGate tests adapt to new FeedEntry construction.
- **FunkArr.Api**: Feed query endpoint adapts to new FeedEntry shape (field access changes from `entry.Item.Title` to `entry.Title`).
- **No persistence event schema changes.** No migration needed.
