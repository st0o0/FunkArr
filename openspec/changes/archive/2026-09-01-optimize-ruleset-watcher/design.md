## Context

The RuleSetManager is a Cluster Singleton that watches two directories (`data/community/rulesets/` and `data/local/rulesets/`) for JSON file changes via `FileSystemWatcher`. Currently, every watcher event — regardless of which file changed — triggers a full directory scan on flush. The watcher event arguments (path, change type) are discarded. With hundreds of rulesets, this creates unnecessary I/O on every single file change.

## Goals / Non-Goals

**Goals:**
- Eliminate full directory scans on file change events by using targeted per-ID file checks
- Accumulate affected ruleset IDs during the debounce window instead of a boolean dirty flag
- Increase debounce window to 2 seconds for better batching at scale
- Maintain self-healing behavior (correct state even if events are imprecise)

**Non-Goals:**
- Persisting known ruleset state across restarts (startup scan is sufficient)
- Eliminating the initial startup scan (called once, cheap on SSD)
- Changing the RuleSetWorker, RuleSetResolver, or MatchMagicManager
- Tracking change types (Created/Changed/Deleted) — re-checking file existence on flush is simpler and self-healing

## Decisions

### Accumulate ruleset IDs, not paths or change types

On each watcher event, extract the ruleset ID from `Path.GetFileNameWithoutExtension(e.FullPath)` and add it to a `HashSet<string>` on the state. On flush, for each pending ID, check whether the community and/or local file exists and compare against known state.

**Why not track change types?** Multiple events for the same file can arrive within the debounce window (e.g., editor writes temp file then renames). Tracking types means reconciling contradictory events. Checking reality on flush is simpler and always correct.

**Why not track full paths?** The ruleset ID is the unit of work — a change to either the community or local file for the same ID triggers the same action (re-merge both). The ID is sufficient.

### Targeted flush checks file existence and timestamps

For each pending ID, the flush handler:
1. Checks `{communityDir}/{id}.json` — exists? timestamp?
2. Checks `{localDir}/{id}.json` — exists? timestamp?
3. Builds a `RuleSetPaths` from reality
4. Compares against `KnownRuleSets[id]`
5. Dispatches `LoadRuleSet` (changed/added) or `RemoveRuleSet` (both gone)

This replaces `ScanDirectories()` in the flush path. The method itself stays for startup.

### Increase debounce window to 2 seconds

500ms is aggressive for batch operations (git pull updating dozens of files, bulk copy). 2 seconds gives enough time for batch operations to settle while remaining responsive for single-file edits.

### Watcher error event triggers full scan fallback

When `FileSystemWatcher` raises an Error (buffer overflow), individual events may have been lost. In this case, fall back to a full `ScanDirectories()` scan. This is the only runtime path that triggers a full scan after startup.

A new `FullRescanRequested` flag on state distinguishes this from normal pending-ID flushes.

## Risks / Trade-offs

**[Missed events on watcher buffer overflow]** → FileSystemWatcher.Error handler falls back to full scan. Same behavior as current implementation.

**[Rename generates two IDs]** → Rename from `foo.json` to `bar.json` fires events for both names. `foo` is checked, found missing, removed. `bar` is checked, found present, added. Works naturally without special handling.

**[2s debounce delay for single-file edits]** → Acceptable for a background sync system. Rulesets are not latency-sensitive — a 2s delay between saving a file and seeing the effect is fine for this use case.
