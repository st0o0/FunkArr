## Why

The ScoringManager singleton carries ~120 lines of TestScore logic (scoring + enrichment orchestration) that blocks its mailbox via `ReceiveAsync`. This logic is request-scoped, has no relation to the manager's actual job (config state + production scoring), and should run in isolated per-request actors. Separately, the `IWith*Id` entity ID interfaces don't integrate with Terp's `IWithEntityId`/`IWithId`, forcing a hand-written `ShardMessageExtractor` and preventing use of Terp's `EntityIdExtractor`. The History domain also violates the "one interface per region" rule by sharing `IWithRuleSetId` with the RuleSet domain.

## What Changes

- Extract `HandleTestScoreWithEnrichment`, `RunEnrichment`, `EnrichEpisodes`, `EnrichMovies` from `ScoringManager` into a new `TestScoringWorker` entity actor
- Register `TestScoringWorker` as a Terp `WithEntityRegion<ITestScoringRegion>` (local entity region, not cluster sharded)
- `ScoringManager` forwards `TestScoreWithEnrichment` to the region instead of handling it inline; `TestScoreItems` stays as a one-liner in the manager (just routes to the pool)
- Migrate `IWithDownloadId`, `IWithSearchId` to inherit both `Terp.IWithId` and `Terp.Akka.IWithEntityId` with default interface implementations
- Migrate `IWithRuleSetId` to inherit `Terp.Akka.IWithEntityId` (string ID, no `IWithId`)
- Introduce `IWithHistoryId : IWithEntityId` for History messages, replacing their use of `IWithRuleSetId`
- Introduce `IWithTestScoringId : IWithId, IWithEntityId` for TestScoring messages
- Simplify `ShardMessageExtractor` to a single `IWithEntityId` case
- Update `TestScoreWithEnrichment` to carry `TestScoringId` (Guid) as entity ID instead of `RequestId`

## Capabilities

### New Capabilities

- `entity-id-hierarchy`: Integration of Terp's `IWithEntityId` and `IWithId` into the `IWith*Id` interface hierarchy with default interface implementations
- `test-scoring-worker`: Dedicated per-request entity actor for test scoring with optional enrichment, using Terp's local entity region

### Modified Capabilities

- `shard-message-contract`: ShardMessageExtractor simplified to use `IWithEntityId` instead of per-type pattern matching; `IWithHistoryId` introduced as separate routing contract for History domain

## Impact

- **FunkArr.Messages**: All `IWith*Id` interfaces gain base interfaces and default implementations; new `IWithHistoryId` and `IWithTestScoringId`; History messages change from `IWithRuleSetId` to `IWithHistoryId`; `TestScoreWithEnrichment` gets `IWithTestScoringId`
- **FunkArr.Core**: `ShardMessageExtractor` simplified to single `IWithEntityId` case
- **FunkArr.Scoring**: New `TestScoringWorker` actor; `ScoringManager` loses ~120 lines of test-score logic
- **FunkArr.Scoring.Tests**: New tests for `TestScoringWorker`; updated `ScoringManager` tests
- **FunkArr (Host)**: `AkkaSetupContainer` gains `WithEntityRegion<ITestScoringRegion>` registration
- **FunkArr.Api**: TestScore endpoint changes target from `IScoringManager` to `ITestScoringRegion`
- **No breaking API changes** — the REST endpoint signature and response shape are unchanged
