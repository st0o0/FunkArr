## 1. Messages

- [x] 1.1 Remove `Resolution` parameter from `MatchingConfig` record
- [x] 1.2 Remove `Resolution` parameter from `ScoreCompleted` record

## 2. RuleSet

- [x] 2.1 Remove `RawResolutionConfig` class, `MergeResolution`, `TransformResolution`, and `Resolution` from `RawRuleSet` in `RuleSetMerger`
- [x] 2.2 Remove `Resolution` argument from `MatchingConfig` construction calls in `RuleSetMerger`

## 3. MatchMagic

- [x] 3.1 Remove `Resolution` passthrough in `MatchMagicActor` when constructing `ScoreCompleted`

## 4. Search

- [x] 4.1 Remove `IOptions<MetadataResolverOptions>` from `TvSearchWorker` constructor
- [x] 4.2 Replace `scored.Resolution ?? new ResolutionConfig(...)` with `new ResolutionConfig()` in `HandleScoreCompleted`

## 5. Host and Core

- [x] 5.1 Delete `MetadataResolverOptions.cs` from FunkArr.Core
- [x] 5.2 Remove `MetadataResolverOptions` DI registration from `ServiceSetupContainer`
- [x] 5.3 Remove `FunkArr:MetadataResolver` section from appsettings files (if present)

## 6. Tests

- [x] 6.1 Update `TvSearchWorkerTests` to remove `MetadataResolverOptions` setup
- [x] 6.2 Update any `RuleSetMerger` tests that reference resolution merging

## 7. Verify

- [x] 7.1 Run `dotnet build FunkArr.slnx` from `src/`
- [x] 7.2 Run `dotnet format --verify-no-changes` from `src/`
- [x] 7.3 Run all affected test projects
