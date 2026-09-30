## Why

RuleSetManager currently does a one-shot directory scan at startup — once rulesets are loaded, the system never detects changes. If community rulesets are updated on disk (manually or via a future automated mechanism), FunkArr requires a full restart. Additionally, the GitHub release infrastructure for community rulesets is fully built (release-please component, CI workflow packaging ZIPs, version.txt tracking) but there is no runtime code to fetch new releases. Users must manually download and extract updates.

## What Changes

- **RuleSetManager gains FileSystemWatcher support.** Two watchers monitor `data/community/rulesets/` and `data/local/rulesets/` for file changes. Events are debounced via a scheduler-based dirty flag (500ms), then the manager re-scans directories, diffs against known state, and dispatches `LoadRuleSet` or `RemoveRuleSet` to workers.
- **RuleSetManager holds state.** A `_knownRuleSets` dictionary tracks discovered ruleset IDs and their file paths for diffing on re-scan.
- **New `LoadRuleSet` message replaces `InitRuleSet`.** Single message used for both initial load and updates — worker logic is identical (read, merge, push config, register resolver).
- **New `RemoveRuleSet` message.** When a ruleset disappears from both directories, the worker deregisters from the resolver and tells MatchMagicManager to drop the config.
- **New RuleSetUpdater actor (Singleton).** Polls GitHub Releases API every 30 minutes for newer `community-rulesets-v*` releases. Downloads the ZIP asset, extracts atomically to `data/community/rulesets/`, and writes `version.txt`. The FileWatcher picks up the resulting file changes automatically.
- **New configuration options.** `RuleSetRepository` (default `st0o0/funkarr`), `RuleSetVersion` (default `latest`, or pinned version), `RuleSetRefreshEnabled` (default `true`, disable for air-gapped environments).
- **Named HttpClient for GitHub API.** Registered in DI with `User-Agent: FunkArr/{version}` header.

## Capabilities

### New Capabilities
- `ruleset-filewatcher`: FileSystemWatcher-based live reload in RuleSetManager — debounced re-scan, diff-based dispatch of LoadRuleSet/RemoveRuleSet
- `ruleset-updater`: RuleSetUpdater actor that polls GitHub Releases API, downloads ZIP assets, and extracts atomically to the community rulesets directory

### Modified Capabilities
- `ruleset-management`: RuleSetManager gains state (known rulesets map), FileSystemWatcher lifecycle, debounced re-scan. InitRuleSet replaced by LoadRuleSet. New RemoveRuleSet message and worker handling.

## Impact

- **FunkArr.RuleSet**: New `RuleSetUpdater` actor, modified `RuleSetManager` (state + watchers), modified `RuleSetWorker` (LoadRuleSet + RemoveRuleSet handling)
- **FunkArr.Messages**: New `RemoveRuleSet` message, `RemoveMatchingConfig` message
- **FunkArr.MatchMagic**: `MatchMagicManager` must handle `RemoveMatchingConfig` to drop a config by ruleSetId
- **FunkArr (Host)**: New `FunkArrOptions` properties, named HttpClient registration, RuleSetUpdater actor registration
- **Dependencies**: `System.IO.Compression` (runtime-included, no new NuGet package)
