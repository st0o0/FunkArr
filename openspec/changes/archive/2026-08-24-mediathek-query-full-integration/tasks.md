## 1. Domain Model — QueryOperator and MediathekSearchQuery

- [x] 1.1 Add `QueryOperator` enum (`And`, `Or`) to `FunkArr.Search` namespace
- [x] 1.2 Rewrite `MediathekSearchQuery` record: rename `FullText` property to `Search`, add `Operator` (QueryOperator), `DurationMin` (int?), `DurationMax` (int?), `ExcludeFuture` (bool), change `MaxResults` default from 5000 to 200
- [x] 1.3 Update `QueryBuilder`: replace `ByFullText` with `Search(string, QueryOperator)`, add `QueryOperator` parameter to `ByTopic`, add `WithDuration(int?, int?)`, add `ExcludeFuture()`, keep `Latest()` unchanged
- [x] 1.4 Update `Build()` validation: size must be 1..1000 (throw `ArgumentOutOfRangeException`), duration min <= max when both set, at least one of min/max when `WithDuration` called

## 2. Domain Model Tests

- [x] 2.1 Update existing `MediathekSearchQueryTests`: rename ByFullText tests to Search, update default MaxResults assertions from 5000 to 200
- [x] 2.2 Add tests for `QueryOperator`: default And, explicit Or, operator preserved through Build
- [x] 2.3 Add tests for `WithDuration`: min only, max only, range, validation (both null, min > max)
- [x] 2.4 Add tests for `ExcludeFuture`: default false, explicit true
- [x] 2.5 Add tests for size capping: Limit(1000) succeeds, Limit(1001) throws, Limit(0) throws

## 3. Wire Format DTOs

- [x] 3.1 Add `Operator` (string?) property to `MediathekQueryItem` with `JsonIgnoreCondition.WhenWritingNull`
- [x] 3.2 Add `DurationMin` (int?), `DurationMax` (int?), `Future` (bool?) to `MediathekQuery` with `JsonIgnoreCondition.WhenWritingNull` and snake_case JSON naming

## 4. Gateway Translation

- [x] 4.1 Update `ToWireQuery` in `MediathekGatewayActor`: map `Search` to `Fields = ["topic", "title", "description"]`, map `Operator` to lowercase string on each query item, `WithTitle`/`FromChannel` always emit `"and"`
- [x] 4.2 Update `ToWireQuery`: map `DurationMin`/`DurationMax` and `ExcludeFuture` to top-level wire fields

## 5. Gateway Translation Tests

- [x] 5.1 Update existing `MediathekGatewayTranslationTests`: rename FullText tests to Search, update expected fields from `["topic", "title"]` to `["topic", "title", "description"]`
- [x] 5.2 Add translation tests for operator field: And maps to `"and"`, Or maps to `"or"`, FromChannel/WithTitle always `"and"`
- [x] 5.3 Add translation tests for duration: min only, max only, range, null omitted
- [x] 5.4 Add translation test for ExcludeFuture: true maps to `future: false`, default omits field

## 6. Search Actor Updates

- [x] 6.1 Update `TextSearchActor`: replace `ByFullText(query)` with `Search(query).ExcludeFuture().Limit(200)`, update `Latest()` path to add `.ExcludeFuture().Limit(100)`
- [x] 6.2 Update `MovieSearchActor`: replace `ByFullText(title)` with `Search(title).WithDuration(min: 2400).ExcludeFuture().Limit(100)`, same for original-title fallback
- [x] 6.3 Update `TvSearchActor`: add `.ExcludeFuture().Limit(200)` to existing `ByTopic` call, add `.FromChannel(channel)` when RuleSet provides channel info
- [x] 6.4 Update `BrowseActor`: add `.ExcludeFuture().Limit(100)` to `Latest()` call

## 7. Search Actor Tests

- [x] 7.1 Update `TextSearchPipelineTests`: adjust expected query assertions from ByFullText to Search
- [x] 7.2 Update `MovieSearchPipelineTests`: adjust expected query assertions, verify duration and ExcludeFuture
- [x] 7.3 Update `TvSearchPipelineTests`: verify ExcludeFuture and optional FromChannel in query assertions

## 8. Specs and Verification

- [x] 8.1 Build passes: `dotnet build FunkArr.slnx` from `src/`
- [x] 8.2 All tests pass: `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` from `src/`
- [x] 8.3 Run `dotnet format` on changed .cs files
