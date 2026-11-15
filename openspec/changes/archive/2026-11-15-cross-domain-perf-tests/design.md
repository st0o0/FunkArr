# Design

## E.3 Cross-Domain Consistency

Integration tests using FunkArrFixture with TestProbes. Each test sends HTTP
requests to multiple API endpoints and verifies the returned counts are
internally consistent.

- E.3.4: Both `/api/downloads/queue` and `/download/api?mode=queue` receive the
  same QueueResult from the DownloadManager probe. Verify both return the same
  item count.

## H.1 Queue Performance

State-level tests using DownloadManagerState directly (no actor needed for
priority and ordering). Actor-level tests for ForceStart using TestKit.

- H.1.2: Enqueue 50 items with mixed priorities, verify ordering.
- H.1.3: ForceStart when at capacity — verify dispatched count exceeds
  MaxConcurrent.
- H.1.4: Rapid enqueue/dequeue 100 items — verify no state corruption.
