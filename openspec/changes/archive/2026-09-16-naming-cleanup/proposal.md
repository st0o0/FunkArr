## Why

The codebase uses "Strategy" and "Resolver" for multiple unrelated concepts, making the code confusing to navigate. "Strategy" means both rule extraction method (scoring) and TVDB match label (resolution). "Resolver" means both ruleset lookup, metadata orchestration, and pure matching logic.

## What Changes

- Rename `strategy` field in ruleset schema to `extraction`
- Rename `IdentificationStrategy` enum to `ExtractionMethod`
- Rename `IdentificationSpec` to `ExtractionSpec`
- Rename `MetadataResolverManager` to `MetadataMatchingManager`
- Rename `EpisodeResolver` / `MovieResolver` to `EpisodeMatcher` / `MovieMatcher`
- Rename project `FunkArr.MetadataResolver` to `FunkArr.MetadataMatching` (including test project)
- Rename actor key interface `IMetadataResolver` to `IMetadataMatching`
- **BREAKING**: ruleset schema field `strategy` → `extraction`

## Capabilities

### New Capabilities
- `naming-conventions`: Consistent terminology across schema, messages, and domain code

### Modified Capabilities

## Impact

- `FunkArr.MetadataResolver` → `FunkArr.MetadataMatching` (project rename)
- `FunkArr.MetadataResolver.Tests` → `FunkArr.MetadataMatching.Tests`
- `FunkArr.Messages` - enum and spec renames
- `FunkArr.RuleSet` - RuleSetMerger parses `extraction` instead of `strategy`
- `FunkArr.Search` - actor ref rename
- `FunkArr` (host) - DI registration rename
- `data/community/ruleset.schema.json` - field rename
- All existing ruleset JSON files need `strategy` → `extraction`
- Solution file update
