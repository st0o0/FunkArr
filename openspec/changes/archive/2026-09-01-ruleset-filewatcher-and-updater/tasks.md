## 1. Messages and State

- [x] 1.1 Add `LoadRuleSet(RuleSetId, CommunityPath?, LocalPath?)` and `RemoveRuleSet(RuleSetId)` messages to `RuleSetWorker` (replacing `InitializeRuleSet`)
- [x] 1.2 Add `RemoveMatchingConfig(RuleSetId)` message to `MatchMagicManager` and `Apply` extension on `MatchMagicManagerState` to remove a config
- [x] 1.3 Add `DeregisterRuleSet(RuleSetId)` message to `RuleSetResolver` and `Apply` extension on `RuleSetResolverState` to remove mappings
- [x] 1.4 Add `FlushChanges` and `CheckForUpdates` messages (internal to `RuleSetManager` and `RuleSetUpdater` respectively)

## 2. RuleSetWorker Updates

- [x] 2.1 Replace `InitializeRuleSet` handler with `LoadRuleSet` handler (same logic — read, merge, push config, register resolver)
- [x] 2.2 Add `RemoveRuleSet` handler that sends `RemoveMatchingConfig` to MatchMagicManager and `DeregisterRuleSet` to RuleSetResolver

## 3. RuleSetResolver and MatchMagicManager Updates

- [x] 3.1 Add `DeregisterRuleSet` handler to `RuleSetResolver` — remove all topic/alias mappings for the ruleSetId
- [x] 3.2 Add `RemoveMatchingConfig` handler to `MatchMagicManager` — remove config from state

## 4. RuleSetManager Rewrite

- [x] 4.1 Add `RuleSetManagerState` record with known rulesets dictionary and dirty flag
- [x] 4.2 Rewrite `RuleSetManager` — startup scan populates known state, dispatches `LoadRuleSet` per discovered ID
- [x] 4.3 Add FileSystemWatcher setup in `PreStart` (two watchers: community + local dirs), dispose in `PostStop`
- [x] 4.4 Add debounce logic — FSW events set dirty flag, schedule `FlushChanges` (500ms) via `Context.System.Scheduler`
- [x] 4.5 Add `FlushChanges` handler — re-scan directories, diff against known state, dispatch `LoadRuleSet`/`RemoveRuleSet`
- [x] 4.6 Add `FileSystemWatcher.Error` handler — set dirty flag, trigger re-scan

## 5. RuleSetUpdater Actor

- [x] 5.1 Add `RuleSetRepository`, `RuleSetVersion`, `RuleSetRefreshEnabled` properties to `FunkArrOptions`
- [x] 5.2 Register named HttpClient `"GitHub"` in `ServiceSetupContainer` with base address and headers
- [x] 5.3 Create `RuleSetUpdater` actor — constructor receives `HttpClient` and options via DI
- [x] 5.4 Implement `CheckForUpdates` handler — read `version.txt`, query GitHub releases API, compare versions
- [x] 5.5 Implement ZIP download and atomic extraction (temp dir → swap → write `version.txt`)
- [x] 5.6 Add 30-minute self-scheduling via `Context.System.Scheduler` (first check on `PreStart`, skip if refresh disabled)

## 6. Host Wiring

- [x] 6.1 Register `RuleSetUpdater` as Singleton in `AkkaSetupContainer` with DI props
- [x] 6.2 Add `IRuleSetUpdater` actor key to `ActorKeys.cs`
- [x] 6.3 Update `appsettings.json` and `appsettings.Development.json` with new config defaults

## 7. Tests

- [x] 7.1 Add `RuleSetWorker` tests for `LoadRuleSet` (community-only, local-only, merged, re-load) and `RemoveRuleSet`
- [x] 7.2 Add `RuleSetResolver` tests for `DeregisterRuleSet` (existing, unknown)
- [x] 7.3 Add `MatchMagicManager` tests for `RemoveMatchingConfig` (existing, unknown)
- [x] 7.4 Add `RuleSetManager` tests for startup scan, debounced re-scan, and diff-based dispatch
