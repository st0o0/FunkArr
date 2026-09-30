## 1. DiskModel records + shared options

- [x] 1.1 Create `DiskModel/DiskRuleSet.cs` — sealed record types mirroring on-disk JSON: `DiskRuleSet`, `DiskRule`, `DiskMedia`, `DiskFilterGroup`, `DiskFilter`, `DiskTitleRule`, `DiskEnrichment`, `DiskTitleMatch`, `DiskAirdateMatch`, `DiskRuntimeMatch`, `DiskYearMatch`; strategy/field/op as `string?`
- [x] 1.2 Create `DiskModel/DiskJsonOptions.cs` — single `Default` instance with CamelCase, WhenWritingNull, WriteIndented, `JsonStringEnumConverter`s for FilterField/FilterOp/TitlePartType/EnrichmentMethod/RuntimeMode/MediaType

## 2. DiskModel mapping extensions

- [x] 2.1 Create `DiskModel/DiskToMatchingExtensions.cs` — `ToMatchingConfig(id)`, `ToIdentity()`, `ToEnrichmentConfig()`, `ToDetailRules()` using `RuleSetEnumMapping` for string→enum conversion
- [x] 2.2 Create `DiskModel/MatchingToDiskExtensions.cs` — `RuleSetBody.ToDiskRuleSet()` using `RuleSetEnumMapping.ToDiskValue()` for enum→string conversion
- [x] 2.3 Migrate `RuleSetManagerState.ToDetailRules/MapFilters/MapConditions/MapTitleRules` into the disk-to-matching extensions

## 3. RuleSetMerger extraction

- [x] 3.1 Create `RuleSetMerger.cs` — extract `Resolve`, `Merge`, `MergeRules`, `MergeAliases`, `MergeMedia`, `MergeEnrichment` from `RuleSetReader` as static methods operating on `DiskRuleSet`
- [x] 3.2 Remove merge logic from `RuleSetReader`

## 4. RuleSetStore service

- [x] 4.1 Create `RuleSetStore.cs` — inject `IDataFiles`, `DataPaths`; implement `Load(id)`, `LoadMerged(id)`, `SaveLocal(id, DiskRuleSet)`, `DeleteLocal(id)`, `ExistsLocal(id)`, `ExistsCommunity(id)`, `Scan()`
- [x] 4.2 `LoadMerged` SHALL deserialize both files and call `RuleSetMerger.Resolve`
- [x] 4.3 `SaveLocal` SHALL serialize with `DiskJsonOptions`, create directory, write atomically, return JSON string
- [x] 4.4 Register `RuleSetStore` as singleton service in `ServiceSetupContainer`

## 5. Worker CRUD messages

- [x] 5.1 Add `IWithRuleSetId` to `QueryRuleSetDetail` (currently lacks it — goes through Manager)
- [x] 5.2 Add `IWithRuleSetId` to `CreateLocalRuleSet`, `UpdateLocalRuleSet`, `DeleteLocalRuleSet`, `ExportRuleSet` messages
- [x] 5.3 Add `WorkerReady` message: `sealed record WorkerReady(string RuleSetId, int RuleCount, string SourceType) : IWithRuleSetId` — Worker→Manager notification
- [x] 5.4 Add `WorkerRemoved` message: `sealed record WorkerRemoved(string RuleSetId)` — Worker→Manager notification

## 6. Expand RuleSetWorker

- [x] 6.1 Add `RuleSetWorkerState.cs` — holds `DiskRuleSet? Merged`, `RuleSetPaths Paths`
- [x] 6.2 Refactor `Reload` handler: use `store.LoadMerged(id)`, store in state, tell downstream, tell Manager `WorkerReady`
- [x] 6.3 Add `QueryRuleSetDetail` handler: respond from in-memory state via `disk.ToIdentity()` + `disk.ToDetailRules()`
- [x] 6.4 Add `CreateLocalRuleSet` handler: check `store.ExistsLocal`, convert `body.ToDiskRuleSet()`, `store.SaveLocal`, validate, reload self on success
- [x] 6.5 Add `UpdateLocalRuleSet` handler: check exists (community or local), convert, save, validate, reload self
- [x] 6.6 Add `DeleteLocalRuleSet` handler: `store.DeleteLocal`, tell downstream Remove, tell Manager `WorkerRemoved`
- [x] 6.7 Add `ExportRuleSet` handler: serialize in-memory merged state, validate, respond with JSON
- [x] 6.8 Inject `RuleSetStore` and `IRuleSetValidator` via constructor DI

## 7. Simplify RuleSetManager

- [x] 7.1 Replace `ScanDirectories()` with `store.Scan()` — inject `RuleSetStore`
- [x] 7.2 Add `WorkerReady` handler: update summaries cache (new state field `ImmutableDictionary<string, WorkerSummary>`)
- [x] 7.3 Add `WorkerRemoved` handler: remove from summaries cache
- [x] 7.4 Rewrite `HandleQueryDetail`: forward to ShardRegion instead of building from disk
- [x] 7.5 Rewrite `ToSummaries`: read from summaries cache instead of disk
- [x] 7.6 Remove `BuildDetail` and disk-reading `ToSummaries` from `RuleSetManagerState`
- [x] 7.7 Remove direct `IDataFiles` usage from Manager (use store for scan only)

## 8. Refactor API endpoints

- [x] 8.1 Detail endpoint: Ask `IRuleSetRegion` directly instead of `IRuleSetManager`
- [x] 8.2 Create/Update/Delete endpoints: Ask `IRuleSetRegion` directly
- [x] 8.3 Export endpoint: Ask `IRuleSetRegion` directly
- [x] 8.4 List endpoint: keep Ask `IRuleSetManager` (summaries cache)
- [x] 8.5 Remove `LocalRuleSetWriter` actor, `ILocalRuleSetWriter` key, registration in AkkaSetupContainer
- [x] 8.6 Remove `RuleSetExporter`, `IRuleSetExporter`, registration

## 9. Remove old code

- [x] 9.1 Delete `RuleSetReader.cs` (or slim to empty if any utility methods remain)
- [x] 9.2 Delete `RuleSetWriter.cs` (absorbed by MatchingToDiskExtensions + RuleSetStore.SaveLocal)
- [x] 9.3 Delete `LocalRuleSetWriter.cs`
- [x] 9.4 Delete `RuleSetExporter.cs`, `IRuleSetExporter.cs`, `RuleSetExportResult.cs`
- [x] 9.5 Remove `JsonStringEnumMemberName` from API enums if still present
- [x] 9.6 Remove `ILocalRuleSetWriter` from `ActorKeys.cs`

## 10. Tests

- [x] 10.1 Add `RuleSetStoreTests` — Load, LoadMerged, SaveLocal, DeleteLocal, Scan
- [x] 10.2 Add `DiskToMatchingExtensionsTests` — ToMatchingConfig, ToIdentity, ToEnrichmentConfig, ToDetailRules
- [x] 10.3 Add `MatchingToDiskExtensionsTests` — ToDiskRuleSet with all enum types
- [x] 10.4 Add `RuleSetMergerTests` — Resolve, Merge (reuse existing test data)
- [x] 10.5 Expand `RuleSetWorkerTests` — QueryDetail, CreateLocal, UpdateLocal, DeleteLocal, Export
- [x] 10.6 Update `RuleSetManagerTests` — verify forward to ShardRegion, WorkerReady handling
- [x] 10.7 Verify `dotnet build` + all test projects pass (726 tests, 0 failures)
- [x] 10.8 Verify architecture tests pass (new naming, removed classes)
