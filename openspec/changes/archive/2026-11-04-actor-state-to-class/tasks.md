## 1. Download Domain

- [x] 1.1 Convert DownloadManagerState from record to sealed class with void Apply methods, mutable collections, and move `_maxConcurrent` from DownloadManager into state
- [x] 1.2 Convert DownloadWorkerState from record to sealed class with void Apply methods
- [x] 1.3 Convert DownloadHistoryManagerState from record to sealed class with void Apply methods, mutable collections
- [x] 1.4 Update DownloadManager, DownloadWorker, DownloadHistoryManager actors to use void Apply pattern (remove `_state = _state.Apply()` reassignment)
- [x] 1.5 Update Download.Tests to use new state pattern (property assignment instead of with-expressions)

## 2. Search Domain

- [x] 2.1 Convert SearchManagerState from record to sealed class with void Apply methods
- [x] 2.2 Normalize MovieSearchWorkerState: rename `ApplyRuleSet` to `Apply(RuleSetResolved)`
- [x] 2.3 Create MediathekViewWebManagerState class, move `_pending` dictionary from MediathekViewWebManager into state
- [x] 2.4 Update SearchManager and MediathekViewWebManager actors to use void Apply pattern
- [x] 2.5 Update Search.Tests to use new state pattern

## 3. RuleSet Domain

- [x] 3.1 Convert RuleSetManagerState from record to sealed class, add Apply methods (replace inline `with{}` in actor), move `_summaries` from RuleSetManager into state
- [x] 3.2 Convert RuleSetResolverState from record to sealed class with void Apply methods, mutable collections
- [x] 3.3 Create RuleSetWorkerState class, move `_merged` and `_paths` from RuleSetWorker into state
- [x] 3.4 Update RuleSetManager, RuleSetResolver, RuleSetWorker actors
- [x] 3.5 Update RuleSet.Tests to use new state pattern

## 4. Scoring Domain

- [x] 4.1 Convert ScoringManagerState from record to sealed class with void Apply methods

## 5. History Domain

- [x] 5.1 Convert HistoryState from record to sealed class with void Apply/ProcessCommand instance methods, mutable collections
- [x] 5.2 Convert StatsCollectorState from record to sealed class with void Apply methods
- [x] 5.3 Update HistoryWorker, StatsCollector actors
- [x] 5.4 Update History.Tests to use new state pattern

## 6. Feed Domain

- [x] 6.1 Convert FeedCrawlManagerState from record to sealed class with void Apply methods, move `_running` and `_resultReceived` from FeedCrawlManager into state
- [x] 6.2 Convert FeedStoreManagerState from record to sealed class with void Apply methods, mutable collections
- [x] 6.3 Convert FeedCrawlWorkerState from record to sealed class, rename all prefixed Apply methods to overloaded `Apply`, fix mutable-list-in-record bug
- [x] 6.4 Convert FeedSchedulerState from record to sealed class with void Apply methods
- [x] 6.5 Update FeedCrawlManager, FeedStoreManager, FeedCrawlWorker, FeedScheduler actors
- [x] 6.6 Update Feed.Tests to use new state pattern

## 7. Documentation

- [x] 7.1 Update `actor-state-management` main spec via delta sync
- [x] 7.2 Update CLAUDE.md actor state pattern section (record → class, extension methods → instance methods, immutable → mutable collections)
- [x] 7.3 Update AGENTS.md if it references record-based state pattern
