## Why

A full-project audit uncovered 13 findings: production bugs (RuleSetManager never initializes, pagination silently ignored), naming/pattern inconsistencies, and dead code. Fixing these now prevents confusion as the codebase grows and eliminates two silent production bugs before they surface in real usage.

## What Changes

**Bug fixes**
- RuleSetManager gets DI injection of `IOptionsMonitor<FunkArrOptions>` and self-initializes via `PreStart()` — currently the actor does nothing in production because `ScanRuleSets` is never sent
- `HandleQueryQueue` implements `Start`/`Limit`/`Category` filtering instead of discarding the parameters
- `HandleQueryHistory` implements `Start`/`Limit` pagination instead of discarding the parameters

**Inconsistency fixes**
- RuleSetManagerState switches from mutable `Dictionary`/`HashSet` to immutable collections, matching all other state records
- DownloadHistoryActor renamed to DownloadHistoryManager (cluster singleton naming convention) with `resolver.Props` registration
- `FunkArrOptions` gains `LocalRuleSetDataPath` computed property for the local rulesets path

**Dead code removal**
- Remove `IDownloadResponse` marker interface (never used as `Ask<T>` constraint)
- Remove `IRuleSetUpdater` actor key (actor self-starts, nobody resolves it)
- Remove `_dataDirectory` write-only field from RuleSetManager
- Remove unused `using Akka.IO` from MediathekViewWebManager

**Minor**
- Add `Path.GetFullPath` resolution for download and ruleset working paths

## Capabilities

### New Capabilities

_None — this is a hygiene sweep of existing code._

### Modified Capabilities

- `download-manager`: QueryQueue handler implements pagination/filtering instead of discarding params
- `download-history`: QueryHistory handler implements pagination instead of discarding params
- `download-messages`: Remove `IDownloadResponse` marker interface from download message types
- `ruleset-filewatcher`: RuleSetManager self-initializes from Options via PreStart, immutable state collections
- `ruleset-management`: ScanRuleSets becomes parameterless trigger, path derivation moves to Options

## Impact

- **FunkArr.Core**: `FunkArrOptions` gains `LocalRuleSetDataPath`. `ActorKeys.cs` loses `IRuleSetUpdater` and renames `IDownloadHistory` → `IDownloadHistoryManager`.
- **FunkArr.Download**: DownloadHistoryActor → DownloadHistoryManager rename. Pagination logic in DownloadManagerState and DownloadHistoryManagerState.
- **FunkArr.RuleSet**: RuleSetManager constructor gets DI, PreStart added, state goes immutable.
- **FunkArr.Messages**: `IDownloadResponse` removed from 6 message types.
- **FunkArr.Search**: Unused import removed (trivial).
- **FunkArr (Host)**: AkkaSetupContainer registration updates for renamed actor and removed key.
- **FunkArr.ArrApi**: Callers already send pagination params — no changes needed.
- **Tests**: RuleSetManagerTests update to parameterless ScanRuleSets. DownloadManagerStateTests add pagination coverage.
