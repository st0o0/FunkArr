# feed-actor-topology Specification

## Purpose

Defines the Feed crawl actor topology: FeedCrawlManager with a page-pool child and FeedStoreManager.

## Requirements

### Requirement: Feed crawl actor topology

The Feed domain SHALL have 3 actor types across a maximum of 2 hierarchy levels:

1. **FeedCrawlManager** — persistent cluster singleton, owns a page-pool child
2. **FeedCrawlPageActor** — non-persistent pool routee (child of FeedCrawlManager's pool)
3. **FeedStoreManager** — persistent cluster singleton (unchanged)

FeedScheduler, FeedCrawlWorker, FeedPageActor, and FeedEvalActor SHALL be removed.

#### Scenario: Actor type count

- **WHEN** the Feed domain actor types are enumerated
- **THEN** there SHALL be exactly 3: FeedCrawlManager, FeedCrawlPageActor, FeedStoreManager

#### Scenario: Maximum hierarchy depth

- **WHEN** the actor hierarchy is traversed from any Feed singleton
- **THEN** the maximum depth SHALL be 2 (singleton → pool routee)
- **AND** no actor SHALL create child actors of its own

#### Scenario: FeedScheduler removed

- **WHEN** the codebase is searched for FeedScheduler or IFeedScheduler
- **THEN** no actor class, interface, or registration SHALL exist

#### Scenario: FeedCrawlWorker removed

- **WHEN** the codebase is searched for FeedCrawlWorker
- **THEN** no actor class or state class SHALL exist

#### Scenario: FeedPageActor removed

- **WHEN** the codebase is searched for FeedPageActor
- **THEN** no actor class SHALL exist

#### Scenario: FeedEvalActor removed

- **WHEN** the codebase is searched for FeedEvalActor
- **THEN** no actor class SHALL exist
