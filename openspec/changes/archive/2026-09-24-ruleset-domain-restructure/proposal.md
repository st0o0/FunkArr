## Why

The `FunkArr.RuleSet` domain has grown organically and now has scattered responsibilities: disk I/O in 7 different places, a 557-line God-class (`RuleSetReader`/ex-Merger) doing deserialization + merging + transformation + export, duplicate `JsonSerializerOptions`, and private nested Raw* classes that prevent reuse. The `RuleSetManager` re-reads all files from disk for every query instead of using data the `RuleSetWorker` already loaded. The recently added `LocalRuleSetWriter` actor is stateless and adds unnecessary Akka overhead for synchronous disk writes.

This restructure extracts shared disk models, centralizes file I/O into a `RuleSetStore`, moves read/write operations into the `RuleSetWorker` (the natural entity owner), and reduces the `RuleSetManager` to a coordination-only role.

## What Changes

- Extract `DiskModel/` — shared records (`DiskRuleSet`, `DiskRule`, `DiskMedia`, etc.) and a single `DiskJsonOptions` instance, replacing 3 duplicate `JsonSerializerOptions` and 9 private nested `Raw*` classes
- Extract `DiskToMatchingExtensions` — extension methods mapping `DiskRuleSet` → domain types (`MatchingConfig`, `Identity`, `EnrichmentConfig`, `DetailRules`)
- Extract `MatchingToDiskExtensions` — extension methods mapping domain `RuleSetBody` → `DiskRuleSet`
- New `RuleSetStore` service — single place for all disk I/O: `Load`, `LoadMerged`, `SaveLocal`, `DeleteLocal`, `Scan`; replaces 7 scattered `Path.Join` + `IDataFiles` call sites
- Extract `RuleSetMerger` — pure function `Resolve(community, local) → merged DiskRuleSet`, extracted from the current reader's merge logic
- Expand `RuleSetWorker` — holds `DiskRuleSet` in memory as state; handles `QueryDetail`, `UpdateLocal`, `DeleteLocal`, `Export` in addition to existing `Reload`; API routes single-entity operations directly to `ShardRegion` bypassing Manager
- Simplify `RuleSetManager` — drops `BuildDetail` and `ToSummaries` disk reads; keeps only `Scan`, `FileWatcher`, and a summaries cache populated by `WorkerReady` messages from workers
- Remove `LocalRuleSetWriter` actor — CRUD moves into `RuleSetWorker`
- Remove `RuleSetExporter` service — export moves into `RuleSetWorker`
- Slim down `RuleSetReader` — becomes a thin static class that only orchestrates `store.Load` + `merger.Resolve` + `extensions.ToDomain` if anything remains; may be fully absorbed by `RuleSetWorker`

## Capabilities

### New Capabilities
- `ruleset-store`: Centralized disk I/O service for ruleset files — path resolution, serialization, deserialization, read, write, delete, scan
- `ruleset-disk-model`: Shared data records for the on-disk JSON format with bidirectional mapping extensions

### Modified Capabilities
- `ruleset-management`: RuleSetManager drops disk-read query handling, keeps scan/watch/summaries-cache; RuleSetWorker becomes the entity owner for queries and CRUD
- `ruleset-api`: Single-entity endpoints (detail, update, delete, export) Ask ShardRegion directly instead of Manager; list endpoint stays with Manager

## Impact

- `FunkArr.RuleSet`: Major restructure — new DiskModel/ directory, RuleSetStore, RuleSetMerger extracted, Worker expanded, Reader/Exporter/LocalRuleSetWriter removed or absorbed
- `FunkArr.Api`: RuleSetApiEndpoints routes detail/CRUD to ShardRegion, list to Manager; removes all direct disk-format concerns
- `FunkArr.Messages`: New messages for Worker CRUD (`UpdateLocalRuleSet`, `DeleteLocalRuleSet`, `ExportRuleSet`, `QueryRuleSetDetail` with `IWithRuleSetId`)
- `FunkArr.Core`: `ILocalRuleSetWriter` actor key removed; `IRuleSetExporter` interface removed
- `FunkArr/Configuration`: `LocalRuleSetWriter` actor registration removed from AkkaSetupContainer
- `FunkArr.RuleSet.Tests`: Tests restructured for new classes, Worker tests expanded for CRUD
