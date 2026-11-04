## 1. Entity ID Interface Hierarchy

- [x] 1.1 Add `IWithEntityId` and `IWithId` inheritance to `IWithDownloadId` with default implementations (`Guid IWithId.Id => DownloadId`, `string IWithEntityId.EntityId => DownloadId.ToString()`)
- [x] 1.2 Add `IWithEntityId` and `IWithId` inheritance to `IWithSearchId` with default implementations
- [x] 1.3 Add `IWithEntityId` inheritance to `IWithRuleSetId` with default implementation (`string IWithEntityId.EntityId => RuleSetId`)
- [x] 1.4 Create `IWithHistoryId : IWithEntityId` in FunkArr.Messages with `string HistoryId` and default `EntityId` implementation
- [x] 1.5 Migrate History messages (`RecordHistory`, `QueryScoringHistory`, `QueryScoringStats`, `QueryScoringDetail`) from `IWithRuleSetId` to `IWithHistoryId` — rename `RuleSetId` property to `HistoryId`
- [x] 1.6 Update all callers that construct History messages to use the new `HistoryId` parameter name
- [x] 1.7 Build and fix any compilation errors from the History message migration

## 2. ShardMessageExtractor Simplification

- [x] 2.1 Simplify `ShardMessageExtractor.EntityId()` to a single `IWithEntityId m => m.EntityId` case
- [x] 2.2 Update ShardMessageExtractor tests to verify the new single-case behavior
- [x] 2.3 Build solution and run all existing tests to verify no regressions

## 3. TestScoring Messages and Interfaces

- [x] 3.1 Create `IWithTestScoringId : IWithId, IWithEntityId` in FunkArr.Messages with `Guid TestScoringId` and default implementations
- [x] 3.2 Create `ITestScoringRegion` marker interface in FunkArr.Messages
- [x] 3.3 Update `TestScoreWithEnrichment` to implement `IWithTestScoringId` — rename `RequestId` to `TestScoringId`
- [x] 3.4 Update `TestScoreItems` to use `TestScoringId` parameter name for consistency
- [x] 3.5 Update all callers constructing `TestScoreWithEnrichment` and `TestScoreItems` to use the new parameter names

## 4. TestScoringWorker Actor

- [x] 4.1 Create `TestScoringWorker` as a `ReceiveActor` in FunkArr.Scoring — initial structure with constructor taking `string nttId`, resolve `IScoringManager` and `IEnrichmentManager` from registry
- [x] 4.2 Implement the scoring phase: receive `TestScoreWithEnrichment`, Ask `IScoringManager` with `TestScoreItems`, PipeTo self
- [x] 4.3 Implement the no-enrichment path: on `ScoreCompleted` without enrichment config, reply with `TestScoreWithEnrichmentCompleted` and self-passivate
- [x] 4.4 Implement the enrichment phase: on `ScoreCompleted` with enrichment config, Become waiting-for-enrichment, Ask `IEnrichmentManager`, PipeTo self
- [x] 4.5 Implement enrichment result merging (episode and movie paths) and reply with `TestScoreWithEnrichmentCompleted`, self-passivate
- [x] 4.6 Implement failure handling: scoring failure → `TestScoreWithEnrichmentFailed` + passivate; enrichment failure → reply with scoring-only results + passivate

## 5. ScoringManager Cleanup

- [x] 5.1 Remove `HandleTestScoreWithEnrichment`, `RunEnrichment`, `EnrichEpisodes`, `EnrichMovies` from `ScoringManager`
- [x] 5.2 Remove the `Receive<TestScoreWithEnrichment>` registration from the constructor
- [x] 5.3 Remove the `_enrichmentTimeout` field and any enrichment-related usings
- [x] 5.4 Verify `ScoringManager` retains `Receive<TestScoreItems>` one-liner forwarding to the pool
- [x] 5.5 Update existing `ScoringManagerTests` to remove test-score-with-enrichment test cases

## 6. Registration and Wiring

- [x] 6.1 Register `WithEntityRegion<ITestScoringRegion>` in `AkkaSetupContainer` with `EntityIdExtractor`, passivation timeout 30s
- [x] 6.2 Update the `/rulesets/test` API endpoint to resolve `ITestScoringRegion` instead of `IScoringManager` and use `TestScoringId` instead of `RequestId`

## 7. Tests

- [x] 7.1 Write `TestScoringWorkerTests` — scoring without enrichment: verify worker sends `TestScoreItems` to scoring manager probe, receives `ScoreCompleted`, replies with `TestScoreWithEnrichmentCompleted`
- [x] 7.2 Write `TestScoringWorkerTests` — scoring with TV enrichment: verify scoring Ask → enrichment Ask → merged reply
- [x] 7.3 Write `TestScoringWorkerTests` — scoring with movie enrichment: verify scoring Ask → enrichment Ask → merged reply
- [x] 7.4 Write `TestScoringWorkerTests` — enrichment failure: verify reply contains scoring-only results
- [x] 7.5 Write `TestScoringWorkerTests` — scoring failure: verify `TestScoreWithEnrichmentFailed` response
- [x] 7.6 Run full solution build and all test projects to verify no regressions
