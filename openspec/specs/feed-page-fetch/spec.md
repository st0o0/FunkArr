# feed-page-fetch Specification

## Purpose

Defines page fetching for the feed crawl via FeedPageActor and the sliding-window harvest. TBD: refine.

## Requirements


### Requirement: Default page fetch parameters

The feed crawl SHALL use optimized defaults for MVW queries: `PageSize` of 1000
(MVW's maximum response size), `MaxPages` of 20, and `PagePrefetch` of 2
concurrent requests.

#### Scenario: Default configuration

- **WHEN** the feed crawl starts with default `FeedOptions`
- **THEN** `PageSize` is 1000
- **AND** `MaxPages` is 20
- **AND** `PagePrefetch` is 2
- **AND** the maximum item ceiling is 20,000 (1000 × 20)

#### Scenario: MVW returns fewer items than page size

- **WHEN** MVW returns fewer than `PageSize` items (its internal cap is 1000)
- **THEN** the harvester marks `ReachedEnd` and stops dispatching new pages
