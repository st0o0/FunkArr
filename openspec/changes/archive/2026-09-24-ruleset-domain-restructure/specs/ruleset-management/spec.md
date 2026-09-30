## MODIFIED Requirements

### Requirement: RuleSetManager scans ruleset directories at startup
The RuleSetManager (Singleton) SHALL use `RuleSetStore.Scan()` at startup instead of building paths and listing files directly. It SHALL send `Reload` messages to the ShardRegion for each discovered ruleSetId. The Manager SHALL NOT read or deserialize ruleset files for queries — it SHALL maintain a summaries cache populated by `WorkerReady` messages from workers.

#### Scenario: Startup scan
- **WHEN** the system starts with 5 community and 2 local rulesets
- **THEN** the Manager SHALL call `store.Scan()` and send `Reload` to the ShardRegion for each ruleSetId

#### Scenario: Query summaries from cache
- **WHEN** `QueryRuleSetSummaries` is received
- **THEN** the Manager SHALL respond from its in-memory summaries cache without reading from disk

#### Scenario: Worker reports ready
- **WHEN** a `WorkerReady(ruleSetId, ruleCount, sourceType)` is received from a Worker
- **THEN** the Manager SHALL update its summaries cache for that ruleSetId

#### Scenario: Query detail forwarded to ShardRegion
- **WHEN** `QueryRuleSetDetail("tatort")` is received
- **THEN** the Manager SHALL forward the message to the ShardRegion and the Worker SHALL respond directly to the original sender

#### Scenario: FileWatcher triggers reload
- **WHEN** a file change is detected for ruleSetId "tatort"
- **THEN** the Manager SHALL send `Reload("tatort")` to the ShardRegion (not re-read files itself)

### Requirement: RuleSetWorker holds entity state and handles CRUD
The RuleSetWorker (Sharded by ruleSetId) SHALL hold the merged `DiskRuleSet` in memory after loading. It SHALL handle `QueryDetail`, `UpdateLocal`, `DeleteLocal`, and `Export` messages in addition to `Reload`. It SHALL use `RuleSetStore` for disk I/O and `DiskToMatchingExtensions` for domain conversion.

#### Scenario: Reload populates state
- **WHEN** `Reload` is received for ruleSetId "tatort"
- **THEN** the Worker SHALL call `store.LoadMerged("tatort")`, store the result in memory, tell ScoringManager and Resolver, and tell Manager `WorkerReady` with summary data

#### Scenario: QueryDetail from memory
- **WHEN** `QueryDetail` is received and the Worker has loaded state
- **THEN** the Worker SHALL respond with `RuleSetDetailResult` built from in-memory `DiskRuleSet` without disk I/O

#### Scenario: UpdateLocal writes and reloads
- **WHEN** `UpdateLocal(body)` is received
- **THEN** the Worker SHALL convert the body to `DiskRuleSet` via extension, call `store.SaveLocal`, validate the JSON, and re-trigger its own `Reload`

#### Scenario: UpdateLocal validation failure
- **WHEN** `UpdateLocal(body)` is received and validation fails
- **THEN** the Worker SHALL delete the written file and respond with `UpdateLocalRuleSetValidationFailed`

#### Scenario: DeleteLocal
- **WHEN** `DeleteLocal` is received
- **THEN** the Worker SHALL call `store.DeleteLocal(id)`, tell ScoringManager and Resolver to remove, and tell Manager `WorkerRemoved`

#### Scenario: Export from memory
- **WHEN** `Export` is received and the Worker has loaded state
- **THEN** the Worker SHALL serialize the in-memory `DiskRuleSet` to JSON, validate, and respond with the JSON string

## REMOVED Requirements

### Requirement: RuleSetManager reads files for queries
**Reason**: Query handling moves to RuleSetWorker which holds state in memory
**Migration**: Manager forwards QueryDetail to ShardRegion; summaries served from cache

### Requirement: RuleSetExporter service
**Reason**: Export moves into RuleSetWorker
**Migration**: `IRuleSetExporter` interface and `RuleSetExporter` class removed; `GET /rulesets/{id}/export` asks ShardRegion directly

### Requirement: LocalRuleSetWriter actor
**Reason**: CRUD moves into RuleSetWorker; stateless actor adds no value
**Migration**: `ILocalRuleSetWriter` key and actor registration removed; API asks ShardRegion directly
