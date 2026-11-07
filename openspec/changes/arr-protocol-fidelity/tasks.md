## 1. get_config Response Fields

- [ ] 1.1 Add `HistoryRetentionOption` (string, `"all"`) and `HistoryRetentionNumber` (int, `0`) properties to `SabnzbdMiscConfig` in `ConfigResponse.cs` with appropriate JSON property names
- [ ] 1.2 Add `TvCategories`, `MovieCategories`, `DateCategories` (string arrays) to `SabnzbdConfigResponse` in `ConfigResponse.cs`
- [ ] 1.3 Populate `TvCategories` and `MovieCategories` from `DownloadOptions.Categories` by media type in `SabnzbdQueueService.GetConfig()`

## 2. Error Responses — HTTP 200

- [ ] 2.1 Change `NewznabXmlResult.Error` StatusCode to 200 (was 403/400)
- [ ] 2.2 Change `SabnzbdController.MapResult` Error branch to always use StatusCode 200 (was `err.StatusCode`)
- [ ] 2.3 Change `SabnzbdApiKeyFilter.CreateErrorResult` to StatusCode 200 and message `"API Key Required"` (was 403 / `"API Key Incorrect"`)

## 3. Numeric Formatting

- [ ] 3.1 Change `Mb` and `Mbleft` in `SabnzbdResponseMapper` from `ToString("F0")` to `ToString("F1", CultureInfo.InvariantCulture)`

## 4. Tests

- [ ] 4.1 Unit test: `get_config` response contains `history_retention_option: "all"` and `history_retention_number: 0`
- [ ] 4.2 Unit test: `get_config` response populates `tv_categories` and `movie_categories` from configured categories
- [ ] 4.3 Unit test: Newznab error response has HTTP 200 status with XML error body
- [ ] 4.4 Unit test: SABnzbd error response has HTTP 200 status with JSON error body
- [ ] 4.5 Unit test: SABnzbd API key error returns "API Key Required" with HTTP 200
- [ ] 4.6 Unit test: `mb`/`mbleft` formatted with invariant culture and decimal precision

## 5. Verify

- [ ] 5.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [ ] 5.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [ ] 5.3 Run ArrApi tests
