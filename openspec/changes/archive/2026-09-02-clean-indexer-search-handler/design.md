## Context

`IndexerApiEndpoints` is the Newznab indexer adapter — it translates between the Newznab XML wire format and internal search messages. Currently it's a static class with free functions that pass `IActorRef` and `bool isMovieSearch` through the call chain. Newznab category IDs (`2030`, `2040`, `5030`, `5040`) are scattered as magic numbers, duplicated between `Caps.cs` and the endpoint logic.

## Goals / Non-Goals

**Goals:**
- Single source of truth for Newznab category IDs, labels, and display formatting
- Type-safe gateway ask pattern (no `object cmd`)
- `IActorRef` as instance state, not a parameter threaded through every call
- Clear separation: routing → command building → gateway ask → HTTP formatting

**Non-Goals:**
- Changing the external Newznab API contract (XML format, categories, status codes)
- Adding new search types or categories
- Refactoring the search domain (workers, gateway actor)
- Touching NZB get/download handling (stays in `IndexerApiEndpoints`)

## Decisions

### 1. NewznabCategory as a sealed record with static instances

`NewznabCategory` is a sealed record with private constructor and two static instances (`Movie`, `Tv`). It owns:
- Parent category ID (2000/5000) and label ("Movies"/"TV")
- HD/SD subcategory IDs (2040/2030, 5040/5030)
- `CategoryId(int quality)` → returns HD or SD ID string
- `DisplayName(int quality)` → returns `"Movies > HD"` etc.
- `static FromCat(int?)` → resolves a Newznab cat parameter to an instance

**Why record over enum:** An enum can't carry the associated IDs and formatting logic. A record with static instances is the idiomatic C# pattern for a closed set of typed constants with behavior.

**Why not an enum + extension methods:** Splits the concept across two files and loses the sealed guarantee. The record keeps everything in one place.

**Alternative: constants class.** Would centralize the IDs but not the formatting. The display string building would still live elsewhere. Rejected.

`Caps.cs` references `NewznabCategory.Movie` and `NewznabCategory.Tv` to build its category entries instead of hardcoding the same IDs.

### 2. ISearchCommand marker interface in Messages

A marker interface `ISearchCommand` implemented by `TvSearchCommand`, `MovieSearchCommand`, and `GeneralSearchCommand`. No members — its sole purpose is to constrain the `Ask` call from `object` to `ISearchCommand`.

**Why marker over base record:** The three commands have different shapes (`GeneralSearchCommand` doesn't even have `SearchId`). A shared base would force a lowest-common-denominator design. A marker keeps them independent.

**Where it lives:** `FunkArr.Messages/Search/` alongside the commands. This is a message-layer concern, not adapter-layer.

### 3. SearchHandler as a sealed class with IActorRef field

A new `SearchHandler` class in `FunkArr.ArrApi/Newznab/`:
- Constructor takes `IActorRef` (the search gateway) — stored as a field
- `Handle(IndexerRequest)` → public entry point, dispatches on `req.T`
- Private `AskSearch(ISearchCommand)` → `Task<SearchCompleted>` — gateway ask + response matching, returns domain result (not `IResult`)
- Private `FormatResult(SearchCompleted, int offset, int limit, NewznabCategory)` → `IResult` — ToRss + Serialize + XmlResult, the only place that produces HTTP responses

`IndexerApiEndpoints.MapIndexerApi` creates the handler once from `IActorRegistry` and routes to it. It keeps `caps`, `get` (NZB download), error formatting, and XML serialization as static utilities.

**Why not DI-registered:** The handler is created inline in `MapIndexerApi` — it's an adapter-internal detail, not a service. Registering it in DI would add ceremony for no benefit.

**Flow through Handle:**
```
Handle(req)
  switch req.T:
    "caps"     → Caps (unchanged)
    "get"      → NzbGet (unchanged, stays in IndexerApiEndpoints)
    "tvsearch" → validate → TvSearchCommand + Tv → AskSearch → FormatResult
    "movie"    → validate → MovieSearchCommand + Movie → AskSearch → FormatResult
    "search"   → GeneralSearchCommand + FromCat(cat) → AskSearch → FormatResult
    _          → ErrorResult
```

### 4. Error handling in AskSearch

`AskSearch` returns `Task<SearchCompleted>`. On `SearchFailed` or timeout, it throws an exception that `Handle` catches and maps to `ErrorResult`. This keeps the happy path clean and avoids a union type or extra result wrapper.

**Alternative: return a result type.** Would require either a third-party `OneOf` package or a custom discriminated union. The exception path is simpler and these are already exceptional conditions (timeout, actor failure).

## Risks / Trade-offs

- **Throwing from AskSearch is a control-flow-via-exception pattern** → Acceptable here because `SearchFailed` and timeouts are genuinely exceptional. The alternative (result type) adds complexity for no behavioral gain.
- **SearchHandler is not unit-testable in isolation** (needs an `IActorRef`) → Same as today. The formatting methods (`FormatResult`, `BuildAttributes`) remain `internal static` and directly testable. Integration testing covers the ask path.
