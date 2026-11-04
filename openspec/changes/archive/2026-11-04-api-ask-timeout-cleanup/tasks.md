# Tasks

## 1. FunkArr.Api endpoint files

- [x] `DownloadsApiEndpoints.cs`: remove `_askTimeout` field, change all Ask calls to `Ask<T>(msg, ct)`
- [x] `FeedApiEndpoints.cs`: remove `_askTimeout` field, change all Ask calls
- [x] `MediathekApiEndpoints.cs`: remove `_queryTimeout` field, change Ask call
- [x] `RuleSetApiEndpoints.cs`: remove `_queryTimeout`, `_statsTimeout`, `_testTimeout` fields, change all Ask calls
- [x] `SystemApiEndpoints.cs`: remove `_askTimeout` field, change Ask call

## 2. EndpointExceptionFilter

- [x] Remove `AskTimeoutException` from the catch clause (keep `TimeoutException` for other sources)
- [x] Remove `using Akka.Actor` if no longer needed

## 3. FunkArr.ArrApi service files

- [x] `SabnzbdQueueService.cs`: remove `Timeout` property, change Ask calls, change `catch (TimeoutException)` → `catch (OperationCanceledException)`
- [x] `SabnzbdDownloadService.cs`: remove `Timeout` property, change Ask calls, change `catch (TimeoutException)` → `catch (OperationCanceledException)`
- [x] `NewznabSearchService.cs`: remove `_feedTimeout` field and `timeout` local var, change Ask calls, change `catch (TimeoutException)` → `catch (OperationCanceledException)`

## 4. Dead config cleanup

- [x] Remove `DownloadTimeoutSeconds` and `SearchTimeoutSeconds` from `ArrApiOptions`

## 5. Verify

- [x] `dotnet build src/FunkArr.slnx`
- [x] `dotnet format src/FunkArr.slnx --verify-no-changes`
