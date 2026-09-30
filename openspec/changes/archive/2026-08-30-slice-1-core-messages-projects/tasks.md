## 1. Create project scaffolding

- [x] 1.1 Create `src/FunkArr.Core/FunkArr.Core.csproj` (net10.0, class library, `<RootNamespace>FunkArr</RootNamespace>`, nullable enable, implicit usings)
- [x] 1.2 Create `src/FunkArr.Messages/FunkArr.Messages.csproj` (net10.0, class library, `<RootNamespace>FunkArr</RootNamespace>`, ProjectReference to Core)
- [x] 1.3 Add ProjectReferences from `FunkArr.csproj` to Core and Messages
- [x] 1.4 Add ProjectReferences from `FunkArr.Tests.csproj` to Core and Messages
- [x] 1.5 Add both new projects to `FunkArr.slnx`
- [x] 1.6 Build to verify green

## 2. Move model types to Core

- [x] 2.1 Move `RuleSetModels.cs` types to `Core/RuleSet/` (Rule, Filter, FilterGroup, TitleRule, RuleSetFile, MediaReference, OverrideConfig, enums)
- [x] 2.2 Move `Shared/Models/SearchResult.cs` and `Shared/Models/QualityInfo.cs` to `Core/Model/`
- [x] 2.3 Move `MediathekResultItem` (from `Search/MediathekClient.cs` — extract the record to its own file) to `Core/Model/`
- [x] 2.4 Move `TvdbEpisodeInfo` and `TmdbMovieInfo` model types to `Core/Model/` (extract from client files)
- [x] 2.5 Move `MatchedItemInfo`, `MatchedEpisodeInfo` and related match result types to `Core/Matching/`
- [x] 2.6 Move `MatchTrace` types to `Core/Matching/`
- [x] 2.7 Build and run tests to verify green

## 3. Move pure logic to Core

- [x] 3.1 Move `ContentFilter.cs` from `Shared/` to `Core/ContentFilter.cs`
- [x] 3.2 Move `ResultScorer.cs` from `Search/Matching/` to `Core/Scoring/`
- [x] 3.3 Move `ReleaseTitleBuilder.cs` from `Indexer/` to `Core/Titles/`
- [x] 3.4 Move `RuleSetGenerator.cs` from `RuleSet/` to `Core/Generation/`
- [x] 3.5 Build and run tests to verify green

## 4. Split matching engine — extract filter evaluator

- [x] 4.1 Create `Core/Matching/FilterEvaluator.cs` — extract filter evaluation logic from `RuleSetMatchingEngine`
- [x] 4.2 Build and run tests to verify green

## 5. Split matching engine — extract strategies

- [x] 5.1 Skipped IMatchStrategy interface — strategies extracted as static classes (interface deferred to later slice)
- [x] 5.2 Create `Core/Matching/Strategies/SeasonAndEpisodeNumberStrategy.cs`
- [x] 5.3 Create `Core/Matching/Strategies/ItemTitleExactStrategy.cs`
- [x] 5.4 Create `Core/Matching/Strategies/ItemTitleIncludesStrategy.cs`
- [x] 5.5 Create `Core/Matching/Strategies/ItemTitleEqualsAirdateStrategy.cs`
- [x] 5.6 Create `Core/Matching/Strategies/ByAbsoluteEpisodeNumberStrategy.cs`
- [x] 5.7 Create `Core/Matching/Strategies/MovieTitleMatchStrategy.cs`
- [x] 5.8 Merged with 5.7 — MovieOriginalTitleMatch uses same strategy class with different title input
- [x] 5.9 Extract shared helpers to `Core/Matching/MatchingHelpers.cs`
- [x] 5.10 Build and run tests to verify green

## 6. Refactor engine to dispatcher

- [x] 6.1 Engine refactored as dispatcher over strategy classes (965 → 310 lines)
- [x] 6.2 All callers unchanged — public API preserved
- [x] 6.3 Build and run tests to verify green (578 tests pass)

## 7. Move standalone messages to Messages project

- [x] 7.1 Skipped — SearchCoordinatorMessages depends on types still in FunkArr
- [x] 7.2 Skipped — SearchMessages depends on IShardedMessage still in FunkArr
- [x] 7.3 Move `DownloadCoordinatorEvents.cs` to `Messages/DownloadClient/`
- [x] 7.4 Move `QueueCoordinatorEvents.cs` to `Messages/DownloadClient/`
- [x] 7.5 Move `DownloadRequestTrackerEvents.cs` to `Messages/DownloadClient/`
- [x] 7.6 Build and run tests to verify green

## 8. Cleanup and final verification

- [x] 8.1 Remove empty source files and folders left behind by moves
- [x] 8.2 Run `dotnet format` on all three projects
- [x] 8.3 Full build and test run — all tests pass
- [x] 8.4 Commit
