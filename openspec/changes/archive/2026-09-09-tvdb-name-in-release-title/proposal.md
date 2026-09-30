## Why

Release titles start with the Mediathek topic name (e.g., "Löwenzahn mit Peter Lustig") instead of the TVDB series name ("Löwenzahn"). Sonarr parses the series name from the beginning of the release title, so longer topic names that don't match the TVDB entry cause "series title mismatch" warnings. This caused 3 of 6 remaining title mismatch warnings in E2E testing.

## What Changes

- Propagate `media.name` from ruleset JSON through the resolver to search workers
- Use `media.name` (TVDB name) instead of Mediathek topic as the series name portion of release titles when available
- Fall back to Mediathek topic when no media name is configured

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `release-title-format`: Use TVDB media name instead of Mediathek topic in release titles when available

## Impact

- **FunkArr.Messages**: `RegisterRuleSet` and `RuleSetResolved` get `MediaName` parameter
- **FunkArr.RuleSet**: `RuleSetMerger` extracts `media.name`, `RuleSetResolverState` stores and returns it
- **FunkArr.Search**: `TvSearchWorkerState` and `MovieSearchWorkerState` use `MediaName ?? raw.Topic` for title building
