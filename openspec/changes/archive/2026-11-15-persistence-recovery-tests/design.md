# Design: Persistence Recovery Tests

## Approach

Use `Akka.TestKit.Xunit.TestKit` which includes an in-memory journal and snapshot
store by default. This is the same base class used by all existing FunkArr actor tests.

### Recovery test pattern

1. Create actor with known PersistenceId
2. Send commands that trigger Persist calls (building up journal)
3. Stop the actor: `Watch(actor); Sys.Stop(actor); ExpectTerminated(actor)`
4. Recreate the actor with the same PersistenceId
5. Query state and verify it matches pre-restart state

### Snapshot recovery pattern

For actors with SnapshotInterval (DownloadManager=25, DownloadHistoryManager=25,
FeedCrawlManager=20, FeedStoreManager=50, HistoryWorker=configurable):

1. Persist enough events to trigger a snapshot
2. Stop and recreate
3. Verify state is correct (restored from snapshot + any post-snapshot events)

### Actors tested

| Actor | PersistenceId | Snapshot | Key state to verify |
|-------|---------------|----------|---------------------|
| DownloadManager | "download-manager" | 25 | Queue, Dispatched, Paused |
| DownloadWorker | "download-{id}" | none | Media, Status, Phase, Attempt |
| DownloadHistoryManager | "download-history" | 25 | Records, Stats |
| FeedCrawlManager | "feed-crawl" | 20 | Cursor, ConsecutiveFailures, LastOutcome |
| FeedStoreManager | "feed-store" | 50 | Entries count, query results |
| HistoryWorker | "history-{id}" | configurable | Snapshots, Stats, Trim on replay |

### Test file placement

Each test file goes in the existing test project for its domain:
- `FunkArr.Download.Tests/Manager/DownloadManagerRecoveryTests.cs`
- `FunkArr.Download.Tests/Worker/DownloadWorkerRecoveryTests.cs`
- `FunkArr.Download.Tests/History/DownloadHistoryManagerRecoveryTests.cs`
- `FunkArr.Feed.Tests/Crawl/FeedCrawlManagerRecoveryTests.cs`
- `FunkArr.Feed.Tests/Store/FeedStoreManagerRecoveryTests.cs`
- `FunkArr.History.Tests/HistoryWorkerRecoveryTests.cs`
