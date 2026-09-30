## 1. Enum type safety

- [x] 1.1 Create PersistedDownloadStatus enum in FunkArr.Persistence
- [x] 1.2 Change HistoryRecorded.Status from int to PersistedDownloadStatus
- [x] 1.3 Move WorkerStatus enum to Messages, change WorkerStatusResult.Status to WorkerStatus
- [x] 1.4 Add switch mappings: DownloadStatus <-> PersistedDownloadStatus
- [x] 1.5 Replace DownloadPriority int-casts with switch mappings
- [x] 1.6 Update DownloadHistoryManager to use .ToPersistence()
- [x] 1.7 Update DownloadHistoryManagerState to use .ToDomain()/.ToPersistence()
- [x] 1.8 Update DownloadWorker.HandleQueryStatus to pass WorkerStatus directly
- [x] 1.9 Update DownloadManager.HandleQueryQueue to compare WorkerStatus enum
- [x] 1.10 Update test files (DownloadWorkerStateTests, DownloadWorkerRetryStateTests, DownloadHistoryManagerStateTests, DownloadEventVerifyTests)

## 2. Category in DownloadManagerState

- [x] 2.1 Extend QueueEntry with MediaType Category field
- [x] 2.2 Create DispatchedEntry record, change Dispatched dict value type
- [x] 2.3 Extend DownloadEnqueued persistence event with optional PersistedMediaType? Category
- [x] 2.4 Update DownloadManagerState.Apply methods for new QueueEntry/DispatchedEntry
- [x] 2.5 Update HandleAdd to pass Category to DownloadEnqueued event
- [x] 2.6 Recovery: old DownloadEnqueued without Category defaults to MediaType.Show
- [x] 2.7 Update PersistedDownloadManagerState DTOs with Category fields
- [x] 2.8 Update DownloadManagerStateTests for new types

## 3. Pre-pagination + Akka.Streams

- [x] 3.1 Add GetPage(QueryQueue) extension method to DownloadManagerState
- [x] 3.2 Rewrite HandleQueryQueue with GetPage for ID-level pagination
- [x] 3.3 Replace Task.WhenAll with Source.Ask pipeline (parallelism 8, ResumingDecider)
- [x] 3.4 Remove AskWorkerStatus helper method
- [x] 3.5 GetPage tests added (filter, pagination, empty queue, dispatched-first ordering)

## 4. Verification

- [x] 4.1 dotnet build - 0 errors
- [x] 4.2 dotnet format - no new violations
- [x] 4.3 All tests pass: 799 total, 0 failed
