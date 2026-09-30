## Context

The `FunkArr.RuleSet` domain currently has a 557-line `RuleSetReader` (ex-Merger) that does deserialization, merging, transformation, and export. Disk I/O is scattered across 7 call sites that each build paths and call `IDataFiles` directly. The `RuleSetWorker` loads data at startup but forgets it, forcing the `RuleSetManager` to re-read from disk for every query. A recently added `LocalRuleSetWriter` actor handles CRUD but holds no state and adds unnecessary Akka overhead.

## Goals / Non-Goals

**Goals:**
- Single disk I/O abstraction (`RuleSetStore`) — one place builds paths, reads, writes
- Shared disk model records — replace 9 private `Raw*` classes and 3 duplicate `JsonSerializerOptions`
- Mapping via extension methods — `DiskRuleSet.ToMatchingConfig()`, `RuleSetBody.ToDiskRuleSet()`
- Worker owns entity state — holds `DiskRuleSet` in memory, handles queries and CRUD
- Manager as coordinator only — scan, watch, summaries cache; no disk reads for queries
- API routes directly to ShardRegion for single-entity operations
- Remove `LocalRuleSetWriter` actor and `RuleSetExporter` service

**Non-Goals:**
- Changing the on-disk JSON format or schema
- Making the API accept string enums (stays number-only)
- Persisting rulesets in Akka.Persistence/SQLite
- Changing the ShardRegion or `ShardMessageExtractor` setup

## Decisions

### 1. RuleSetStore as DI service, not actor

The store centralizes disk I/O (paths, read, write, delete, scan) but needs no mailbox or state. It wraps `IDataFiles` + `DataPaths` and calls `RuleSetMerger.Resolve` internally for `LoadMerged`. Injected into Worker and Manager via DI (`resolver.Props<RuleSetWorker>()`).

**Alternative considered:** Reader actor. Rejected because it forces async patterns (Ask + PipeTo) in Worker and Manager for what is synchronous file I/O, adding complexity without value.

### 2. Worker holds DiskRuleSet in memory

After `Reload`, the worker keeps the merged `DiskRuleSet` in its state. `QueryDetail` responds from memory — no disk read. `UpdateLocal` converts the incoming body to a `DiskRuleSet`, saves via store, and re-triggers its own reload. This eliminates the double-read pattern where Manager re-reads what Worker already loaded.

The worker tells the Manager a `WorkerReady` summary (ruleSetId, ruleCount, sourceType) after each reload so the Manager can maintain its summaries cache without disk I/O.

### 3. API routes single-entity operations directly to ShardRegion

`GET/PUT/DELETE /rulesets/{id}` and `GET /rulesets/{id}/export` Ask the `IRuleSetRegion` directly. Messages implement `IWithRuleSetId` for shard routing. Only `GET /rulesets` (list) goes through the Manager because it needs aggregated data.

**Alternative considered:** Forwarding everything through Manager. Rejected because it adds a hop with no value — the Manager doesn't hold per-entity state.

### 4. DiskModel records replace Raw* classes

Immutable records (`DiskRuleSet`, `DiskRule`, `DiskMedia`, etc.) with `init` properties for JSON deserialization. One `DiskJsonOptions` instance with the `JsonStringEnumConverter`s for the disk format. Records are `internal` to `FunkArr.RuleSet`.

### 5. RuleSetMerger extracted as pure function

`RuleSetMerger.Resolve(community, local)` is a pure function: two `DiskRuleSet?` inputs → one `DiskRuleSet?` output. No I/O, no enums, no transformation. Called by `RuleSetStore.LoadMerged`.

### 6. Validation stays at the write boundary

The `RuleSetValidator` is called by the Worker after `store.SaveLocal` returns the serialized JSON. If validation fails, the worker deletes the file and responds with the validation errors. This keeps the store simple (serialize + write) and validation explicit.

## Risks / Trade-offs

- **Worker passivation**: Sharded workers passivate after 5 minutes of idle. After passivation, the next query triggers a reload from disk. This is the existing behavior and acceptable — cold reads are fast (<1ms for a JSON file).

- **Create routing**: `POST /rulesets` creates a new entity. The ShardRegion will auto-create a new Worker for an unknown ruleSetId. The Worker must handle the case where no files exist on disk yet (first-time create).

- **Summaries cache staleness**: The Manager's summaries cache is eventually consistent — updated when Workers send `WorkerReady`. Between a write and the `WorkerReady` message, the list endpoint may serve stale data. This window is milliseconds and acceptable for a local dev tool.
