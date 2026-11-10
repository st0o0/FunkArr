# crawl-stream-dispatch Specification

## Purpose

Defines the Akka.Streams-based page dispatch that replaces manual Tell-loop + PendingPages tracking in FeedCrawlManager.

## ADDED Requirements

### Requirement: Stream-based page dispatch

FeedCrawlManager SHALL dispatch page processing via an Akka.Streams pipeline: `Source.From(offsets) → SelectAsync(maxConcurrency, Ask) → TakeWhile(!reachedEnd) → Sink.Seq → PipeTo(Self)`. The stream SHALL replace the manual Tell-loop and PendingPages/PagesFired/NextOffset tracking.

#### Scenario: Stream dispatches pages with bounded parallelism

- **WHEN** StartCrawling is called with MaxConcurrency=3 and MaxPages=10
- **THEN** the stream SHALL generate offsets 0 through 9×PageSize
- **AND** SelectAsync SHALL limit concurrent Asks to 3
- **AND** each Ask SHALL send a ProcessPage to the page pool and await PageProcessed

#### Scenario: Stream stops on reachedEnd

- **WHEN** a PageProcessed result has ReachedEnd=true
- **THEN** TakeWhile SHALL include that result and stop the stream
- **AND** in-flight Asks MAY complete but their results SHALL be discarded

#### Scenario: Stream delivers batch result via PipeTo

- **WHEN** the stream completes (all pages processed or TakeWhile triggered)
- **THEN** Sink.Seq SHALL collect all PageProcessed results into an IImmutableList
- **AND** PipeTo SHALL deliver CrawlPagesCompleted(results) to Self on the Actor thread

#### Scenario: Stream failure delivers error via PipeTo

- **WHEN** the stream fails (Ask timeout, materialization error)
- **THEN** PipeTo SHALL deliver CrawlPagesFailed(reason) to Self on the Actor thread

### Requirement: Thread safety of stream closures

Stream closures SHALL capture only immutable or read-only values: IActorRef (immutable, Ask is thread-safe), CrawlInput/CrawlWindow (immutable records), RuleSetLookup (read-only after construction), and static readonly TimeSpan. No mutable state SHALL be captured or mutated from stream threads.

#### Scenario: No shared mutable state

- **WHEN** SelectAsync lambdas execute on Materializer threads
- **THEN** they SHALL NOT read or write FeedCrawlRunState, FeedCrawlManagerState, or any mutable field on FeedCrawlManager

### Requirement: Manager Become-state collapse

FeedCrawlManager SHALL have 3 Become-states: Idle, Probing, and Crawling. Probing SHALL combine the former Probing and LoadingRuleSets states. Crawling SHALL combine the former Crawling and Publishing states. Small handler methods SHALL be inlined as lambdas in Command registrations.

#### Scenario: Probing handles probe and ruleset loading

- **WHEN** a probe Ask succeeds with a valid ListTimestamp
- **THEN** the Probing state SHALL Ask for registered rulesets inline
- **AND** on RuleSetsLoaded, transition to StartCrawling
- **AND** on RuleSetsFailed, return to Idle

#### Scenario: Crawling handles pages and publishing

- **WHEN** CrawlPagesCompleted arrives
- **THEN** the Crawling state SHALL aggregate all results via ApplyAll
- **AND** if accepted entries exist, Ask FeedStoreManager to publish inline
- **AND** on PublishCompleted or PublishFailed, call CompleteCrawl
- **AND** if no accepted entries, call CompleteCrawl directly

### Requirement: FeedCrawlRunState removes dispatch fields

FeedCrawlRunState SHALL NOT contain PendingPages, NextOffset, PagesFired, HasMorePages, StartCrawling(), or NextPageRequest(). It SHALL retain only aggregation state and provide an ApplyAll method that iterates Apply per result.

#### Scenario: ApplyAll aggregates batch

- **WHEN** ApplyAll is called with a list of PageProcessed results
- **THEN** it SHALL call Apply for each result in order
- **AND** AcceptedEntries, TotalNewItems, TotalUnmatched, RejectedByReason, ReachedEnd, and error state SHALL reflect all results

### Requirement: Manager internal messages for stream results

FeedCrawlManager SHALL define two sealed records for stream results: `CrawlPagesCompleted(IImmutableList<PageProcessed> Results)` and `CrawlPagesFailed(string Reason)`. These are private sealed records on the Manager class.

#### Scenario: CrawlPagesCompleted carries all page results

- **WHEN** the stream completes successfully
- **THEN** CrawlPagesCompleted.Results SHALL contain all PageProcessed results that passed TakeWhile

#### Scenario: CrawlPagesFailed carries error reason

- **WHEN** the stream fails
- **THEN** CrawlPagesFailed.Reason SHALL contain the exception message
