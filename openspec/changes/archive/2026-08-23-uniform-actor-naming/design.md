# Design: Uniform Actor Naming

## Approach

Mechanical rename in a single pass. No behavioral changes.

## Key Constraint: Persistence IDs

Event-sourced actors have `PersistenceId` strings that are used as journal keys. These must NOT change:

- `DownloadCoordinator` → class becomes `DownloadActor`, but `PersistenceId` stays whatever it currently is
- `DownloadRequestTracker` → same
- `QueueCoordinator` → same
- `SeriesResolver`, `MovieResolver` → not renamed at all
- `MatchQualityWorker` → class becomes `MatchQualityActor`, `PersistenceId` unchanged

Verify each persistent actor's `PersistenceId` property before and after rename.

## Shard Region Type Names

`WithShardRegion<T>` uses the type name as the region name by default. After rename:
- `WithShardRegion<TextSearchPipeline>` → `WithShardRegion<TextSearchActor>`
- etc.

The `typeName` parameter is set explicitly in registration — verify it stays the same string to avoid cluster issues. If it derives from the class name, pin it to the old string.

## Namespace Change

Files moving from `Search/Pipelines/` to `Search/` change namespace:
- `FunkArr.Search.Pipelines` → `FunkArr.Search`

All `using FunkArr.Search.Pipelines;` statements become unnecessary (already in `FunkArr.Search`).

## Execution Order

1. Rename files + classes + references in `Search/Pipelines/` → `Search/` (includes namespace change)
2. Rename remaining actors (no folder move, just class + file + refs)
3. Verify persistence IDs unchanged
4. Verify shard region type names unchanged (pin if needed)
5. Update CLAUDE.md convention section
6. Build + test
