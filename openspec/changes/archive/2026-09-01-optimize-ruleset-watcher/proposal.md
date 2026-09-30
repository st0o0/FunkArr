## Why

The RuleSetManager discards all FileSystemWatcher event arguments and performs a full directory scan (`Directory.GetFiles` on both community and local dirs) on every flush. With hundreds of rulesets, this is wasteful — the watcher already knows which files changed.

## What Changes

- Replace the empty `FileChanged` message with one that carries the affected ruleset ID (extracted from the file path)
- Accumulate affected IDs in a `HashSet<string>` during the debounce window instead of a boolean `Dirty` flag
- Increase debounce window from 500ms to 2 seconds for better batching at scale
- On flush, only check the specific files for affected IDs instead of rescanning entire directories
- Keep `ScanDirectories()` for the initial startup scan only

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ruleset-filewatcher`: Change from full-scan flush to targeted per-ID file checks. Debounce window increases from 500ms to 2s. Event accumulation replaces dirty flag.

## Impact

- `RuleSetManager.cs` — new message type, accumulation logic, targeted flush
- `RuleSetManagerState.cs` — `Dirty: bool` → `PendingIds: HashSet<string>`
- `RuleSetManagerTests.cs` — update tests for new behavior
- No API changes, no persistence changes, no message contract changes to other actors
