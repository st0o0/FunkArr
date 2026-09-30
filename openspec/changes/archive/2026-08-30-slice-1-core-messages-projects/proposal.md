## Why

The foundation re-architecture design (§3-4, §6) requires compiler-enforced layering: Core cannot reach Akka because it cannot see it. Today all code lives in one csproj, so nothing prevents a "pure" type from importing an actor or an HttpClient. Two new projects establish the dependency wall the compiler enforces.

The matching engine (965 lines, 7 inline strategies) is the largest single file and the primary target for the Core extraction. Splitting it into per-strategy files behind `IMatchStrategy` also unblocks Axis 4 (new matching strategy = new file + enum value).

## What Changes

- **Create `FunkArr.Core`** — class library, no dependencies beyond BCL. Receives:
  - `RuleSetModels.cs` types (Rule, Filter, FilterGroup, TitleRule, MatchingStrategy enum, FilterOp enum, etc.)
  - `ContentFilter.cs` (pure static filtering logic)
  - `ResultScorer.cs` (pure scoring/sorting)
  - `ReleaseTitleBuilder.cs` (pure title formatting)
  - `RuleSetGenerator.cs` (pure generation logic)
  - `IMatchStrategy` interface + 7 strategy files extracted from `RuleSetMatchingEngine`
  - Matching engine dispatcher (~250 lines, down from 965)
  - Filter evaluation logic (extracted from engine)
  - Supporting model types (`SearchResult`, `QualityInfo`, `QualityTier`, `MediathekResultItem`, `TvdbEpisodeInfo`, `TmdbMovieInfo`, `MatchedItemInfo`, etc.)
- **Create `FunkArr.Messages`** — class library, depends on Core only. Receives:
  - Standalone message files (`SearchCoordinatorMessages.cs`, `SearchMessages.cs`)
  - `DownloadCoordinatorMessages.cs`, `DownloadCoordinatorEvents.cs`, `QueueCoordinatorEvents.cs`, `DownloadRequestTrackerEvents.cs`
  - Project structure for nested actor messages (moved in Slice 2 alongside shard interface changes)
- **Update `FunkArr`** — add ProjectReferences to Core and Messages
- **Update `FunkArr.Tests`** — add ProjectReferences as needed
- **Update solution file** — include both new projects

## Capabilities

### New Capabilities

- `core-project`: The FunkArr.Core project — pure logic with no I/O, no Akka, no ASP.NET dependencies
- `messages-project`: The FunkArr.Messages project — commands, queries, responses, typed ids

### Modified Capabilities

- `ruleset-matching-engine`: Engine split into per-strategy files with `IMatchStrategy` dispatcher pattern

## Impact

- **Projects:** Two new class libraries added to the solution
- **Code:** ~1500 lines moved/refactored across 20+ files
- **Build:** Main project gains two ProjectReferences; dependency direction enforced by compiler
- **Tests:** Test project gains references to new projects; existing tests must stay green
