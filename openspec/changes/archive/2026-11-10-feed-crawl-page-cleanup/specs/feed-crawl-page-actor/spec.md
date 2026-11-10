## MODIFIED Requirements

### Requirement: ProcessPage message shape

ProcessPage SHALL carry flat data only: Offset (int), PageSize (int), LowerBound (long), UpperBound (long), Targets (RuleSetTarget[]), MinConfidence (double). The actor SHALL build a RuleSetLookup internally from the Targets array. These are domain-internal messages in the FunkArr.Feed namespace.

#### Scenario: ProcessPage fields

- **WHEN** ProcessPage is constructed
- **THEN** it SHALL have fields: Offset (int), PageSize (int), LowerBound (long), UpperBound (long), Targets (RuleSetTarget[]), MinConfidence (double)
- **AND** it SHALL NOT carry CrawlWindow or RuleSetLookup objects

#### Scenario: RuleSetLookup built internally

- **WHEN** FeedCrawlPageActor receives ProcessPage with Targets
- **THEN** it SHALL construct a RuleSetLookup from the RuleSetTarget array
- **AND** use it for topic matching during item grouping

### Requirement: PageProcessed message shape

ProcessPage SHALL return an abstract `ProcessPageResponse` with two subtypes. `ProcessPageCompleted` SHALL carry: Accepted (FeedEntry[]), NewItems (int), Unmatched (int), ReachedEnd (bool), RejectedByReason (IReadOnlyDictionary<RejectionReason, int>). `ProcessPageFailed` SHALL carry: Reason (string). These are domain-internal messages.

#### Scenario: Successful page returns ProcessPageCompleted

- **WHEN** a page is processed successfully (even with zero accepted entries)
- **THEN** the actor SHALL reply with ProcessPageCompleted
- **AND** Accepted SHALL contain the FeedEntry array from FeedGate
- **AND** NewItems SHALL be the count of items in the window after dedup
- **AND** Unmatched SHALL be items that matched no ruleset
- **AND** ReachedEnd SHALL be true if MVW returned fewer than pageSize items or items fell below the lower bound
- **AND** RejectedByReason SHALL be a non-nullable dictionary (empty if no rejections)

#### Scenario: Fetch failure returns ProcessPageFailed

- **WHEN** the MediathekManager Ask fails or times out
- **THEN** the actor SHALL reply with ProcessPageFailed containing the failure reason

### Requirement: FeedCrawlPageActor uses Become for concurrency safety

FeedCrawlPageActor SHALL use Become to transition through Ready → Fetching → Evaluating → Ready. While in Fetching or Evaluating, the actor SHALL stash incoming ProcessPage messages via IWithUnboundedStash and unstash them on return to Ready. This ensures no messages are lost when SmallestMailboxPool routes to a busy actor.

#### Scenario: Become transitions

- **WHEN** FeedCrawlPageActor receives ProcessPage in Ready state
- **THEN** it SHALL Become(Fetching) and stash any further ProcessPage messages
- **WHEN** the fetch completes and evaluation starts
- **THEN** it SHALL Become(Evaluating) and continue stashing ProcessPage messages
- **WHEN** all evaluation results are collected
- **THEN** it SHALL reply with ProcessPageResponse, call Stash.UnstashAll(), and Become(Ready)

#### Scenario: Stashed message processed after completion

- **WHEN** a ProcessPage message arrives while the actor is in Fetching state
- **THEN** it SHALL be stashed (not dropped or sent to dead letters)
- **AND** after the current page completes and Become(Ready) is called
- **THEN** the stashed ProcessPage SHALL be processed

#### Scenario: Pool routes around busy routee

- **WHEN** a routee is in Fetching or Evaluating state and has a message in its mailbox (stashed)
- **THEN** SmallestMailboxPool SHALL prefer other routees with smaller mailboxes

## ADDED Requirements

### Requirement: FeedCrawlManager uses SmallestMailboxPool router

FeedCrawlManager SHALL create a permanent SmallestMailboxPool router for FeedCrawlPageActor instances. The router SHALL be created in RecoveryCompleted with the initial MaxConcurrency from FeedOptions. The router SHALL live for the lifetime of the Manager.

#### Scenario: Router creation at startup

- **WHEN** FeedCrawlManager completes recovery
- **THEN** it SHALL create a SmallestMailboxPool router with Props.Create<FeedCrawlPageActor>() and pool size from FeedOptions.MaxConcurrency
- **AND** store the router IActorRef and current pool size

#### Scenario: Dynamic pool resize on config change

- **WHEN** FeedOptions.MaxConcurrency changes via OptionsMonitor while Manager is Idle
- **THEN** it SHALL send AdjustPoolSize(newSize - currentSize) to the router
- **AND** update the tracked pool size

#### Scenario: Deferred pool resize during crawl

- **WHEN** FeedOptions.MaxConcurrency changes while Manager is in Crawling or Probing state
- **THEN** it SHALL defer the resize until ReturnToIdle

### Requirement: FeedCrawlManager stream pipeline uses stream-native Ask

FeedCrawlManager SHALL build the ProcessPage[] array in the actor thread before starting the stream. The stream SHALL use Source.From(messages).Ask<ProcessPageResponse>(router, timeout, parallelism) with no closures over actor state.

#### Scenario: Stream pipeline shape

- **WHEN** StartCrawling is called
- **THEN** Manager SHALL build ProcessPage[] from run state in the actor thread
- **AND** start stream: Source.From(messages).Ask<ProcessPageResponse>(router, pageTimeout, poolSize)
- **AND** TakeWhile(r => r is not ProcessPageCompleted { ReachedEnd: true }, inclusive: true)
- **AND** RunWith(Sink.Seq, materializer).PipeTo(Self)

#### Scenario: No actor context access in stream

- **WHEN** the stream pipeline is running
- **THEN** no stream stage SHALL access _runState, _state, or any actor field
- **AND** all data needed by the stream SHALL be captured in the ProcessPage[] array before stream start

### Requirement: FeedCrawlRunState pattern matches on response type

FeedCrawlRunState.Apply SHALL accept ProcessPageResponse and use pattern matching to dispatch to ApplyCompleted or ApplyFailed. It SHALL NOT use nullable field checks.

#### Scenario: Completed response handling

- **WHEN** Apply receives a ProcessPageCompleted
- **THEN** it SHALL accumulate NewItems, Unmatched, AcceptedEntries, and RejectedByReason

#### Scenario: Failed response handling

- **WHEN** Apply receives a ProcessPageFailed
- **THEN** it SHALL record the error and set HasPageError

### Requirement: RuleSetLookup supports ToTargets extraction

RuleSetLookup SHALL provide a ToTargets() method that returns the distinct RuleSetTarget[] it was built from. This allows the Manager to extract flat data for ProcessPage message construction.

#### Scenario: Round-trip targets

- **WHEN** a RuleSetLookup is built from RegisteredRuleSetEntry items
- **AND** ToTargets() is called
- **THEN** it SHALL return one RuleSetTarget per unique RuleSetId
- **AND** RuleSetLookup.From(targets) SHALL produce an equivalent lookup
