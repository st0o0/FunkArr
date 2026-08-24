## MODIFIED Requirements

### Requirement: Periodic RSS feed refresh
The `RssFeedCoordinator` SHALL refresh its cache using scatter-gather on the `TextSearchPipeline` ShardRegion instead of sequential asks to `SearchCoordinator`. All topic queries SHALL be sent in parallel, with results collected using a configurable timeout (default: 60 seconds).

#### Scenario: Parallel scatter-gather refresh
- **WHEN** `RssFeedCoordinator` starts a refresh cycle with 60 active ruleset topics
- **THEN** it SHALL send all 60 `TextSearchRequest` messages to the `TextSearchPipeline` ShardRegion simultaneously

#### Scenario: Result collection with timeout
- **WHEN** 58 of 60 topic responses arrive within the timeout and 2 are still pending
- **THEN** `RssFeedCoordinator` SHALL aggregate the 58 received responses and log warnings for the 2 timed-out topics

#### Scenario: Refresh duration
- **WHEN** all topic queries complete within the timeout
- **THEN** the total refresh time SHALL be bounded by the slowest individual query (typically 2-5 seconds) instead of the sum of all queries

### Requirement: Sequential topic search with rate limiting
**This requirement is REMOVED** — replaced by parallel scatter-gather. Rate limiting is handled by the `MediathekGatewayWorker` singleton, not by artificial delays in `RssFeedCoordinator`.

#### Scenario: No artificial delays
- **WHEN** `RssFeedCoordinator` sends topic queries to `TextSearchPipeline`
- **THEN** it SHALL NOT introduce delays between queries — rate limiting is the responsibility of `MediathekGatewayWorker`
