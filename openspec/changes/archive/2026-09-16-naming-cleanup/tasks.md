## 1. Schema

- [x] 1.1 Rename `strategy` to `extraction` in `data/community/ruleset.schema.json` (matchingStrategy → extractionMethod)
- [x] 1.2 Update all community ruleset JSON files: `strategy` → `extraction`

## 2. Messages

- [x] 2.1 Rename `IdentificationStrategy` enum to `ExtractionMethod` (update JsonStringEnumMemberName values stay the same)
- [x] 2.2 Rename `IdentificationSpec` to `ExtractionSpec`
- [x] 2.3 Rename `IMetadataResolver` actor key to `IMetadataMatching` in ActorKeys.cs
- [x] 2.4 Update `RuleSetDetailRule` field from `Strategy` to `Extraction`

## 3. Project rename

- [x] 3.1 Rename `FunkArr.MetadataResolver` project directory and .csproj to `FunkArr.MetadataMatching`
- [x] 3.2 Rename `FunkArr.MetadataResolver.Tests` project directory and .csproj to `FunkArr.MetadataMatching.Tests`
- [x] 3.3 Update namespaces in all files under both projects
- [x] 3.4 Update solution file (FunkArr.slnx) with new project paths
- [x] 3.5 Update all .csproj ProjectReferences pointing to old project name

## 4. Class renames

- [x] 4.1 Rename `MetadataResolverManager` → `MetadataMatchingManager`
- [x] 4.2 Rename `EpisodeResolver` → `EpisodeMatcher`
- [x] 4.3 Rename `MovieResolver` → `MovieMatcher`
- [x] 4.4 Rename `TvdbResolverActor` → `TvdbMatchingActor`
- [x] 4.5 Rename `TmdbResolverActor` → `TmdbMatchingActor`

## 5. Update consumers

- [x] 5.1 Update RuleSetMerger to parse `extraction` field (was `strategy`)
- [x] 5.2 Update RuleSetManagerState.ToDetailRules to use new names
- [x] 5.3 Update AkkaSetupContainer actor registrations
- [x] 5.4 Update Search workers actor ref types
- [x] 5.5 Update API mapping extensions if affected

## 6. Verify

- [x] 6.1 Run dotnet format and verify build
- [x] 6.2 Run all tests
