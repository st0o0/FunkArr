## Context

RuleSetManager is a Cluster Singleton that scans `data/community/rulesets/` and `data/local/rulesets/` once at startup via `ScanRuleSets`. Each discovered ruleset ID is dispatched to a sharded RuleSetWorker which reads the JSON files, merges community+local layers, pushes a `MatchingConfig` to MatchMagicManager, and registers with RuleSetResolver.

The GitHub release infrastructure is fully operational: release-please tracks `data/community` as a separate component, CI packages `*.json` files into a `community-rulesets.zip` asset on `community-rulesets-v*` tagged releases. However, no runtime code exists to fetch these releases or react to file changes after startup.

## Goals / Non-Goals

**Goals:**
- Live-reload rulesets when files change on disk (community or local directories)
- Automatic polling of GitHub Releases API for community ruleset updates (30-minute interval)
- Atomic extraction of downloaded ZIP archives (no partial state)
- Version comparison to skip unnecessary downloads
- Support for version pinning and air-gapped environments
- Clean removal of rulesets when files disappear

**Non-Goals:**
- GitHub authentication (public repo, unauthenticated API is sufficient)
- UI for triggering updates or viewing update status (future change)
- Persistence of updater state (version tracked via `version.txt` on disk)
- Legacy URL-based ruleset fetch (old codebase is gone)

## Decisions

### 1. Scheduler-based debounce with full directory re-scan

**Decision:** FileSystemWatcher events set a dirty flag on the Manager. On the first event, a `FlushChanges` message is scheduled to self after 500ms. When `FlushChanges` fires, the Manager re-scans both directories entirely and diffs against its `_knownRuleSets` dictionary.

```
FSW event → _dirty = true, schedule FlushChanges(500ms)
  (more events within 500ms → ignored, timer already pending)
FlushChanges → _dirty = false, re-scan dirs, diff, dispatch
```

**Rationale:** Individual path tracking is fragile — `FileSystemWatcher` fires multiple events per file operation, and the RuleSetUpdater's atomic directory swap generates 60+ events simultaneously. A full re-scan after debounce is cheap (~62 `GetFiles` calls, no file content reading) and naturally handles bulk operations. The 500ms window absorbs all events from a single atomic swap.

**Alternative considered:** Per-file change tracking with deduplication. Rejected because the atomic swap case (rename dir, rename temp → dir) generates Created/Deleted events for every file, making individual tracking complex without benefit.

### 2. Single `LoadRuleSet` message replacing `InitRuleSet`

**Decision:** One message type for both initial load and updates: `LoadRuleSet(RuleSetId, CommunityPath?, LocalPath?)`. The RuleSetWorker handler is fully idempotent — read files, merge, push config, register resolver. A separate `RemoveRuleSet(RuleSetId)` message handles deletion.

**Rationale:** The worker logic is identical for init and update. The Manager already knows whether a ruleset is new or changed (from the diff), so it can log the distinction. The worker doesn't need to care — it just loads whatever paths it receives.

**Alternative considered:** Separate `InitRuleSet` and `UpdateRuleSet` messages with the same handler body. Rejected as unnecessary indirection — the semantic distinction belongs in the Manager's log output, not the message protocol.

### 3. RuleSetUpdater as Actor (Singleton)

**Decision:** RuleSetUpdater is a Cluster Singleton actor that:
1. Schedules `CheckForUpdates` to self every 30 minutes (first check on PreStart)
2. Receives `HttpClient` via DI (named `"GitHub"`)
3. Reads `data/community/version.txt` for current version
4. Queries `GET /repos/{repo}/releases` for latest `community-rulesets-v*` release
5. Downloads `community-rulesets.zip` asset if version differs
6. Extracts atomically: temp dir → swap → write `version.txt`
7. FileWatcher in RuleSetManager picks up the resulting changes

**Rationale:** Actor fits the existing pattern, gets scheduler for free, and can receive `CheckForUpdates` from the API layer for a future "check now" button. A `IHostedService` would work for the timer but require a separate mechanism for manual triggers.

**Alternative considered:** `IHostedService` with `PeriodicTimer`. Rejected because it doesn't integrate naturally with actor messaging for manual triggers, and all other periodic work in FunkArr is actor-based.

