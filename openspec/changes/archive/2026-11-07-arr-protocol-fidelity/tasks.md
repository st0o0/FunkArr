## 1. get_config Response Fields

- [x] 1.1 Add `HistoryRetentionOption` (string, `"all"`) and `HistoryRetentionNumber` (int, `0`) properties to `SabnzbdMiscConfig` in `ConfigResponse.cs` with appropriate JSON property names
- [x] 1.2 Add `TvCategories`, `MovieCategories`, `DateCategories` (string arrays) to `SabnzbdConfigResponse` in `ConfigResponse.cs`
- [x] 1.3 Populate `TvCategories` and `MovieCategories` from `DownloadOptions.Categories` by media type in `SabnzbdQueueService.GetConfig()`

## 2. Error Responses — HTTP 200

- [x] 2.1 Change `NewznabXmlResult.Error` StatusCode to 200 (was 403/400)
- [x] 2.2 Change `SabnzbdController.MapResult` Error branch to always use StatusCode 200 (was `err.StatusCode`)
- [x] 2.3 Change `SabnzbdApiKeyFilter.CreateErrorResult` to StatusCode 200 and message `"API Key Required"` (was 403 / `"API Key Incorrect"`)

## 3. Numeric Formatting

- [x] 3.1 Change `Mb` and `Mbleft` in `SabnzbdResponseMapper` from `ToString("F0")` to `ToString("F1", CultureInfo.InvariantCulture)`

## 4. Tests

- [x] 4.1 Unit test: `get_config` response contains `history_retention_option: "all"` and `history_retention_number: 0`
- [x] 4.2 Unit test: `get_config` response populates `tv_categories` and `movie_categories` from configured categories
- [x] 4.3 Unit test: Newznab error response has HTTP 200 status with XML error body
- [x] 4.4 Unit test: SABnzbd error response has HTTP 200 status with JSON error body
- [x] 4.5 Unit test: SABnzbd API key error returns "API Key Required" with HTTP 200
- [x] 4.6 Unit test: `mb`/`mbleft` formatted with invariant culture and decimal precision

## 5. Verify

- [x] 5.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 5.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.3 Run ArrApi tests
