## Context

All FunkArr code currently lives in a single csproj. The foundation re-architecture requires compiler-enforced layering via separate projects. This slice creates the two extraction targets (`FunkArr.Core`, `FunkArr.Messages`) and performs the first wave of moves — pure logic types and the matching engine split.

The matching engine is 965 lines with 7 inline strategies. Splitting it is the primary extraction work and establishes the `IMatchStrategy` pattern for Axis 4 extensibility.

## Goals / Non-Goals

**Goals:**
- Create `FunkArr.Core` (net10.0 class library, BCL-only)
- Create `FunkArr.Messages` (net10.0 class library, depends on Core only)
- Move all pure model types, filtering, scoring, title building, generation, and matching strategies to Core
- Split `RuleSetMatchingEngine` into `IMatchStrategy` implementations + dispatcher
- Build stays green throughout

**Non-Goals:**
- Moving nested actor messages (Slice 2 — intertwined with shard interface changes)
- Adding `ISeriesShard`/`IMovieShard`/`IDownloadShard` interfaces (Slice 2)
- Adding `IRequest<TResponse>` (Slice 2)
- Architecture tests (land with the slice that makes them pass)
- Deleting `IShardedMessage`/`ShardedMessageExtractor` (Slice 2)

## Decisions

1. **Model types that strategies depend on move to Core first.** `MediathekResultItem`, `TvdbEpisodeInfo`, `TmdbMovieInfo`, `MatchedItemInfo`, `SearchResult`, `QualityInfo`, `QualityTier` must be in Core before strategies can compile there. These types are currently scattered across `Search/`, `Search/Resolvers/`, and `Shared/Models/`.

2. **Filter evaluation moves to Core as a standalone class.** `EvaluateFilterGroup`, `EvaluateFilterGroupWithChecks`, and related helpers are shared infrastructure used by all strategies. They become `FilterEvaluator` in Core, separate from the dispatcher.

3. **Each strategy gets its own file in `Core/Matching/Strategies/`.** The interface:
   ```csharp
   public interface IMatchStrategy
   {
       MatchingStrategy Kind { get; }
       MatchOutcome Match(ContentItem item, Rule rule, MatchingContext context);
   }
   ```
   Seven implementations: `SeasonAndEpisodeNumberStrategy`, `ItemTitleExactStrategy`, `ItemTitleIncludesStrategy`, `ItemTitleEqualsAirdateStrategy`, `ByAbsoluteEpisodeNumberStrategy`, `MovieTitleMatchStrategy`, `MovieOriginalTitleMatchStrategy`.

4. **The engine becomes a dispatcher.** It holds a `Dictionary<MatchingStrategy, IMatchStrategy>` populated from the strategy files. `EvaluateRulesWithTraces` and `EvaluateMovieRulesWithTraces` shrink to: filter → dispatch to strategy → collect traces.

5. **`MatchingContext` is a new Core type** carrying resolved metadata (episode list, movie info, expected runtime). Named deliberately differently from the deleted `MatchContext`.

6. **Messages project starts with standalone message files only.** `SearchCoordinatorMessages.cs`, `SearchMessages.cs`, and the event/message files already separated from actors. Nested actor messages move in Slice 2 when shard interfaces are introduced.

7. **Project structure:**
   ```
   src/
     FunkArr.Core/
       FunkArr.Core.csproj          (net10.0, no dependencies)
       Model/                        MediathekResultItem, SearchResult, QualityInfo, etc.
       RuleSet/                      Rule, Filter, FilterGroup, RuleSetFile, enums
       Matching/                     IMatchStrategy, MatchingContext, MatchOutcome
         Strategies/                 7 strategy files
         FilterEvaluator.cs          filter evaluation logic
         MatchingEngine.cs           dispatcher (~250 lines)
       ContentFilter.cs
       Scoring/                      ResultScorer
       Titles/                       ReleaseTitleBuilder
       Generation/                   RuleSetGenerator
     FunkArr.Messages/
       FunkArr.Messages.csproj       (net10.0, ProjectReference to Core)
       Search/                       SearchMessages, SearchCoordinatorMessages
     FunkArr/
       FunkArr.csproj                (adds ProjectReference to Core + Messages)
       ...existing code with using aliases updated
   ```

8. **Namespace strategy:** Types keep their semantic namespace (`FunkArr.RuleSet`, `FunkArr.Search`, etc.) even when moving to Core, using `<RootNamespace>FunkArr</RootNamespace>` in the Core csproj. This avoids namespace-change churn in all consumers.

## Risks / Trade-offs

- **Risk: Circular dependency during extraction.** Some types reference each other across what will become project boundaries. → Mitigation: Move in dependency order — model types first, then logic that uses them. The survey confirms no cycles exist in the pure types.
- **Risk: Large diff makes review harder.** → Mitigation: The changes are mechanical moves + the engine split. No behavior changes.
- **Risk: `RootNamespace` sharing causes ambiguity.** → Mitigation: Files move to different folders; the compiler resolves by the project they're in. This is a standard pattern for split-project migrations.