### 4. Atomic directory swap for ZIP extraction

**Decision:** When extracting a new release ZIP:
1. Create temp directory alongside `data/community/rulesets/` (e.g., `data/community/rulesets-temp-{guid}`)
2. Extract ZIP contents into temp directory
3. Rename `rulesets/` → `rulesets-old-{guid}/`
4. Rename temp → `rulesets/`
5. Delete `rulesets-old-{guid}/`
6. Write updated `version.txt` to `data/community/`

If any step fails, the old directory remains in place. The FileWatcher is temporarily disabled during the swap to avoid spurious events from the intermediate rename — re-enabled after completion, followed by a manual `FlushChanges` trigger to the Manager.

**Rationale:** Prevents a partially-extracted state from being loaded. The rename-swap approach is faster than delete+recreate and provides a clean rollback point. Temporarily disabling the watcher during the swap avoids the burst of events from intermediate states.

**Alternative considered:** Extract in-place (delete old files, write new). Rejected because a failure mid-extraction leaves a corrupt directory. Also considered keeping the watcher running during swap — rejected because it would fire events for the intermediate rename states.

### 5. Manager state and diff logic

**Decision:** RuleSetManager maintains:
- `_knownRuleSets: Dictionary<string, (string? CommunityPath, string? LocalPath)>` — current known state
- `_dirty: bool` — debounce flag
- `_watchers: FileSystemWatcher[]` — one per monitored directory

On `FlushChanges`, the scan produces a new dictionary. The diff yields three categories:
- **New IDs** (in current, not in known) → `LoadRuleSet`
- **Changed paths** (same ID, different community/local path presence or modification time) → `LoadRuleSet`
- **Removed IDs** (in known, not in current) → `RemoveRuleSet`

**Rationale:** Tracking known state in the Manager keeps workers stateless for their initial load path. The diff is O(n) where n is the number of rulesets (~62), which is negligible.

### 6. RuleSetUpdater communicates with Manager via FileWatcher, not messages

**Decision:** The Updater writes files to disk and relies on the FileWatcher to trigger the Manager. The Updater does NOT send messages to the Manager directly.

**Rationale:** This keeps the Updater completely decoupled from the ruleset loading pipeline. The Updater's only job is getting files onto disk — it has no knowledge of actors, merging, or the matching engine. Manual file edits and automated updates follow the exact same code path.

### 7. Configuration via FunkArrOptions

**Decision:** Three new properties on `FunkArrOptions`:

| Property | Type | Default | Description |
|---|---|---|---|
| `RuleSetRepository` | `string` | `"st0o0/funkarr"` | GitHub `owner/repo` |
| `RuleSetVersion` | `string` | `"latest"` | `"latest"` or pinned semver |
| `RuleSetRefreshEnabled` | `bool` | `true` | Disable for air-gapped |

Environment variables: `FunkArr__RuleSetRepository`, `FunkArr__RuleSetVersion`, `FunkArr__RuleSetRefreshEnabled`.

**Rationale:** Follows existing `FunkArrOptions` pattern. Version pinning lets users freeze on a known-good release. The enabled flag provides a clean disable for air-gapped or development environments.

## Risks / Trade-offs

- **FileSystemWatcher reliability on Linux/Docker** → `FileSystemWatcher` uses `inotify` on Linux, which doesn't fire for changes to files inside a renamed/moved directory. The atomic swap triggers rename events on the parent, which the watcher detects. Volume mounts in Docker also support inotify. If the watcher misses an event, the next Updater cycle (30min) re-triggers a version check and the watcher picks up any drift.
- **GitHub API rate limiting (60 req/hour unauthenticated)** → 30-minute polling uses 2 of 60 hourly requests. Risk is negligible for a single instance. Multiple instances behind the same IP could accumulate, but this is a home-server application.
- **Concurrent access during directory swap** → A RuleSetWorker could be reading a file while the Updater renames the directory. Mitigation: workers read file content in a single `File.ReadAllText` call, and if the file disappears mid-read, the worker handles the IOException gracefully (retains current config).
- **Watcher buffer overflow** → `FileSystemWatcher` has an internal buffer. If too many events fire at once, some are lost and an `Error` event is raised. Mitigation: on `Error`, set `_dirty = true` and trigger a full re-scan.
