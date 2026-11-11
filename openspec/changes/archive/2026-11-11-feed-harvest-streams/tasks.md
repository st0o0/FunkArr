# Tasks

## Phase 1: New Types

- [x] Create IngestSummary record and CrawlStatus enum (replaces CrawlResult + CrawlOutcome)
- [x] Create GatedResult record
- [x] Create ItemGroup record with EvaluateAsync method
- [x] Create ScoredGroup record with Gate method (port logic from FeedCrawlPageActor.Evaluate/Reject)
- [x] Create HarvestAccumulator class with Add and ToSummary
- [x] Create StartHarvest, HarvestCompleted, HarvestUnchanged, HarvestFailed messages

## Phase 2: FeedHarvestActor

- [x] Create FeedHarvestActor with Receive<StartHarvest> handler and stream pipeline
- [x] Implement Resolved record and TopicEntry (nested in actor)
- [x] Implement BuildTargetMap (port from FeedCrawlRunState.Apply)
- [x] Implement FilterPage (port from FeedCrawlPageActor.Fetching)
- [x] Implement GroupByTopic (port from FeedCrawlPageActor.Fetching)
- [x] Register FeedHarvestActor actor key in ActorKeys

## Phase 3: Rewrite FeedCrawlManager

- [x] Strip Manager to: timer, persistence, status, options, harvester child
- [x] Remove Become states (Idle/Probing/Crawling), pool, stream, materializer
- [x] Add StartHarvest tell + HarvestCompleted/Unchanged/Failed handlers
- [x] Create FeedHarvestActor child at RecoveryCompleted
- [x] Update FeedCrawlManagerState if needed (CrawlOutcome → CrawlStatus mapping)
- [x] Update Telemetry for new IngestSummary shape

## Phase 4: Delete Old Code

- [x] Delete FeedCrawlPageActor.cs
- [x] Delete FeedCrawlPageActorState.cs
- [x] Delete FeedCrawlPageMessages.cs
- [x] Delete FeedCrawlRunState.cs
- [x] Delete CrawlResult.cs
- [x] Delete RuleSetTarget.cs
- [x] Remove pool/router references from AkkaSetupContainer if any

## Phase 5: Tests

- [x] Unit tests for HarvestAccumulator (Add, ToSummary)
- [x] Unit tests for ScoredGroup.Gate (port + expand from deleted FeedGateTests)
- [x] Unit tests for ItemGroup.EvaluateAsync
- [x] Unit tests for FilterPage and GroupByTopic (port from deleted tests)
- [x] Actor test: FeedHarvestActor with TestProbe mocks for all dependencies
- [x] Actor test: FeedCrawlManager with TestProbe mock for harvester
- [x] Update existing FeedCrawlManagerTests for new message flow

## Phase 6: Cleanup

- [x] Verify build passes with 0 warnings
- [x] Run dotnet format
- [x] Update main specs with delta spec content
- [x] Verify architecture tests still pass (domain boundaries)
