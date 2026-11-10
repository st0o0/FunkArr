## Why

Actor state classes inconsistently name their mutation methods — some use `Apply(TypedEvent)` overloads, others use `ApplyProgress(...)`, `ApplyMaxConcurrent(...)`, etc. This makes the state API non-uniform and harder to scan. Standardizing on `Apply` overloads everywhere gives a single, predictable pattern.

## What Changes

- Rename all public `Apply*`-suffixed methods on state classes to `Apply` overloads
- Introduce small record types for methods that currently take primitive/multiple parameters
- Private helper methods (internal decomposition inside an `Apply` dispatcher) are unchanged
- Update all actor callers and test files
- Update CLAUDE.md actor state pattern section to document the convention

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

_(none — `actor-state-management` spec already requires `Apply` overloads at line 37; this change brings code into compliance)_

## Impact

- **State files** (4 files, ~10 methods): `DownloadWorkerState`, `DownloadManagerState`, `FeedCrawlPageActorState`, `RuleSetManagerState`
- **Actor files**: All actors calling the renamed methods
- **Test files**: All tests referencing `Apply*` methods
- **CLAUDE.md**: Actor state pattern documentation
- No API changes, no persistence changes, no breaking runtime behavior
