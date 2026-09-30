## 1. NewznabCategory Value Type

- [x] 1.1 Create `NewznabCategory` sealed record in `FunkArr.ArrApi/Newznab/Models/` with static `Movie` and `Tv` instances, category IDs (2000/5000, 2030/2040/5030/5040), `CategoryId(int quality)`, `DisplayName(int quality)`, and `FromCat(int?)`
- [x] 1.2 Update `Caps.cs` to derive category entries from `NewznabCategory` instead of hardcoded IDs

## 2. ISearchCommand Interface

- [x] 2.1 Add `ISearchCommand` marker interface in `FunkArr.Messages/Search/` and implement it on `TvSearchCommand`, `MovieSearchCommand`, `GeneralSearchCommand`

## 3. SearchHandler

- [x] 3.1 Create `SearchHandler` sealed class in `FunkArr.ArrApi/Newznab/` with `IActorRef` field via constructor, `Handle(IndexerRequest)` dispatching on `req.T`, private `AskSearch(ISearchCommand)` returning `Task<SearchCompleted>`, and private `FormatResult` producing `IResult`
- [x] 3.2 Move `ToRss`, `BuildAttributes` into `SearchHandler` — update to use `NewznabCategory` instead of `bool isMovieSearch`
- [x] 3.3 Slim `IndexerApiEndpoints.MapIndexerApi` to create `SearchHandler` and route to it — keep caps, NZB get, error/XML utilities as static methods

## 4. Tests

- [x] 4.1 Update `SearchResultMappingTests` to use `NewznabCategory` instead of `bool isMovieSearch` and target `SearchHandler` methods
- [x] 4.2 Run tests and `dotnet format`, verify all pass
