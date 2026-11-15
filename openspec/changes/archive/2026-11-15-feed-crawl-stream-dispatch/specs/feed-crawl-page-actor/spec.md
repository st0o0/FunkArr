# feed-crawl-page-actor Specification (Delta)

## Purpose

Updates FeedCrawlPageActor to work with Ask-based stream dispatch: remove Stash, reply to captured Sender, add State object, fix naming conventions.

## MODIFIED Requirements

### Requirement: FeedCrawlPageActor uses Become for concurrency safety

FeedCrawlPageActor SHALL use Become to transition through Ready → Fetching → Evaluating → Ready. While in Fetching or Evaluating, the actor SHALL NOT register a handler for ProcessPage. Any ProcessPage arriving in these states SHALL go to dead letters (unhandled). The actor SHALL NOT implement IWithUnboundedStash.

#### Scenario: Become transitions

- **WHEN** FeedCrawlPageActor receives ProcessPage in Ready state
- **THEN** it SHALL capture Sender as _replyTo, Become(Fetching), and stop handling ProcessPage
- **WHEN** the fetch completes and evaluation starts
- **THEN** it SHALL Become(Evaluating)
- **WHEN** all evaluation results are collected
- **THEN** it SHALL reply PageProcessed to _replyTo and Become(Ready)

#### Scenario: No Stash

- **WHEN** FeedCrawlPageActor is in Fetching or Evaluating state
- **THEN** it SHALL NOT stash incoming messages
- **AND** it SHALL NOT implement IWithUnboundedStash

#### Scenario: Pool routes around busy routee

- **WHEN** a routee is in Fetching or Evaluating state
- **THEN** SmallestMailboxPool SHALL route new ProcessPage messages to other routees
- **AND** if a ProcessPage arrives at a busy routee, it SHALL go to dead letters

### Requirement: FeedCrawlPageActor resolves external actors via registry

FeedCrawlPageActor SHALL resolve MediathekManager, TvEvaluationRegion, and MovieEvaluationRegion via Context.GetActor<T>() in the constructor. It SHALL NOT resolve IFeedCrawlManager. It SHALL NOT receive IActorRef as constructor parameters.

#### Scenario: Actor resolution

- **WHEN** FeedCrawlPageActor is constructed
- **THEN** it SHALL resolve IMediathekManager, ITvEvaluationRegion, and IMovieEvaluationRegion from the actor registry
- **AND** it SHALL NOT resolve or hold a reference to IFeedCrawlManager

## ADDED Requirements

### Requirement: FeedCrawlPageActor replies to captured Sender

FeedCrawlPageActor SHALL capture Sender as `_replyTo` when receiving ProcessPage in the Ready state. All replies (PageProcessed) SHALL be sent to `_replyTo`, not to a hardcoded manager reference. This is required because PipeTo overwrites Sender in Fetching and Evaluating handlers.

#### Scenario: Reply to Ask caller

- **WHEN** FeedCrawlPageActor completes page processing (success or fetch failure)
- **THEN** it SHALL send PageProcessed to the _replyTo captured from the original ProcessPage handler
- **AND** it SHALL NOT use a stored _manager reference for the reply

### Requirement: FeedCrawlPageActor uses State object

FeedCrawlPageActor SHALL store all mutable domain state in a `FeedCrawlPageActorState` sealed class in a separate `FeedCrawlPageActorState.cs` file. The actor SHALL NOT have mutable domain fields (counters, collections, flags) directly on the actor class. Infrastructure references (IActorRef, ILoggingAdapter) MAY remain on the actor.

#### Scenario: State object contains all mutable domain state

- **WHEN** FeedCrawlPageActor fields are inspected
- **THEN** _pendingGroups, _totalNewItems, _totalUnmatched, _reachedEnd, _accepted, and _rejectedByReason SHALL be in FeedCrawlPageActorState
- **AND** FeedCrawlPageActorState SHALL have Apply methods for structured mutation
- **AND** FeedCrawlPageActorState SHALL have a static Empty property

#### Scenario: Fresh state per page

- **WHEN** FeedCrawlPageActor receives ProcessPage
- **THEN** it SHALL create a new FeedCrawlPageActorState (via Empty) for each page processing cycle
- **AND** it SHALL NOT manually clear individual fields

### Requirement: FeedCrawlPageActor static readonly fields use PascalCase

FeedCrawlPageActor static readonly fields SHALL use PascalCase naming: `MediathekTimeout`, `EvaluationTimeout`, `MaxLead`. They SHALL NOT use underscore-prefixed camelCase.

#### Scenario: Naming convention

- **WHEN** FeedCrawlPageActor static readonly fields are inspected
- **THEN** they SHALL be named MediathekTimeout, EvaluationTimeout, and MaxLead
