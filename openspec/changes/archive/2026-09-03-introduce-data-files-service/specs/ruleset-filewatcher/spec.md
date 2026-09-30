## MODIFIED Requirements

### Requirement: RuleSetManager monitors ruleset directories with FileSystemWatcher
The RuleSetManager SHALL inject `IDataFiles` and `DataPaths` via DI and use `DataPaths.CommunityRuleSets` and `DataPaths.LocalRuleSets` as its watched directories. It SHALL create two watchers using `IDataFiles.Watch()`: one for the community directory and one for the local directory. Both SHALL watch for `*.json` file changes.

#### Scenario: Watchers start on PreStart
- **WHEN** the RuleSetManager actor starts
- **THEN** it SHALL use `DataPaths.CommunityRuleSets` and `DataPaths.LocalRuleSets` for directory paths
- **AND** two watchers SHALL be created via `IDataFiles.Watch(directory, "*.json")`
- **AND** both watchers SHALL be active, monitoring for `*.json` file changes

#### Scenario: Watcher created for non-existent directory
- **WHEN** the local rulesets directory does not exist at startup
- **THEN** `IDataFiles.Watch()` SHALL create the directory and return a watcher

#### Scenario: Watchers disposed on PostStop
- **WHEN** the RuleSetManager actor stops
- **THEN** both watcher instances SHALL be disposed

### Requirement: FlushChanges checks only affected rulesets
On `FlushChanges`, the RuleSetManager SHALL check file existence using `IDataFiles.Exists()` for the accumulated pending ruleset IDs. For each pending ID, it SHALL check both `{CommunityRuleSets}/{id}.json` and `{LocalRuleSets}/{id}.json`, build the current `RuleSetPaths`, and compare against the known state to dispatch `LoadRuleSet` or `RemoveRuleSet`.

#### Scenario: New ruleset detected via targeted check
- **WHEN** `FlushChanges` fires with pending ID `"new-show"` and `IDataFiles.Exists("{CommunityRuleSets}/new-show.json")` returns true but the ID is not in known state
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("new-show", communityPath, null)` to the shard region

#### Scenario: Changed ruleset detected via targeted check
- **WHEN** `FlushChanges` fires with pending ID `"tatort"` and the file timestamp differs from known state
- **THEN** the RuleSetManager SHALL send `LoadRuleSet("tatort", communityPath, localPath)` with current paths

#### Scenario: Ruleset removed detected via targeted check
- **WHEN** `FlushChanges` fires with pending ID `"custom-show"` and `IDataFiles.Exists()` returns false for both directories
- **THEN** the RuleSetManager SHALL send `RemoveRuleSet("custom-show")` and remove it from known state
