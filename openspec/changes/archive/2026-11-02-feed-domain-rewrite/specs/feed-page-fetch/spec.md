## ADDED Requirements

### Requirement: FeedPageActor fetches one page via MvwManager
FeedPageActor SHALL be a `ReceiveActor` used as a pool routee. It SHALL receive a `FetchPage` message containing offset and CrawlWindow, Ask MvwManager with a `QueryMediathek` request, apply window filtering to the results, and reply to the original sender with a `PageResult`.

#### Scenario: Successful page fetch within window
- **WHEN** FeedPageActor receives FetchPage(offset=500, window=[1000, 2000])
- **THEN** it SHALL Ask MvwManager for page at offset 500, filter items to those with filmlisteTimestamp in (window.LowerBound, window.UpperBound], and reply with PageResult containing the filtered items

#### Scenario: Page reaches end of window
- **WHEN** a page contains an item with filmlisteTimestamp <= window.LowerBound
- **THEN** the PageResult SHALL have ReachedEnd=true and SHALL include only items within the window

#### Scenario: Incomplete page signals end
- **WHEN** MvwManager returns fewer items than the page size
- **THEN** the PageResult SHALL have ReachedEnd=true

#### Scenario: MvwManager error
- **WHEN** the Ask to MvwManager fails or returns QueryMediathekError
- **THEN** FeedPageActor SHALL reply with PageResult containing Error set and ReachedEnd=true

#### Scenario: MvwManager queue full
- **WHEN** MvwManager returns QueryMediathekQueueFull
- **THEN** FeedPageActor SHALL reply with PageResult containing Error="queue full" and ReachedEnd=true

### Requirement: FeedPageActor uses PipeTo with failure handler
FeedPageActor SHALL use `Ask<QueryMediathekResponse>().PipeTo(Self, success:, failure:)` to handle the MvwManager response. The failure handler SHALL produce an error PageResult, not propagate the exception.

#### Scenario: Ask timeout
- **WHEN** the Ask to MvwManager times out
- **THEN** the PipeTo failure handler SHALL produce a PageResult with Error set, not crash the actor

### Requirement: Sliding window page fetching in worker
FeedCrawlWorker's Harvesting phase SHALL use a sliding window pattern: it SHALL fire N initial FetchPage messages to the PageFetchPool (where N = pool size), then for each PageResult received, fire the next FetchPage if ReachedEnd has not been reached. The worker SHALL track pending request count and collected items in its state.

#### Scenario: Initial burst of page requests
- **WHEN** the worker enters the Harvesting phase with PagePrefetch=3
- **THEN** it SHALL immediately send FetchPage for offsets 0, pageSize, and 2*pageSize to the PageFetchPool

#### Scenario: Sliding window continues
- **WHEN** the worker receives a PageResult with ReachedEnd=false and there are more pages to fetch
- **THEN** it SHALL send the next FetchPage to the PageFetchPool and decrement/increment pending counts

#### Scenario: All pages collected
- **WHEN** all pending page requests have returned and ReachedEnd has been observed
- **THEN** the worker SHALL Become(Evaluating) with all collected items in state

#### Scenario: Page error terminates harvesting
- **WHEN** any PageResult has an Error set
- **THEN** the worker SHALL stop sending new FetchPage requests, wait for pending requests to return, record the error, and proceed to complete with a failed CrawlResult

### Requirement: FetchPage and PageResult are internal messages
FetchPage and PageResult SHALL be internal sealed records within the Feed project. FetchPage SHALL carry: Offset (int), Window (CrawlWindow), PageSize (int). PageResult SHALL carry: Offset (int), Items (MediathekItem[]), ReachedEnd (bool), Error (string?).

#### Scenario: Message shape
- **WHEN** constructing a FetchPage
- **THEN** it SHALL require Offset, Window, and PageSize as constructor parameters
