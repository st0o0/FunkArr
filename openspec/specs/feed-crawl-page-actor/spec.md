# feed-crawl-page-actor Specification

## Purpose

Defines the FeedCrawlPageActor pool routee that fetches one MVW page, groups items by ruleset, evaluates via Search domain, applies FeedGate, and returns accepted entries plus stats to the Manager.

## Requirements

### Requirement: FeedCrawlPageActor processes one page end-to-end

FeedCrawlPageActor SHALL be a non-persistent ReceiveActor pool routee that receives a `ProcessPage` message and independently fetches one MVW page, filters by window, groups by ruleset, evaluates via Search domain, applies FeedGate, and replies with `PageProcessed`.

#### Scenario: Successful page processing

- **WHEN** FeedCrawlPageActor receives ProcessPage(offset=0, window, pageSize=1000, lookup, minConfidence=0.7)
- **THEN** it SHALL Ask MediathekManager for the page
- **AND** filter items by the crawl window bounds
- **AND** deduplicate by stable ID (FeedGate.StableIdFor)
- **AND** group filtered items by ruleset via RuleSetLookup.Match
- **AND** Ask TvEvaluationRegion or MovieEvaluationRegion per group
- **AND** apply FeedGate.Evaluate per group
- **AND** reply PageProcessed with accepted entries and stats

#### Scenario: No items in window

- **WHEN** MVW returns items but none fall within the crawl window
- **THEN** PageProcessed SHALL have empty accepted entries, newItems=0, and reachedEnd based on page size

#### Scenario: No ruleset matches

- **WHEN** all items in the window have topics that match no registered ruleset
- **THEN** PageProcessed SHALL have empty accepted entries and unmatched equal to the filtered item count

### Requirement: FeedCrawlPageActor uses Become for concurrency safety

FeedCrawlPageActor SHALL use Become to transition through Ready → Fetching → Evaluating → Ready. While in Fetching or Evaluating, the actor SHALL NOT accept new ProcessPage messages. This ensures a pool routee never processes two pages concurrently and eliminates stale-sender risk.

#### Scenario: Become transitions

- **WHEN** FeedCrawlPageActor receives ProcessPage in Ready state
- **THEN** it SHALL Become(Fetching) and stop accepting ProcessPage
- **WHEN** the fetch completes and evaluation starts
- **THEN** it SHALL Become(Evaluating)
- **WHEN** all evaluation results are collected
- **THEN** it SHALL reply PageProcessed and Become(Ready)

#### Scenario: Pool routes around busy routee

- **WHEN** a routee is in Fetching or Evaluating state
- **THEN** SmallestMailboxPool SHALL route new ProcessPage messages to other routees

### Requirement: FeedCrawlPageActor fetch failure handling

When the MediathekManager Ask fails or times out, FeedCrawlPageActor SHALL reply with a PageProcessed containing the error and reachedEnd=true, then Become(Ready).

#### Scenario: MVW timeout

- **WHEN** the MediathekManager Ask times out (30s)
- **THEN** PageProcessed SHALL have error set, empty accepted entries, and reachedEnd=true

#### Scenario: MVW error response

- **WHEN** MediathekManager replies with QueryMediathekError
- **THEN** PageProcessed SHALL have error set with the failure description

### Requirement: FeedCrawlPageActor evaluation failure handling

When one or more evaluation Asks fail, FeedCrawlPageActor SHALL still process successful groups. Failed groups contribute zero accepted entries. If all groups fail, the page result has zero accepted entries but is not an error (the page fetch itself succeeded).

#### Scenario: One evaluation group fails

- **WHEN** 3 groups are evaluated and 1 Ask times out
- **THEN** PageProcessed SHALL contain accepted entries from the 2 successful groups
- **AND** error SHALL be null (page-level success)

#### Scenario: All evaluation groups fail

- **WHEN** all evaluation Asks fail
- **THEN** PageProcessed SHALL have empty accepted entries
- **AND** error SHALL be null (page fetched successfully, evaluation produced no results)

### Requirement: ProcessPage message shape

ProcessPage SHALL carry all data needed for self-contained page processing: offset (int), window (CrawlWindow), pageSize (int), lookup (RuleSetLookup), minConfidence (double). These are domain-internal messages in the FunkArr.Feed namespace.

#### Scenario: ProcessPage fields

- **WHEN** ProcessPage is constructed
- **THEN** it SHALL have fields: Offset (int), Window (CrawlWindow), PageSize (int), Lookup (RuleSetLookup), MinConfidence (double)

### Requirement: PageProcessed message shape

PageProcessed SHALL carry: accepted (FeedEntry[]), newItems (int), unmatched (int), reachedEnd (bool), rejectedByReason (IReadOnlyDictionary<RejectionReason, int>?), error (string?). These are domain-internal messages.

#### Scenario: PageProcessed fields

- **WHEN** PageProcessed is constructed for a successful page
- **THEN** Accepted SHALL contain the FeedEntry array from FeedGate
- **AND** NewItems SHALL be the count of items in the window after dedup
- **AND** Unmatched SHALL be items that matched no ruleset
- **AND** ReachedEnd SHALL be true if MVW returned fewer than pageSize items or items fell below the window lower bound
- **AND** Error SHALL be null

### Requirement: FeedCrawlPageActor resolves external actors via registry

FeedCrawlPageActor SHALL resolve MediathekManager, TvEvaluationRegion, and MovieEvaluationRegion via Context.GetActor<T>() in the constructor. It SHALL NOT receive IActorRef as constructor parameters.

#### Scenario: Actor resolution

- **WHEN** FeedCrawlPageActor is constructed
- **THEN** it SHALL resolve IMediathekManager, ITvEvaluationRegion, and IMovieEvaluationRegion from the actor registry
