## 1. Response interfaces in FunkArr.Messages

- [x] 1.1 Add `ISearchResponse` marker interface in `FunkArr.Messages/Search/`; make `SearchCompleted` and `SearchFailed` implement it
- [x] 1.2 Add `IRuleSetResponse` marker interface in `FunkArr.Messages/RuleSet/`; make `RuleSetResolved`, `RuleSetNotFound`, `RuleSetDetailResult`, and `RegisteredRuleSetsResult` implement it
- [x] 1.3 Add `IMediathekResponse` marker interface in `FunkArr.Messages/Mediathek/`; make `MediathekQueryCompleted` and `MediathekQueryFailed` implement it
- [x] 1.4 Add `IScoringResponse` marker interface in `FunkArr.Messages/Scoring/`; make `ScoreCompleted`, `ScoringHistoryResult`, `ScoringDetailResult`, and `ScoringDetailNotFound` implement it

## 2. Unified SearchCommand

- [x] 2.1 Add `SearchCommand` record with nested `TvParams` and `MovieParams` in `FunkArr.Messages/Search/`
- [x] 2.2 Remove `ISearchCommand` interface and delete `ISearchCommand.cs`
- [x] 2.3 Remove `ISearchCommand` from `TvSearchCommand` and `MovieSearchCommand` declarations (keep only `IWithSearchId`)
- [x] 2.4 Delete `GeneralSearchCommand.cs`

## 3. SearchManager receives SearchCommand

- [x] 3.1 Replace `Receive<GeneralSearchCommand>` with `Receive<SearchCommand>` in SearchManager
- [x] 3.2 Add `Receive<TvSearchCommand>` and `Receive<MovieSearchCommand>` routing based on `SearchCommand.Tv` / `SearchCommand.Movie` / `SearchCommand.Cat`
- [x] 3.3 Remove `HandleGeneralSearch`, `RouteTv(GeneralSearchCommand)`, `RouteMovie(GeneralSearchCommand)`, `RouteAll(GeneralSearchCommand)` — replace with unified handler that decomposes SearchCommand
- [x] 3.4 Update SearchManagerState and SearchManagerStateExtensions if they reference GeneralSearchCommand

## 4. SearchHandler builds SearchCommand

- [x] 4.1 Replace `BuildTvSearch`, `BuildMovieSearch`, `BuildGeneralSearch` in SearchHandler to all return `SearchCommand`
- [x] 4.2 Change `AskAndFormat` to accept `SearchCommand` and use `Ask<ISearchResponse>` instead of `Ask<object>`
- [x] 4.3 Remove `ISearchCommand` return type from `BuildCommand` — return `SearchCommand?` directly

## 5. Workers use typed Ask

- [x] 5.1 In `TvSearchWorker`: change `Ask<object>` to `Ask<IRuleSetResponse>` for ResolveRuleSet, `Ask<IMediathekResponse>` for QueryMediathek, `Ask<ScoreCompleted>` for ScoreItems
- [x] 5.2 In `MovieSearchWorker`: same changes as 5.1

## 6. API endpoints use typed Ask

- [x] 6.1 In `RuleSetApiEndpoints`: change `Ask<object>(QueryRuleSetDetail)` to `Ask<IRuleSetResponse>`
- [x] 6.2 In `RuleSetApiEndpoints`: change `Ask<object>(QueryScoringDetail)` to `Ask<IScoringResponse>`

## 7. Tests

- [x] 7.1 Update `SearchManagerTests` to send `SearchCommand` instead of `GeneralSearchCommand`
- [x] 7.2 Update `SearchResultMappingTests` and `NewznabXmlTests` if they reference `ISearchCommand` or `GeneralSearchCommand`
- [x] 7.3 Update `MovieSearchWorkerTests` and `TvSearchWorkerTests` if they reference old types
- [x] 7.4 Build and run all tests, fix any remaining compilation errors
