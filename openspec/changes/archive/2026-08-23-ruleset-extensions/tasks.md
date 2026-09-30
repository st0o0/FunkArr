## 1. Rename RuleSetRegistryActor → RuleSetCoordinator

- [x] 1.1 Renamed file and class `RuleSetRegistryActor` → `RuleSetCoordinator`
- [x] 1.2 Updated `FunkArrActorSystemSetup` registration
- [x] 1.3 Updated all controller references (RulesetController, MatchIntelligenceController)
- [x] 1.4 Updated SearchCoordinator references

## 2. Rename RuleSetGeneratorActor → RuleSetGeneratorWorker

- [x] 2.1 Renamed file and class
- [x] 2.2 Updated RuleSetCoordinator and test references

## 3. Extract RefreshWorker

- [x] 3.1 Created `RefreshWorker.cs` — permanent child wrapping GitHubReleaseClient.RefreshAsync
- [x] 3.2 Updated RuleSetCoordinator: spawns RefreshWorker in constructor, forwards RefreshCommunity timer, handles RefreshComplete

## 4. Replace MatchLedgerActor with MatchQualityWorker

- [x] 4.1 Created `MatchQualityWorker.cs` — ReceivePersistentActor (PersistenceId: "match-quality") with same query API, event-sourced with MatchRecorded/MatchesExpired, snapshots every 500, time-based eviction
- [x] 4.2 Created `Persistence/MatchQualityEventDtos.cs` with DTOs and mapping
- [x] 4.3 Updated RuleSetCoordinator: spawns MatchQualityWorker, forwards RecordMatchResult and query messages
- [x] 4.4 Deleted `MatchLedgerActor.cs`, removed registration from `FunkArrActorSystemSetup`
- [x] 4.5 Updated controllers to resolve RuleSetCoordinator for match queries (MatchIntelligenceController, RulesetController)
- [x] 4.6 Updated SearchCoordinator: removed MatchLedgerActor dependency, sends match records through RuleSetCoordinator

## 5. Cleanup and Verify

- [x] 5.1 dotnet format clean, build passes 0 warnings / 0 errors
- [x] 5.2 Deleted MatchLedgerActorTests.cs, updated SearchCoordinatorTests stub to handle match records
- [x] 5.3 Full test suite: 429/429 passing, 0 failures
