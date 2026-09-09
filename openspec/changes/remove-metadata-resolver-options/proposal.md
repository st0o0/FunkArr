## Why

`MetadataResolverOptions` exists as an app-level config class (`appsettings.json`) providing fallback defaults for episode resolution (strategy, threshold, airdate tolerance). The `ResolutionConfig` record in FunkArr.Messages already has identical defaults in its constructor. Additionally, RuleSets can carry a per-RuleSet `resolution` section that flows through `MatchingConfig.Resolution` and `ScoreCompleted.Resolution`. No actual RuleSet uses this override.

The result is three redundant layers of defaulting for three values that never vary.

## What Changes

- Remove `MetadataResolverOptions` class and its DI registration
- Remove `Resolution` field from `MatchingConfig` (scoring messages)
- Remove `Resolution` passthrough from `ScoreCompleted`
- Remove `RawResolutionConfig`, `MergeResolution`, `TransformResolution` from `RuleSetMerger`
- Remove `Resolution` from `RawRuleSet` model
- Simplify `TvSearchWorker` to use `new ResolutionConfig()` directly (record defaults)
- Remove `FunkArr:MetadataResolver` config section from appsettings
- Update affected tests

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `episode-resolution`: Remove requirement that `ResolutionConfig` is configurable per-RuleSet via `MatchingConfig`. Resolution always uses record defaults. The resolution strategies themselves are unchanged.
- `matching-config`: Remove optional `Resolution` field from `MatchingConfig`.
- `tv-search`: Remove `MetadataResolverOptions` dependency from `TvSearchWorker`.

## Impact

- **FunkArr.Core**: `MetadataResolverOptions.cs` deleted
- **FunkArr.RuleSet**: `RuleSetMerger` simplified (remove resolution merge/transform)
- **FunkArr.Messages**: `MatchingConfig.Resolution` and `ScoreCompleted.Resolution` removed
- **FunkArr.MatchMagic**: No longer passes `Resolution` through `ScoreCompleted`
- **FunkArr.Search**: `TvSearchWorker` loses `IOptions<MetadataResolverOptions>` dependency
- **FunkArr (Host)**: DI registration and appsettings section removed
- **Tests**: Affected test files updated
- **No API changes**: External behavior is identical (same defaults apply)
