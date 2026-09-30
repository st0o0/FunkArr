## 1. Result Types

- [x] 1.1 Create `SearchServiceResult` sealed record hierarchy in `FunkArr.ArrApi.Newznab` (Ok/Empty/Failed)
- [x] 1.2 Create `NzbGetResult` sealed record hierarchy in `FunkArr.ArrApi.Newznab` (Ok/Error)
- [x] 1.3 Create `SabnzbdResult` sealed record hierarchy in `FunkArr.ArrApi.Sabnzbd` (Ok/Error)

## 2. Newznab Services

- [x] 2.1 Refactor `NewznabSearchService.Search()` to return `SearchServiceResult` instead of `IActionResult`
- [x] 2.2 Refactor `NzbService.GetNzb()` to return `NzbGetResult` instead of `IActionResult`, remove MVC using
- [x] 2.3 Update `NewznabController.Handle()` to pattern-match on service results and map to `IActionResult`

## 3. SABnzbd Services

- [x] 3.1 Refactor `SabnzbdQueueService` methods to return `SabnzbdResult` instead of `IActionResult`, remove MVC using
- [x] 3.2 Refactor `SabnzbdDownloadService` methods to return `SabnzbdResult` instead of `IActionResult`, remove MVC using
- [x] 3.3 Update `SabnzbdController.HandleGet()` to pattern-match on service results and map to `IActionResult` using ControllerBase helpers
- [x] 3.4 Update `SabnzbdController.HandlePost()` to pattern-match on service results

## 4. Tests

- [x] 4.1 Update `NzbServiceTests` to assert on `NzbGetResult` types instead of `IActionResult` casts
- [x] 4.2 Update any other ArrApi tests that assert on `IActionResult` types
- [x] 4.3 Run all test projects, verify green

## 5. Spec + Doc Cleanup

- [x] 5.1 Update `arr-api-structure` main spec: replace "MVC Controllers" with "controller-based API" terminology
