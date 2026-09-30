## ADDED Requirements

### Requirement: RssFeedCoordinator singleton actor
The system SHALL register an `RssFeedCoordinator` actor as a singleton in the ActorSystem, resolvable via `IActorRegistry`. The coordinator SHALL maintain an in-memory cache of recent Mediathek content matching active rulesets.

#### Scenario: Registration and resolution
- **WHEN** the application starts
- **THEN** `RssFeedCoordinator` SHALL be registered and resolvable via `actorRegistry.GetAsync<RssFeedCoordinator>()`

#### Scenario: Actor naming
- **WHEN** `RssFeedCoordinator` is created
- **THEN** it SHALL follow the project naming convention with the `Coordinator` suffix

### Requirement: Periodic RSS feed refresh
The `RssFeedCoordinator` SHALL periodically refresh its cache by querying `SearchCoordinator` for each active ruleset topic. The refresh interval SHALL be configurable (default: 30 minutes).

#### Scenario: Initial refresh on startup
- **WHEN** `RssFeedCoordinator` starts
- **THEN** it SHALL schedule an immediate refresh to populate the cache

#### Scenario: Periodic timer-based refresh
- **WHEN** the configured refresh interval elapses (default 30 minutes)
- **THEN** `RssFeedCoordinator` SHALL trigger a new refresh cycle

#### Scenario: Configurable refresh interval
- **WHEN** the configuration specifies `FunkArr__RssFeed__RefreshIntervalMinutes=60`
- **THEN** the refresh cycle SHALL run every 60 minutes

### Requirement: Ruleset-driven topic discovery
During a refresh cycle, `RssFeedCoordinator` SHALL ask `RuleSetCoordinator` for all active rulesets and extract the distinct topics to search for.

#### Scenario: Topics from rulesets
- **WHEN** `RuleSetCoordinator` returns rulesets for "Tatort", "heute-show", and "Tagesschau"
- **THEN** `RssFeedCoordinator` SHALL search for all three topics

#### Scenario: No rulesets configured
- **WHEN** `RuleSetCoordinator` returns zero rulesets
- **THEN** `RssFeedCoordinator` SHALL skip the refresh and keep the cache empty

#### Scenario: Deduplicated topics
- **WHEN** multiple rulesets share the same topic (e.g., aliases)
- **THEN** `RssFeedCoordinator` SHALL search each unique topic only once

### Requirement: Sequential topic search with rate limiting
During a refresh, `RssFeedCoordinator` SHALL query topics sequentially through `SearchCoordinator`, with a configurable delay between queries (default: 1 second) to avoid overwhelming MediathekViewWeb.

#### Scenario: Sequential execution
- **WHEN** refreshing 3 topics
- **THEN** `RssFeedCoordinator` SHALL search topic 1, wait, search topic 2, wait, search topic 3

#### Scenario: Search via SearchCoordinator pipeline
- **WHEN** searching for topic "Tatort"
- **THEN** `RssFeedCoordinator` SHALL send a `TextSearchRequest("Tatort")` to `SearchCoordinator`, leveraging its full pipeline (Mediathek query, content filter, quality probing, caching)

### Requirement: Aggregated result cache
The `RssFeedCoordinator` SHALL aggregate results from all topic searches into a single cache, sorted by timestamp (newest first), deduplicated by URL, and bounded to a configurable maximum (default: 500 items).

#### Scenario: Aggregation across topics
- **WHEN** "Tatort" returns 20 results and "heute-show" returns 15 results
- **THEN** the cache SHALL contain up to 35 items, sorted newest first

#### Scenario: Deduplication by URL
- **WHEN** the same video URL appears in results from different topics
- **THEN** the cache SHALL contain only one entry for that URL

#### Scenario: Bounded cache size
- **WHEN** aggregated results exceed 500 items
- **THEN** the cache SHALL keep only the 500 newest items

#### Scenario: Atomic cache replacement
- **WHEN** a refresh cycle completes
- **THEN** the entire cache SHALL be replaced atomically (not merged incrementally)

### Requirement: Query interface for RSS feed
`RssFeedCoordinator` SHALL respond to `GetRssFeed(int limit, int offset)` messages with `RssFeedResponse(IReadOnlyList<SearchResult> items, int total)`, returning a paginated slice of the cached results.

#### Scenario: Default pagination
- **WHEN** a `GetRssFeed(limit=100, offset=0)` request arrives and the cache has 250 items
- **THEN** the response SHALL contain the first 100 items and total=250

#### Scenario: Offset pagination
- **WHEN** a `GetRssFeed(limit=50, offset=100)` request arrives
- **THEN** the response SHALL contain items 101-150 from the cache

#### Scenario: Cache empty
- **WHEN** no refresh has completed yet
- **THEN** the response SHALL contain zero items and total=0

### Requirement: Refresh resilience
The refresh cycle SHALL handle failures gracefully without losing the existing cache.

#### Scenario: Single topic failure
- **WHEN** a `SearchCoordinator` query for one topic times out or fails
- **THEN** `RssFeedCoordinator` SHALL log the error, skip that topic, and continue with remaining topics

#### Scenario: Complete refresh failure
- **WHEN** all topic queries fail during a refresh
- **THEN** `RssFeedCoordinator` SHALL retain the previous cache contents

#### Scenario: RuleSetCoordinator unavailable
- **WHEN** asking `RuleSetCoordinator` for rulesets times out
- **THEN** `RssFeedCoordinator` SHALL log the error and retry on the next refresh cycle
