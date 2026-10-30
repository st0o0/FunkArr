# Tasks

## 1. Validation RSS builder

- [x] 1.1 Add a static validation RSS builder in `FunkArr.ArrApi/Newznab` that creates an `Rss` with one item for `NewznabCategory.Tv` or `NewznabCategory.Movie` (title `FunkArr.Validation.…`, stable GUID `funkarr-validation-tv|movie`, `get` link with that ID and the API key, `pubDate` from a passed `DateTimeOffset`, positive size, `application/x-nzb` enclosure, `category` attribute 5040/2040), honoring `offset` (item only at offset 0, `total=1` always). Verify with unit tests in `FunkArr.ArrApi.Tests/Newznab` that assert the exact title, GUID, category attribute, size, pubDate and the `newznab:response` offset/total for offset 0 and offset 1.
- [x] 1.2 Add a unit test in `NzbServiceTests` proving that `NzbService.GetNzb("funkarr-validation-tv")` and `GetNzb("funkarr-validation-movie")` return `NzbGetResult.Error` with `NewznabError.IncorrectParameter` (code 201). Verify the test passes without production changes to `NzbService`.

## 2. RSS-sync detection in NewznabSearchService

- [x] 2.1 Add an internal static check for requests without search criteria (`t` is `tvsearch`, `movie` or `search`; `q` null or whitespace; no `tvdbid`, `imdbid`, `tmdbid`; `season`/`ep` ignored). Verify with a `Theory` in `NewznabSearchServiceTests` covering: Sonarr RSS, Radarr RSS, whitespace `q`, season+ep only, `tvdbid` only (not RSS), `q=Tatort` (not RSS), `t=caps` (not RSS).
- [x] 2.2 Add internal static category selection for the validation item: `tvsearch` → Tv, `movie` → Movie, `search` → Movie only if every parsable entry of the comma-separated `cat` is in 2000–2999, otherwise Tv. Verify with a `Theory` covering `cat` = `2000,2040`, `2040,5040`, `5030,5040`, empty, and unparsable values.
- [x] 2.3 Inject `TimeProvider` into `NewznabSearchService` and return `SearchServiceResult.Success` with the validation RSS for requests without search criteria, before `BuildCommand` and without touching `SearchResultCache` or the search gateway. Log at Debug level. Verify with `dotnet build src/FunkArr.slnx` and the existing ArrApi tests passing.

## 3. Integration and documentation

- [x] 3.1 Add integration tests in `FunkArr.IntegrationTests/Arr/NewznabSearchTests.cs`: `t=tvsearch` without `q` returns one item in category 5040 and the `ISearchManager` probe receives no `SearchCommand`; `t=movie` without `q` returns one item in category 2040 and no error XML; the validation item's `get` link returns Newznab error 201; `t=tvsearch&q=Tatort` still reaches the probe. Verify with `dotnet run --project src/FunkArr.IntegrationTests/FunkArr.IntegrationTests.csproj`.
- [x] 3.2 Document the behavior in `docs/en/how-it-works.md` and `docs/how-it-works.md` (section "Newznab Response"): RSS sync and indexer-test requests without query or ID are answered with a single validation item until the RSS feed (#204) exists. Verify both language versions describe the same behavior.
- [x] 3.3 Run `dotnet format src/FunkArr.slnx --verify-no-changes` and all affected test projects (`FunkArr.ArrApi.Tests`, `FunkArr.IntegrationTests`, `FunkArr.Architecture.Tests`) and verify they pass.

## Workflow follow-up

- Manual check against the dev environment (`docker compose -f docker-compose.dev.yml up -d --build`): adding FunkArr as an indexer in Sonarr and Radarr passes the "Test" button, and an RSS sync logs no indexer error.
- Archive the change with `/opsx:archive` after review; reference #202 in the PR.
