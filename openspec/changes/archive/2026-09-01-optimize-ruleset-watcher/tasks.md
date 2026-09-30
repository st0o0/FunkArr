## 1. State Changes

- [x] 1.1 Update `RuleSetManagerState` — replace `Dirty: bool` with `PendingIds: HashSet<string>` and add `FullRescanRequested: bool`
- [x] 1.2 Add `CheckRuleSetPaths` method to build `RuleSetPaths` for a single ruleset ID by checking file existence and timestamps

## 2. Message and Watcher Changes

- [x] 2.1 Replace `FileChanged` with `FileChanged(string RuleSetId)` carrying the affected ID extracted from the file path
- [x] 2.2 Add `FullRescanRequested` message for watcher error events
- [x] 2.3 Update `CreateWatcher` to extract ruleset ID from `FileSystemEventArgs.FullPath` and pass it in the message; wire error handler to send `FullRescanRequested`

## 3. Flush Logic

- [x] 3.1 Update `HandleFileChanged` to accumulate the ruleset ID into `PendingIds` instead of setting a dirty flag
- [x] 3.2 Update debounce window from 500ms to 2 seconds
- [x] 3.3 Rewrite `HandleFlush` to branch on `FullRescanRequested` — full scan fallback vs targeted per-ID checks
- [x] 3.4 Implement targeted flush: for each pending ID, call `CheckRuleSetPaths`, diff against `KnownRuleSets[id]`, dispatch `LoadRuleSet`/`RemoveRuleSet`

## 4. Tests

- [x] 4.1 Update `RuleSetManagerTests` for new accumulation behavior and targeted flush logic
