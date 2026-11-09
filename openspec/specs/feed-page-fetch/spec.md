# feed-page-fetch Specification

## Purpose

Defines page fetching for the feed crawl via FeedCrawlPageActor pool routees and the sliding-window harvest.

## Requirements

### Requirement: Default page fetch parameters

The feed crawl SHALL use optimized defaults for MVW queries: `PageSize` of 1000 (MVW's maximum response size), `MaxPages` of 20, and `MaxConcurrency` of 4 concurrent page-pool routees. `PagePrefetch` is removed as a separate concept — the pool size controls concurrency directly.

#### Scenario: Default configuration

- **WHEN** the feed crawl starts with default `FeedOptions`
- **THEN** `PageSize` is 1000
- **AND** `MaxPages` is 20
- **AND** `MaxConcurrency` is 4 (pool size)
- **AND** the maximum item ceiling is 20,000 (1000 × 20)

#### Scenario: MVW returns fewer items than page size

- **WHEN** MVW returns fewer than `PageSize` items
- **THEN** the PageProcessed response SHALL have `ReachedEnd=true`
- **AND** the Manager SHALL stop dispatching new pages

### Requirement: Page fetch via pool routee

Page fetching SHALL be performed by FeedCrawlPageActor pool routees. Each routee Asks MediathekManager directly (no FeedPageActor proxy). The Manager dispatches ProcessPage messages to the pool and receives PageProcessed responses. Sliding-window dispatch: fire initial batch (bounded by pool size), then fire one more on each PageProcessed until MaxPages reached or ReachedEnd.

#### Scenario: Sliding window dispatch

- **WHEN** a crawl starts with MaxConcurrency=4 and MaxPages=20
- **THEN** the Manager SHALL fire 4 initial ProcessPage messages (offsets 0, 1000, 2000, 3000)
- **AND** on each PageProcessed, fire the next offset if more pages remain
- **AND** stop when MaxPages reached or any PageProcessed has ReachedEnd=true

#### Scenario: Window filtering inside page actor

- **WHEN** FeedCrawlPageActor receives items from MediathekManager
- **THEN** it SHALL filter items to those within the CrawlWindow (LowerBound < timestamp <= UpperBound)
- **AND** items below the lower bound SHALL cause ReachedEnd=true
