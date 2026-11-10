# feed-crawl-page-actor Specification

## Purpose

Defines the FeedCrawlPageActor pool routee that fetches one MVW page, groups items by ruleset, evaluates via Search domain, applies FeedGate, and returns accepted entries plus stats to the Manager.

## Requirements

### Requirement: FeedCrawlPageActor processes one page end-to-end

FeedCrawlPageActor SHALL be a non-persistent ReceiveActor pool routee that receives a `ProcessPage` message and independently fetches one MVW page, filters by window, groups by ruleset, evaluates via Search domain, applies FeedGate, and replies with `ProcessPageResponse`.

#### Scenario: Successful page processing

- **WHEN** FeedCrawlPageActor receives ProcessPage(offset=0, window, pageSize=1000, lookup, minConfidence=0.7)
- **THEN** it SHALL Ask MediathekManager for the page
- **AND** filter items by the crawl window bounds
- **AND** deduplicate by stable ID (FeedGate.StableIdFor)
- **AND** group filtered items by ruleset via RuleSetLookup.Match
- **AND** Ask TvEvaluationRegion or MovieEvaluationRegion per group
- **AND** apply FeedGate.Evaluate per group
- **AND** reply ProcessPageCompleted with accepted entries and stats

#### Scenario: No items in window

- **WHEN** MVW returns items but none fall within the crawl window
- **THEN** ProcessPageCompleted SHALL have empty accepted entries, newItems=0, and reachedEnd based on page size

#### Scenario: No ruleset matches

- **WHEN** all items in the window have topics that match no registered ruleset
- **THEN** ProcessPageCompleted SHALL have empty accepted entries and unmatched equal to the filtered item count

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

### Requirement: FeedCrawlPageActor fetch failure handling

When the MediathekManager Ask fails or times out, FeedCrawlPageActor SHALL reply with ProcessPageFailed containing the failure reason, then UnstashAll and Become(Ready).

#### Scenario: MVW timeout

- **WHEN** the MediathekManager Ask times out (30s)
- **THEN** the actor SHALL reply with ProcessPageFailed containing the timeout reason

#### Scenario: MVW error response

- **WHEN** MediathekManager replies with QueryMediathekError
- **THEN** the actor SHALL reply with ProcessPageFailed containing the failure description

### Requirement: FeedCrawlPageActor evaluation failure handling

When one or more evaluation Asks fail, FeedCrawlPageActor SHALL still process successful groups. Failed groups contribute zero accepted entries. If all groups fail, the page result has zero accepted entries but is not an error (the page fetch itself succeeded).

#### Scenario: One evaluation group fails

- **WHEN** 3 groups are evaluated and 1 Ask times out
- **THEN** ProcessPageCompleted SHALL contain accepted entries from the 2 successful groups

#### Scenario: All evaluation groups fail

- **WHEN** all evaluation Asks fail
- **THEN** ProcessPageCompleted SHALL have empty accepted entries (page fetched successfully, evaluation produced no results)

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

### Requirement: ProcessPageResponse message shape

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

### Requirement: FeedCrawlPageActor resolves external actors via registry

FeedCrawlPageActor SHALL resolve MediathekManager, TvEvaluationRegion, and MovieEvaluationRegion via Context.GetActor<T>() in the constructor. It SHALL NOT receive IActorRef as constructor parameters.

#### Scenario: Actor resolution

- **WHEN** FeedCrawlPageActor is constructed
- **THEN** it SHALL resolve IMediathekManager, ITvEvaluationRegion, and IMovieEvaluationRegion from the actor registry

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

### Requirement: FeedCrawlManager stream pipeline uses pre-built messages

FeedCrawlManager SHALL build the ProcessPage[] array in the actor thread before starting the stream. The stream SHALL use Source.From(messages) with SelectAsync to the router, with no closures over actor state.

#### Scenario: Stream pipeline shape

- **WHEN** StartCrawling is called
- **THEN** Manager SHALL build ProcessPage[] from run state in the actor thread
- **AND** start stream: Source.From(messages).SelectAsync(poolSize, msg => router.Ask<ProcessPageResponse>(msg, pageTimeout))
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

### Requirement: RuleSetLookup supports ToDictionary extraction

RuleSetLookup SHALL provide a ToDictionary() method that returns IReadOnlyDictionary<string, RuleSetTarget> exposing its internal topic-to-target mapping. This allows the Manager to extract flat data for ProcessPage message construction.

#### Scenario: Dictionary extraction

- **WHEN** a RuleSetLookup is built from RegisteredRuleSetEntry items
- **AND** ToDictionary() is called
- **THEN** it SHALL return the internal topic-to-target dictionary
- **AND** the dictionary SHALL use OrdinalIgnoreCase comparison
